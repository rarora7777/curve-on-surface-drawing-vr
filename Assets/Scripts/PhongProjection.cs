using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Profiling;
using g3;
using System.Globalization;
using Stopwatch = System.Diagnostics.Stopwatch;


namespace StrokeMimicry
{
    public enum PhongProjectionResult
    {
        Success,
        OutsideTetVolume,
        PhongProjectionFail,
        UnknownError
    };

    struct Tet
    {
        public uint[] idx;
    }

    struct Tri
    {
        public uint[] idx;
    }

    struct VectorX
    {
        public float[] data;

        public VectorX(int size)
        {
            data = new float[size];
        }

        public static VectorX operator +(VectorX v1, VectorX v2)
        {
            VectorX v = new VectorX(v1.data.Length);

            for (int i = 0; i < v1.data.Length; ++i)
                v.data[i] = v1.data[i] + v2.data[i];
            return v;
        }

        public static VectorX operator *(VectorX v1, float scale)
        {
            VectorX v = new VectorX(v1.data.Length);

            for (int i = 0; i < v1.data.Length; ++i)
                v.data[i] = v1.data[i] * scale;
            return v;
        }

        public double[] ToDouble()
        {
            var res = new double[data.Length];
            for (int i = 0; i < data.Length; ++i)
                res[i] = (double)data[i];
            return res;
        }

        public override string ToString()
        {
            return "(" + String.Join(" ", Array.ConvertAll<float, String>(data, Convert.ToString)) + ")";
        }
    }

    public class PhongProjection
    {
        static ProfilerMarker _triTetLoadMarker = new ProfilerMarker("AC.TriTet");
        static ProfilerMarker _phongCreateMarker = new ProfilerMarker("AC.PhongCreate");
        static ProfilerMarker _offsetLoadMarker = new ProfilerMarker("AC.Offset");
        //static ProfilerMarker _ioMarker = new ProfilerMarker("AC.IO");
        const float negEps = -0.00001f;

        private IntPtr phong = IntPtr.Zero;

        [DllImport("Phong")]
        private static extern IntPtr createPhongObject(double[] V, int nV, int dim, uint[] F, int nF);

        [DllImport("Phong")]
        private static extern bool project(IntPtr phong, double[] p, int fid_start, float[] w);

        [DllImport("Phong")]
        private static extern bool projectBruteForce(IntPtr phong, double[] p, float[] w);

        [DllImport("Phong")]
        private static extern bool projectBruteForceEuclidean(IntPtr phong, double[] p, float[] w);

        [DllImport("Phong")]
        private static extern bool deletePhongObject(IntPtr phong);

        [DllImport("Phong")]
        private static extern double getInitTime(IntPtr phong);

        double[] vertices;
        uint[] triangles;

        int triID = 0;

        // tet mesh
        VectorX[] TV;
        Vector3[] TV3D;
        Tet[] TT;
        Matrix4x4[] TMat;
        List<int>[] vertTetList;

        // surface mesh
        VectorX[] FV;
        Vector3[] FV3D;
        Tri[] FF;
        const int dim = 8;

        public string ModelName;
        public bool LoadInsideOffsetSurface;

        DMeshAABBTree3 outMeshTree = null, inMeshTree = null;
        PointAABBTree3 tetCenterTree = null;

        private int oldFid = 0;

        DMesh3 inMesh = null, outMesh = null;

        static int _stats_totalCallsToBary = 0;
        static int _stats_nearestNeighbourBary = 0, _stats_oneRingBary = 0, _stats_bruteForceBary = 0;

        StrokeMimicrySettings settings;

