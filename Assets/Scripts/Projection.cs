using System;
using System.Collections.Generic;
using UnityEngine;
using g3;
using ExtensionMethods;

namespace StrokeMimicry
{
    public static class Projection
    {
        public static bool IsReady
        {
            get
            {
                if (Target is null || !Target.gameObject.activeInHierarchy)
                    return false;

                if (PenObject is null || !PenObject.gameObject.activeInHierarchy)
                    return false;

                return true;
            }
        }

        public static bool IsPhongProjectionAvailable
        {
            get
            {
                if (!IsReady)
                    return false;

                return !(Target.Phong is null);
            }
        }

        private static Target _target;
        public static Target Target
        {
            get => _target;

            set
            {
                _target = value;
                string surfMeshFile = System.IO.Path.Combine(
                    StrokeMimicryManager.Instance.PhongFilesPath,
                    _target.Name + ".obj");

                StandardMeshReader surfMeshReader = new StandardMeshReader();
                surfMeshReader.MeshBuilder = new DMesh3Builder();
                var surfMeshReadResult = surfMeshReader.Read(surfMeshFile, new ReadOptions());
                Debug.Assert(surfMeshReadResult.code == IOCode.Ok);
                SurfMesh = ((DMesh3Builder)surfMeshReader.MeshBuilder).Meshes[0];

                foreach (var vidx in SurfMesh.VertexIndices())
                {
                    SurfMesh.SetVertex(vidx, StrokeMimicryUtils.ChangeHandedness((Vector3)SurfMesh.GetVertex(vidx)));
                    SurfMesh.SetVertexNormal(vidx, StrokeMimicryUtils.ChangeHandedness(SurfMesh.GetVertexNormal(vidx)));
                }

                SurfMeshTree = new DMeshAABBTree3(SurfMesh, true);
                Debug.Assert(SurfMesh.IsClosed());
                Debug.Assert(SurfMesh.IsCompact);
                SurfMeshTree.FastWindingNumber(Vector3d.Zero);

                if (Target.Phong is null)
                {
                    Debug.LogWarning("Phong projection unavailable. Closest point queries will use vanilla version.");
                }
            }
        }

        private static Pen _penObject;
        public static Pen PenObject
        {
            get => _penObject;

            set
            {
                _penObject = value;
            }
        }

        private static Stroke _currentStroke = null;
        public static Stroke CurrentStroke
        {
            get => _currentStroke;

            set
            {
                _currentStroke = value;
            }
        }

        private static DMesh3 SurfMesh { get; set; } = null;
        private static DMeshAABBTree3 SurfMeshTree { get; set; } = null;

        private static DataFrame CurrentDataFrame = null;
        private static Ray SprayRay;
        private static HitInfo CurrentHit;
        private static Ray CurrentRay;

        public static void Update()
        {
            if (!IsReady)
                return;

            CurrentDataFrame = new DataFrame(
                (DateTime.Now - StrokeMimicryManager.Instance.StartTime).TotalMilliseconds,
                Camera.main.transform.position,
                PenObject.transform.TransformPoint(PenObject.PenTipPosition),
                Camera.main.transform.up,
                Camera.main.transform.forward,
                PenObject.transform.up,
                PenObject.transform.forward
                );

            var penTipGlobalPosition = CurrentDataFrame.PenPosition;

            var rotation = Quaternion.LookRotation(
                CurrentDataFrame.ControllerForward,
                CurrentDataFrame.ControllerUp);

            SprayRay = new Ray(
                penTipGlobalPosition,
                rotation * PenObject.SprayDirection
            );

            CurrentRay = SprayRay;

            _Raycast(CurrentRay, out CurrentHit);
        }

        public static void TryCreateNewStroke()
        {
            if (!IsReady)
                return;

            bool hitSuccess = _Raycast(SprayRay, out _);

            if (hitSuccess)
            {
                GameObject strokeObject = new GameObject("stroke");
                CurrentStroke = strokeObject.AddComponent<Stroke>();
                strokeObject.transform.SetParent(Target.transform, false);
                CurrentStroke.Init(StrokeMimicryManager.Instance.ProjectionMode, Target.TargetTransform.localToWorldMatrix);
            }
        }

