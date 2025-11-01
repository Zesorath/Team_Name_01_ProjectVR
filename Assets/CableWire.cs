using UnityEngine;

/// Generates a 3D tube mesh between two Transforms (no physics).
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class KinematicCableTube : MonoBehaviour
{
    public Transform endA;
    public Transform endB;

    [Header("Curve")]
    [Range(4, 256)] public int segments = 48;
    public float slack = 0.15f;
    public Vector3 sagDirection = Vector3.down;

    [Header("Tube")]
    [Range(3, 64)] public int radialSegments = 14;
    public float radius = 0.008f;
    public float uvTiling = 1f;
    public bool doubleSided = false;

    Mesh mesh;
    Vector3[] verts, normals;
    Vector2[] uvs;
    int[] tris;

    void Awake()
    {
        mesh = new Mesh { name = "CableTube" };
        mesh.MarkDynamic();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    void LateUpdate()
    {
        if (!endA || !endB) { mesh.Clear(); return; }

        // ---- LOCAL SPACE CONVERSION ----
        var t = transform;
        Vector3 p0 = t.InverseTransformPoint(endA.position);
        Vector3 p2 = t.InverseTransformPoint(endB.position);
        float dWorld = Vector3.Distance(endA.position, endB.position);
        Vector3 sagDirLocal = t.InverseTransformDirection(sagDirection).normalized;
        Vector3 p1 = (p0 + p2) * 0.5f + sagDirLocal * (slack * dWorld); // scale sag by world distance

        int ringCount = segments + 1;
        int perRing = radialSegments + 1;
        int vertCount = ringCount * perRing;
        EnsureArrays(vertCount, segments, radialSegments);

        for (int i = 0; i <= segments; i++)
        {
            float t01 = i / (float)segments;

            // Quadratic Bézier in LOCAL space
            Vector3 a = Vector3.Lerp(p0, p1, t01);
            Vector3 b = Vector3.Lerp(p1, p2, t01);
            Vector3 center = Vector3.Lerp(a, b, t01);

            // Tangent in LOCAL space
            Vector3 ta = Vector3.Lerp(p1 - p0, p2 - p1, t01);
            Vector3 tangent = ta.sqrMagnitude > 1e-8f ? ta.normalized : (p2 - p0).normalized;

            // Build local frame (avoid degeneracy)
            Vector3 refUp = Mathf.Abs(Vector3.Dot(Vector3.up, tangent)) > 0.95f ? Vector3.right : Vector3.up;
            Vector3 binormal = Vector3.Cross(tangent, refUp).normalized;
            Vector3 normal = Vector3.Cross(binormal, tangent).normalized;

            for (int j = 0; j <= radialSegments; j++)
            {
                float ang = (j / (float)radialSegments) * Mathf.PI * 2f;
                Vector3 dir = Mathf.Cos(ang) * normal + Mathf.Sin(ang) * binormal;
                int idx = i * perRing + j;

                verts[idx] = center + dir * radius; // LOCAL position
                normals[idx] = dir;                   // LOCAL normal
                uvs[idx] = new Vector2(j / (float)radialSegments, t01 * uvTiling);
            }
        }

        // Triangles
        int triIndex = 0;
        for (int i = 0; i < segments; i++)
        {
            int ringA = i * perRing;
            int ringB = (i + 1) * perRing;
            for (int j = 0; j < radialSegments; j++)
            {
                int a0 = ringA + j, a1 = ringA + j + 1;
                int b0 = ringB + j, b1 = ringB + j + 1;

                tris[triIndex++] = a0; tris[triIndex++] = b1; tris[triIndex++] = a1;
                tris[triIndex++] = a0; tris[triIndex++] = b0; tris[triIndex++] = b1;

                if (doubleSided)
                {
                    tris[triIndex++] = a1; tris[triIndex++] = b1; tris[triIndex++] = a0;
                    tris[triIndex++] = b1; tris[triIndex++] = b0; tris[triIndex++] = a0;
                }
            }
        }

        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
    }

    void EnsureArrays(int vertCount, int segs, int radial)
    {
        int triCount = segs * radial * 6 * (doubleSided ? 2 : 1);
        if (verts == null || verts.Length != vertCount)
        {
            verts = new Vector3[vertCount];
            normals = new Vector3[vertCount];
            uvs = new Vector2[vertCount];
        }
        if (tris == null || tris.Length != triCount)
            tris = new int[triCount];
    }
}