        public void Init()
        {
            settings = GameObject.FindObjectOfType<StrokeMimicrySettings>();
            if (settings is null)
            {
                Debug.LogError("Cannot find stroke mimicry settings!");
                return;
            }

            Stopwatch totalWatch = new Stopwatch();
            totalWatch.Start();

            string tetMeshFile = Path.Combine(Application.streamingAssetsPath, ModelName + "_tet.txt");
            string triMeshFile = Path.Combine(Application.streamingAssetsPath, ModelName + "_tri.txt");
            string outMeshFile = Path.Combine(Application.streamingAssetsPath, ModelName + "_out.obj");

            string inMeshFile =
                LoadInsideOffsetSurface ?
                    Path.Combine(Application.streamingAssetsPath, ModelName + "_in.obj") :
                    "";

            _stats_totalCallsToBary = 0;
            _stats_nearestNeighbourBary = 0;
            _stats_oneRingBary = 0;
            _stats_bruteForceBary = 0;

            if (phong != IntPtr.Zero)
            {
                deletePhongObject(phong);
                inMesh = null;
                outMesh = null;
                inMeshTree = outMeshTree = null;
                tetCenterTree = null;
            }

            _triTetLoadMarker.Begin();
            Stopwatch triTetMeshWatch = new Stopwatch();
            triTetMeshWatch.Start();
            Debug.Log("Loading tet mesh...");
            bool tetRes = LoadTetMesh(tetMeshFile);
            Debug.Assert(tetRes);
            Debug.Log("Loading tri mesh...");
            bool triRes = LoadTriMesh(triMeshFile);
            Debug.Assert(triRes);
            triTetMeshWatch.Stop();
            _triTetLoadMarker.End();

            Debug.Log("Loading offset surfaces...");
            _offsetLoadMarker.Begin();
            Stopwatch inOffsetWatch = new Stopwatch();
            inOffsetWatch.Start();
            if (LoadInsideOffsetSurface)
            {
                StandardMeshReader inMeshReader = new StandardMeshReader();
                inMeshReader.MeshBuilder = new DMesh3Builder();
                var inMeshReadResult = inMeshReader.Read(inMeshFile, new ReadOptions());
                Debug.Assert(inMeshReadResult.code == IOCode.Ok);
                inMesh = ((g3.DMesh3Builder)inMeshReader.MeshBuilder).Meshes[0];
                inMeshTree = new DMeshAABBTree3(inMesh, true);
                inMeshTree.FastWindingNumber(Vector3d.Zero);
            }
            inOffsetWatch.Stop();

            Stopwatch outOffsetWatch = new Stopwatch();
            outOffsetWatch.Start();
            StandardMeshReader outMeshReader = new StandardMeshReader();
            outMeshReader.MeshBuilder = new DMesh3Builder();
            var outMeshReadResult = outMeshReader.Read(outMeshFile, new ReadOptions());
            Debug.Assert(outMeshReadResult.code == IOCode.Ok);
            outMesh = ((g3.DMesh3Builder)outMeshReader.MeshBuilder).Meshes[0];
            outMeshTree = new DMeshAABBTree3(outMesh, true);
            outMeshTree.FastWindingNumber(Vector3d.Zero);
            outOffsetWatch.Stop();
            _offsetLoadMarker.End();
            Debug.Assert(outMesh != null, "Unable to read the outer offset surface!");



            _phongCreateMarker.Begin();
            vertices = new double[FV.Length * dim];
            triangles = new uint[FF.Length * 3];

            for (int i = 0; i < FV.Length; ++i)
                for (int j = 0; j < dim; ++j)
                    vertices[j * FV.Length + i] = (double)FV[i].data[j];

            for (int i = 0; i < FF.Length; ++i)
                for (int j = 0; j < 3; ++j)
                    triangles[j * FF.Length + i] = FF[i].idx[j];

            Debug.Log("Creating phong object...");
            Stopwatch phongCreateWatch = new Stopwatch();
            phongCreateWatch.Start();
            phong = createPhongObject(
                vertices, (int)FV.Length, dim,
                triangles, (int)FF.Length);
            phongCreateWatch.Stop();
            _phongCreateMarker.End();

            totalWatch.Stop();

            Debug.Log("Total initialization time: " + totalWatch.Elapsed.TotalSeconds);
            Debug.Log("Tri and tet mesh load time: " + triTetMeshWatch.Elapsed.TotalSeconds);
            Debug.Log("in-offset surface load time: " + inOffsetWatch.Elapsed.TotalSeconds);
            Debug.Log("out-offset surface load time: " + outOffsetWatch.Elapsed.TotalSeconds);
            Debug.Log("Phong Object creation time: " + phongCreateWatch.Elapsed.TotalSeconds +
                "( of which C++ time: " + getInitTime(phong) + ")");

            //Vector3 dbgPt, dbgProjPt;
            //dbgPt = new Vector3(0.06455f, 0.58133f, 0.18225f);
            //Project(dbgPt, out dbgProjPt, out int dbgTri, out float[] dbgBary, true);
            //Project(dbgPt, out dbgProjPt, out dbgTri, out dbgBary, true, true);
        }