        public static void TryFinishStroke()
        {
            if (CurrentStroke != null)
                CurrentStroke.Finish();
        }

        
        private static HitInfo _ClosestHitPhong(Vector3 p)
        {
            if (!IsPhongProjectionAvailable)
                return _ClosestHitVanilla(p);

            HitInfo hit = new HitInfo(CurrentDataFrame, Target.TargetTransform);

            var transform = Target.TargetTransform;

            // convert to right-handed point in model space
            // the input point `p` is left-handed and in the world space
            Vector3 positionModelSpace = StrokeMimicryUtils.ChangeHandedness(transform.InverseTransformPoint(p));

            var phong = Target.Phong;
            var res = phong.Project(
                positionModelSpace,
                out Vector3 projection,
                out int triangleIdx,
                out float[] bary,
                false,
                true);

            if (res == PhongProjectionResult.Success)
            {
                hit.Success = true;
                hit.TriangleIndex = triangleIdx;
                hit.BarycentricCoordinate = new Vector3(bary[0], bary[1], bary[2]);

                if (triangleIdx < 0)
                    Debug.LogError("Negative triangle idx!");
                else if (triangleIdx >= SurfMesh.TriangleCount)
                    Debug.LogError("Invalid triangle idx!");

                hit.Point = StrokeMimicryUtils.ChangeHandedness(projection);
                hit.Distance = (positionModelSpace - projection).magnitude;

                hit.Normal = (Vector3)SurfMesh.GetTriNormalLeftHanded(triangleIdx);
            }
            else
            {
                if (res != PhongProjectionResult.OutsideTetVolume)
                    Debug.LogWarning("Phong projection failed! Projection result: " + res.ToString());

                hit = _ClosestHitVanilla(p);
                hit.Success = true;
            }

            return hit;
        }

        private static HitInfo _ClosestHitVanilla(Vector3 p)
        {
            HitInfo hit = new HitInfo(CurrentDataFrame, Target.TargetTransform);

            if (!IsReady)
                return hit;

            p = Target.TargetTransform.InverseTransformPoint(p);

            if (SurfMeshTree == null)
            {
                hit.Success = false;
                return hit;
            }

            hit.TriangleIndex = SurfMeshTree.FindNearestTriangle(p);
            g3.Vector3d ad = new Vector3d(), bd = new Vector3d(), cd = new Vector3d();
            SurfMesh.GetTriVertices(hit.TriangleIndex, ref ad, ref bd, ref cd);

            var distpt3tri3 = new DistPoint3Triangle3(p, new Triangle3d(ad, bd, cd)).Compute();

            hit.Point = (Vector3)distpt3tri3.TriangleClosest;
            hit.Normal = (Vector3)SurfMesh.GetTriNormalLeftHanded(hit.TriangleIndex);
            hit.Distance = (float)distpt3tri3.Get();
            hit.BarycentricCoordinate = (Vector3)distpt3tri3.TriangleBaryCoords;

            hit.Success = true;

            return hit;
        }

        private static bool _Raycast(Ray ray, out HitInfo hitInfo, float maxDist = Mathf.Infinity)
        {
            // assign current dataframe to hitinfo using default constructor
            hitInfo = new HitInfo(CurrentDataFrame, Target.TargetTransform);

            if (!IsReady)
                return false;

            Ray3d ray3d;

            var transform = Target.TargetTransform;

            ray3d = new Ray3d(
                transform.InverseTransformPoint(ray.origin),
                transform.InverseTransformDirection(ray.direction));
        
            var tIdx = SurfMeshTree.FindNearestHitTriangle(ray3d, maxDist);
            if (tIdx == DMesh3.InvalidID)
                return false;

            Vector3d v0 = Vector3d.Zero, v1 = Vector3d.Zero, v2 = Vector3d.Zero;
            SurfMesh.GetTriVertices(tIdx, ref v0, ref v1, ref v2);

            var triangle = new Triangle3d(v0, v1, v2);
            var intr = new IntrRay3Triangle3(ray3d, triangle).Compute();
            var point = ray3d.PointAt(intr.RayParameter);

            hitInfo.Init(
                point,
                SurfMesh.GetTriNormalLeftHanded(tIdx),
                tIdx,
                (point - ray3d.Origin).Length,
                intr.TriangleBaryCoords);

            return true;
        }

