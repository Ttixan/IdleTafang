using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    public static class SectorWedgeMesh
    {
        public static Mesh Create(float innerRadius, float outerRadius, float startAngleDeg, float endAngleDeg, int arcSegments = 12)
        {
            int segments = Mathf.Max(2, arcSegments);
            float startRad = startAngleDeg * Mathf.Deg2Rad;
            float endRad = endAngleDeg * Mathf.Deg2Rad;
            int vertexCount = (segments + 1) * 2;
            var vertices = new Vector3[vertexCount];
            var triangles = new int[segments * 6];
            int triangleIndex = 0;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float angle = Mathf.Lerp(startRad, endRad, t);
                float sin = Mathf.Sin(angle);
                float cos = Mathf.Cos(angle);
                int innerIndex = i * 2;
                int outerIndex = innerIndex + 1;
                vertices[innerIndex] = new Vector3(sin * innerRadius, 0f, cos * innerRadius);
                vertices[outerIndex] = new Vector3(sin * outerRadius, 0f, cos * outerRadius);

                if (i >= segments)
                {
                    continue;
                }

                int nextInner = (i + 1) * 2;
                int nextOuter = nextInner + 1;
                triangles[triangleIndex++] = innerIndex;
                triangles[triangleIndex++] = outerIndex;
                triangles[triangleIndex++] = nextOuter;
                triangles[triangleIndex++] = innerIndex;
                triangles[triangleIndex++] = nextOuter;
                triangles[triangleIndex++] = nextInner;
            }

            var mesh = new Mesh { name = "SectorWedge" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