        public Vector3 GetTriangleNormal(int triId)
        {
            var v1 = FV3D[FF[triID].idx[0]];
            var v2 = FV3D[FF[triID].idx[1]];
            var v3 = FV3D[FF[triID].idx[2]];

            return Vector3.Cross(v3 - v1, v2 - v1).normalized;
        }

        void WarningHandler(string message, object other_data)
        {
            Debug.Log(message);
            Debug.Log(other_data.ToString());
        }

        private void OnDestroy()
        {
            if (phong != IntPtr.Zero)
                deletePhongObject(phong);

            Debug.Log(_stats_totalCallsToBary.ToString() + " total calls to findBary()");
            Debug.Log(
                "NN: " + (100f * _stats_nearestNeighbourBary / _stats_totalCallsToBary).ToString("F2") + "%, " +
                "1R: " + (100f * _stats_oneRingBary / _stats_totalCallsToBary).ToString("F2") + "%, " +
                "BF: " + (100f * _stats_bruteForceBary / _stats_totalCallsToBary).ToString("F2") + "%");
        }

        // NOTE: You might want to convert your input point as well as the output of this function
        // to Unity's left-handed coordinate system by flipping the X-coordinate (since this is how 
        // Unity imports OBJ files)
        public PhongProjectionResult Project(
            Vector3 point3D,
            out Vector3 projection,
            out int triID,
            out float[] triBary,
            bool printVerboseDebugInfo = false,
            bool useBruteForce = true)
        {
            triBary = new float[3];
            if (printVerboseDebugInfo)
                Debug.Log("Input point: " + point3D.ToString("F5"));

            projection = new Vector3();
            triID = -1;

            float[] bary;
            int tetId;

            double outWindingNumber = 0, inWindingNumber = 0;
            outWindingNumber = outMeshTree.FastWindingNumber(point3D);

            if (inMeshTree != null)
            {
                inWindingNumber = inMeshTree.FastWindingNumber(point3D);
            }

            // point3D should be INSIDE the outer offset surf, and OUTSIDE the inner offset surf.
            if (outWindingNumber < 0.5 || inWindingNumber > 0.5)
            {
                if (printVerboseDebugInfo)
                    Debug.Log("Point is outside the tet volume! Winding #s: Out " + outWindingNumber.ToString() + " In " + inWindingNumber.ToString());
                return PhongProjectionResult.OutsideTetVolume;
            }

            findBary(point3D, out tetId, out bary, printVerboseDebugInfo);

            if (printVerboseDebugInfo)
            {
                Debug.Log("Tet: " + tetId.ToString());
                Debug.Log("Bary: " + String.Join(" ", Array.ConvertAll<float, String>(bary, Convert.ToString)));
            }

            if (tetId >= 0)
            {
                if (printVerboseDebugInfo)
                {
                    Vector3 pointRecon =
                        TV3D[TT[tetId].idx[0]] * bary[0] +
                        TV3D[TT[tetId].idx[1]] * bary[1] +
                        TV3D[TT[tetId].idx[2]] * bary[2] +
                        TV3D[TT[tetId].idx[3]] * bary[3];
                    Debug.Log("Reconstructed point: " + pointRecon.ToString("F5"));
                }

                VectorX point =
                    TV[TT[tetId].idx[0]] * bary[0] +
                    TV[TT[tetId].idx[1]] * bary[1] +
                    TV[TT[tetId].idx[2]] * bary[2] +
                    TV[TT[tetId].idx[3]] * bary[3];

                if (printVerboseDebugInfo)
                    Debug.Log("8D point: " + String.Join(" ", Array.ConvertAll<double, String>(point.ToDouble(), Convert.ToString)));

                float[] w = new float[4];
                bool proj = false;
                if (useBruteForce)
                {
                    proj = projectBruteForce(phong, point.ToDouble(), w);
                }
                else
                {
                    proj = project(phong, point.ToDouble(), oldFid, w);
                }

                triBary[0] = w[0]; triBary[1] = w[1]; triBary[2] = w[2];
                triID = (int)w[3];

                oldFid = triID;

                if (printVerboseDebugInfo)
                {
                    Debug.Log("Tri: " + triID.ToString());
                    Debug.Log("Bary: " + String.Join(" ", Array.ConvertAll<float, String>(triBary, Convert.ToString)));
                }

                if (proj)
                {
                    projection =
                        FV3D[FF[triID].idx[0]] * triBary[0] +
                        FV3D[FF[triID].idx[1]] * triBary[1] +
                        FV3D[FF[triID].idx[2]] * triBary[2];
                    if (printVerboseDebugInfo)
                        Debug.Log("Projected point: " + projection.ToString("F5"));

                    return PhongProjectionResult.Success;
                }
                return PhongProjectionResult.PhongProjectionFail;
            }
            return PhongProjectionResult.UnknownError;
        }