        public static void Project()
        {
            if (!IsReady || CurrentStroke == null)
                return;

            var penTipGlobalPosition = CurrentDataFrame.PenPosition;

            HitInfo hit = new HitInfo(CurrentDataFrame, Target.TargetTransform);

            Ray ray = SprayRay;

            var transform = Target.TargetTransform;
            
            var frames = CurrentStroke.HitInfoFrames;
            Vector3 lastPointDrawn = Vector3.zero;
            Vector3 delta = Vector3.zero;

            if (CurrentStroke.PointCount > 0)
            {
                var lastFrame = frames[frames.Count-1];
                var lastUsedPenPosition = lastFrame.Frame.PenPosition;
                lastPointDrawn = lastFrame.Point;

                lastUsedPenPosition = transform.TransformPoint(lastUsedPenPosition);
                lastPointDrawn = transform.TransformPoint(lastPointDrawn);
                
                delta = penTipGlobalPosition - lastUsedPenPosition;
            }

            ProjectionMode projectionMode = StrokeMimicryManager.Instance.ProjectionMode;

            if (projectionMode == ProjectionMode.MimicryPhong && !IsPhongProjectionAvailable)
            {
                projectionMode = ProjectionMode.MimicryClosest;
            }
            
            switch (projectionMode)
            {
                case ProjectionMode.Spraypaint:
                    {
                        ray = SprayRay;
                        _Raycast(ray, out hit);
                        break;
                    }
                // anchored closest-point
                case ProjectionMode.MimicryClosest:
                    {
                        // When starting a new stroke, default to SprayRay
                        if (CurrentStroke.PointCount == 0)
                        {
                            ray = SprayRay;
                            _Raycast(ray, out hit);
                            break;
                        }
                        // Later on, switch to "as-similar-as-possible" projection
                        // The basic idea is this: let delta = movement of the controller b/w the prev. frame and cur. frame
                        // Then, we want to find a point on the mesh which makes the segment from the prev. projectied point
                        // to it as close as possible to delta. If {p_i} and {q_i} are the 3D and projected strokes, then

                        // q_i = argmin_{q \in M} || (q - q_{i-1}) - (p_i - p_{i-1}) ||^2
                        // delta = p_i - p_{i-1}
                        // q_i = project_onto_M( q_{i-1} + delta )
                        // just use closest point projection which actually solves for
                        // q_i = argmin_{q \in M} || q - (q_{i-1} + delta) ||^2
                        // This might actually work better for sharp edges

                        hit = _ClosestHitVanilla(lastPointDrawn + delta);
                        ray = new Ray(penTipGlobalPosition, hit.Point - penTipGlobalPosition);
                        break;
                    }
                // anchored smooth closest-point
                case ProjectionMode.MimicryPhong:
                    { // When starting a new stroke, default to SprayRay
                        if (CurrentStroke.PointCount == 0)
                        {
                            ray = SprayRay;
                            _Raycast(ray, out hit);
                            break;
                        }

                        // q_i = project_onto_M( q_{i-1} + delta )
                        // Perform the projection using [Panozzo 2013] to estimate the projection on the ideal smooth
                        // surface encoded by the mesh M
                        hit = _ClosestHitPhong(lastPointDrawn + delta);
                        ray = new Ray(penTipGlobalPosition, hit.Point - penTipGlobalPosition);
                        break;
                    }
                default:
                    hit = new HitInfo(CurrentDataFrame, Target.TargetTransform);
                    ray = new Ray(penTipGlobalPosition, hit.Point - penTipGlobalPosition);
                    break;
            }

            CurrentRay = ray;
            CurrentHit = hit;

            CurrentStroke.TryDrawPoint(hit);

            Debug.Log("Point: " + hit.Point);
        }

        public static void UpdateProjectionPointer()
        {
            //throw new NotImplementedException();

            // This logic belongs to the ProjectionPointer class.
            // We should have a single object that handles both the pointer and laser logic.
            // Pointer should only appear when CurrentHit is successful.
            //if (hit.Success == false)
            //{
            //    ProjectionPointer.transform.position =
            //        ray.origin + 10.0f * ray.direction;
            //    ProjectionPointer.transform.up = -ray.direction;
            //}
            //else
            //{
            //    ProjectionPointer.transform.position =
            //        transform.TransformPoint(hit.Point);
            //    ProjectionPointer.transform.up =
            //    transform.TransformDirection(hit.Normal);
            //}
        }

    }
}
