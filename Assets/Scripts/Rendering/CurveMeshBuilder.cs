using UnityEngine;
using System;


namespace StrokeMimicry
{
    // Low-level stroke mesh operations.
    public class CurveMeshBuilder
    {
        private readonly float strokeWidth;
        public readonly ProjectionMode ProjMode;
        private readonly MeshFilter mf;
        private readonly MeshCollider mc;
        public readonly ProjectedCurve ParentStroke;

        public CurveMeshBuilder(ProjectedCurve parent)
        {
            ParentStroke = parent;
            mf = parent.GetComponent<MeshFilter>();
            mf.sharedMesh = new Mesh();
            mf.sharedMesh.MarkDynamic();
            mc = parent.GetComponent<MeshCollider>();
            strokeWidth = StrokeMimicryManager.Instance.MeshThickness;
        }

        public void Finish()
        {
            if (ParentStroke.PointCount > 0)
            {
                mf.sharedMesh.RecalculateBounds();
                mc.sharedMesh = mf.sharedMesh;
            }
        }


        // Main function to manage rendering data (meshes for curves)
        public void DrawNewStrokeSegment()
        {
            Vector3[] vertices;
            Vector3[] strokeMeshNormals;
            int[] triangles;

            // creating a stroke by storing in a mesh
            if (mf.sharedMesh)
            {
                vertices = mf.sharedMesh.vertices;
                triangles = mf.sharedMesh.triangles;
                strokeMeshNormals = mf.sharedMesh.normals;
                mf.sharedMesh.Clear();
            }
            else
            {
                vertices = new Vector3[0];
                strokeMeshNormals = new Vector3[0];
                triangles = new int[0];
            }

            var PointCount = ParentStroke.PointCount;

            if (ParentStroke.PointCount == 1)
                // First point of the stroke: do not draw anything
                return;


            int oldVertexLength = vertices.Length;
            Array.Resize(ref vertices,
                PointCount * StrokeMimicryManager.Instance.MeshVerticesPerPoint);
            Array.Resize(ref strokeMeshNormals,
                PointCount * StrokeMimicryManager.Instance.MeshVerticesPerPoint);


            var latestPoint = ParentStroke.Points[PointCount - 1];
            var previousPoint = ParentStroke.Points[PointCount - 2];
            Vector3 normal = ParentStroke.HitInfoFrames[PointCount - 1].Normal;

            
            Vector3 binormal = Vector3.Cross(latestPoint - previousPoint, normal).normalized;

            // radius of the stroke cross-section
            float r = strokeWidth / 2.0f;

            // Create the vertex ring around the first point
            if (PointCount == 2)
            {
                for (int i = 0; i < StrokeMimicryManager.Instance.MeshVerticesPerPoint; ++i)
                {
                    vertices[i] =
                    previousPoint + 1e-3f * normal +
                    (float)Mathf.Cos(2 * Mathf.PI * (i) / StrokeMimicryManager.Instance.MeshVerticesPerPoint) * r * binormal +
                    (float)Mathf.Sin(2 * Mathf.PI * (i) / StrokeMimicryManager.Instance.MeshVerticesPerPoint) * r * normal;
                    strokeMeshNormals[i] = (vertices[i] - previousPoint).normalized;
                }

                oldVertexLength = StrokeMimicryManager.Instance.MeshVerticesPerPoint;
            }

            for (int i = 0; i < StrokeMimicryManager.Instance.MeshVerticesPerPoint; ++i)
            {
                vertices[oldVertexLength + i] =
                latestPoint + 1e-3f * normal +
                (float)Mathf.Cos(2 * Mathf.PI * (i) / StrokeMimicryManager.Instance.MeshVerticesPerPoint) * r * binormal +
                (float)Mathf.Sin(2 * Mathf.PI * (i) / StrokeMimicryManager.Instance.MeshVerticesPerPoint) * r * normal;
                strokeMeshNormals[oldVertexLength + i] = (vertices[oldVertexLength + i] - latestPoint).normalized;
            }

            int oldTriangleLength = triangles.Length;
            Array.Resize(ref triangles, oldTriangleLength + StrokeMimicryManager.Instance.MeshVerticesPerPoint * 6);
            for (int quad = 0; quad < StrokeMimicryManager.Instance.MeshVerticesPerPoint; ++quad)
            {
                triangles[oldTriangleLength + quad * 6 + 0] = (oldVertexLength - StrokeMimicryManager.Instance.MeshVerticesPerPoint) + quad;
                triangles[oldTriangleLength + quad * 6 + 1] = oldVertexLength + quad;
                triangles[oldTriangleLength + quad * 6 + 2] = (oldVertexLength - StrokeMimicryManager.Instance.MeshVerticesPerPoint) + (quad + 1) % StrokeMimicryManager.Instance.MeshVerticesPerPoint;
                triangles[oldTriangleLength + quad * 6 + 3] = (oldVertexLength - StrokeMimicryManager.Instance.MeshVerticesPerPoint) + (quad + 1) % StrokeMimicryManager.Instance.MeshVerticesPerPoint;
                triangles[oldTriangleLength + quad * 6 + 4] = oldVertexLength + quad;
                triangles[oldTriangleLength + quad * 6 + 5] = oldVertexLength + (quad + 1) % StrokeMimicryManager.Instance.MeshVerticesPerPoint;
            }

            Mesh stroke = mf.sharedMesh;
            stroke.vertices = vertices;
            stroke.normals = strokeMeshNormals;
            stroke.triangles = triangles;
            mf.sharedMesh = stroke;
        }
    }
}