        /*
         * Load Tet Mesh from file. File format
         * Line 1: <numVert> <numTet>
         * Then, 2 lines per vertex
         * Line 1: x y z // 3D embedding
         * Line 2: x0 x1 x2 x3 x4 x5 x6 x7 // 8D embedding
         * Then, one line per tet
         * Line 1: idx0 idx1 idx2 idx3 // indices into the vert array such that the tet is positively-oriented
         */
        bool LoadTetMesh(string filename)
        {
            NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;

            var data = File.ReadAllLines(filename);

            if (data.Length < 2)
                return false;

            int n = int.Parse(data[0], nfi);
            TV = new VectorX[n];
            TV3D = new Vector3[n];
            int m = int.Parse(data[1], nfi);
            TT = new Tet[m];
            TMat = new Matrix4x4[m];
            vertTetList = new List<int>[n];

            Vector3d[] tetCenters = new Vector3d[m];

            int count = 0;

            for (int i = 2; i < 2 * n + 2; ++i)
            {
                var d = Array.ConvertAll(
                    data[i].Split(' '),
                    x => float.Parse(x, nfi));

                if (i % 2 == 0)
                    TV3D[count] = new Vector3(d[0], d[1], d[2]);
                else
                {
                    TV[count] = new VectorX(dim);
                    TV[count].data = d;
                    vertTetList[count] = new List<int>();
                    count++;
                }
            }

            if (count != TV.Length)
                return false;

            count = 0;

            for (int i = 2 * n + 2; i < 2 * n + m + 2; ++i)
            {
                var d = Array.ConvertAll(
                    data[i].Split(' '),
                    x => uint.Parse(x, nfi));


                TT[count] = new Tet();
                TT[count].idx = new uint[4];
                TT[count].idx = d;

                if (tetVolume(count) < 0)
                {
                    var temp = TT[count].idx[0];
                    TT[count].idx[0] = TT[count].idx[1];
                    TT[count].idx[1] = temp;
                }

                var v1 = TV3D[TT[count].idx[0]];
                var v2 = TV3D[TT[count].idx[1]];
                var v3 = TV3D[TT[count].idx[2]];
                var v4 = TV3D[TT[count].idx[3]];

                vertTetList[(int)TT[count].idx[0]].Add(count);
                vertTetList[(int)TT[count].idx[1]].Add(count);
                vertTetList[(int)TT[count].idx[2]].Add(count);
                vertTetList[(int)TT[count].idx[3]].Add(count);

                tetCenters[count] = (v1 + v2 + v3 + v4) / 4;

                TMat[count] = new Matrix4x4(
                    new Vector4(v1.x - v4.x, v1.y - v4.y, v1.z - v4.z, 0),
                    new Vector4(v2.x - v4.x, v2.y - v4.y, v2.z - v4.z, 0),
                    new Vector4(v3.x - v4.x, v3.y - v4.y, v3.z - v4.z, 0),
                    new Vector4(0, 0, 0, 1));

                TMat[count] = TMat[count].inverse;
                Debug.Assert(tetVolume(count) > 0);
                count = count + 1;
            }

            tetCenterTree = new PointAABBTree3(new Globals.PointSet(tetCenters));

            if (count != TT.Length)
                return false;

            return true;
        }

        bool LoadTriMesh(string filename)
        {
            NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;

            var data = File.ReadAllLines(filename);

            if (data.Length < 2)
                return false;

            int n = int.Parse(data[0], nfi);
            FV = new VectorX[n];

            FV3D = new Vector3[n];
            int m = int.Parse(data[1], nfi);
            FF = new Tri[m];

            int count = 0;

            for (int i = 2; i < 2 * n + 2; ++i)
            {
                var d = Array.ConvertAll(
                    data[i].Split(' '),
                    x => float.Parse(x, nfi));

                if (i % 2 == 0)
                    FV3D[count] = new Vector3(d[0], d[1], d[2]);
                else
                {
                    FV[count] = new VectorX(dim);
                    FV[count].data = d;
                    count++;
                }
            }

            if (count != FV.Length)
                return false;

            count = 0;

            for (int i = 2 * n + 2; i < 2 * n + m + 2; ++i)
            {
                var d = Array.ConvertAll(
                    data[i].Split(' '),
                    x => uint.Parse(x, nfi));

                FF[count] = new Tri();
                FF[count].idx = new uint[3];
                FF[count].idx = d;
                count = count + 1;
            }

            if (count != FF.Length)
                return false;

            return true;
        }

        double tetVolume(int tetIdx)
        {
            var v1 = TV3D[TT[tetIdx].idx[0]];
            var v2 = TV3D[TT[tetIdx].idx[1]];
            var v3 = TV3D[TT[tetIdx].idx[2]];
            var v4 = TV3D[TT[tetIdx].idx[3]];

            return Vector3.Dot((v1 - v4), Vector3.Cross(v2 - v4, v3 - v4)) / 6;
        }

        Vector3 findBaryInTet(Vector3 p, int tetId)
        {
            return TMat[tetId].MultiplyPoint(p - TV3D[TT[tetId].idx[3]]);
        }

        /*
         * Find the location (tetId: tetrahedron index, bary: barycentric coordinate)
         * of a given point `p` inside the tet mesh:
         * 1. Find the tet whose center is nearest to p (using the kd-tree 
         * `tetCenterTree`) and check if this tet contains p.
         * 2. If not, check if any of the 1-ring neighbours of the tet contain p.
         * 3. If not, fall back to brute force search over all tets. This logs 
         * a debug message.
         */
        void findBary(Vector3 p, out int tetId, out float[] bary, bool printDebugInfo = false)
        {
            _stats_totalCallsToBary++;
            // lambda to check if a barycentric coordinate is valid (all elements ∈ [0, 1])
            bool isBaryValid(Vector3 coor) => Math.Min(Math.Min(Math.Min(coor.x, coor.y), coor.z), 1 - coor.x - coor.y - coor.z) > negEps;

            float[] baryFromVec3(Vector3 coor) => new float[] { coor.x, coor.y, coor.z, 1 - coor.x - coor.y - coor.z };

            bary = new float[] { -1, -1, -1, -1 };
            tetId = -1;

            // First, search in the nearest tet (point to tet center distance)
            var nearestTetId = tetCenterTree.FindNearestPoint(p);
            var curBary = findBaryInTet(p, nearestTetId);

            if (printDebugInfo)
                Debug.Log("Nearest Tet: " + nearestTetId.ToString());

            if (isBaryValid(curBary))
            {
                if (printDebugInfo)
                    Debug.Log("Nearest tet contains the point!");

                bary = baryFromVec3(curBary);
                tetId = nearestTetId;
                _stats_nearestNeighbourBary++;
                return;
            }

            // If failed, then try searching in 1-ring
            HashSet<int> oneRingTets = new HashSet<int>();
            for (int i = 0; i < 4; ++i)
                foreach (var idx in vertTetList[(int)TT[nearestTetId].idx[i]])
                    oneRingTets.Add(idx);
            oneRingTets.Remove(nearestTetId);

            foreach (var idx in oneRingTets)
            {
                curBary = findBaryInTet(p, idx);
                if (isBaryValid(curBary))
                {
                    if (printDebugInfo)
                        Debug.Log("1-ring contains the point!");

                    bary = baryFromVec3(curBary);
                    tetId = idx;
                    _stats_oneRingBary++;
                    return;
                }
            }

            Debug.Log("Falling back to brute-force search!");

            // If both fail, fall back a brute-force search
            for (int i = 0; i < TT.Length; ++i)
            {
                curBary = findBaryInTet(p, i);
                if (isBaryValid(curBary))
                {
                    bary = baryFromVec3(curBary);
                    tetId = i;
                    _stats_bruteForceBary++;
                    return;
                }
            }

        }
    }
}