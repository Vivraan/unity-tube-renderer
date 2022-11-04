using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OParaskos.TubeRenderer
{
    [RequireComponent(typeof(MeshFilter))]
    [ExecuteInEditMode]
    public class TubeRenderer : MonoBehaviour
    {
#if TUBE_OPARASKOS
        [Min(1)]
        public int subdivisions = 3;
        [Min(0)]
        public int segments = 8;
        public Vector3[] positions;
        [Min(0)]
        public float startWidth = 1f;
        [Min(0)]
        public float endWidth = 1f;
        public bool showNodesInEditor;
        public Vector2 uvScale = Vector2.one;
        public bool inside;
        public bool shouldRender;

        private MeshFilter meshFilter;
        private Mesh mesh;
        private float theta;
        private int lastUpdate;

        public Vector3 GetInterpPosition(float f)
        {
            var a = Math.Max(0, Math.Min(positions.Length, Mathf.FloorToInt(f)));
            var b = Math.Max(0, Math.Min(positions.Length, Mathf.CeilToInt(f)));
            var t = f - a;
            return Vector3.Lerp(positions[a], positions[b], t);
        }

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            mesh ??= new Mesh();
            
            if (!shouldRender) return;
            meshFilter.mesh = CreateMesh();
            lastUpdate = PropHashCode();
        }

        private Mesh CreateMesh()
        {
            var interpolatedPositions = Enumerable.Range(0, (positions.Length - 1) * subdivisions)
                .Select(i => i / (float)subdivisions)
                .Select(GetInterpPosition)
                .Append(positions.Last())
                .ToArray();

            theta = Mathf.PI * 2 / segments;

            var verts = new Vector3[interpolatedPositions.Length * segments];
            var uvs = new Vector2[verts.Length];
            var normals = new Vector3[verts.Length];
            var tris = new int[2 * 3 * verts.Length];

            for (var i = 0; i < interpolatedPositions.Length; i++)
            {
                var dia = Mathf.Lerp(startWidth, endWidth, (float)i / interpolatedPositions.Length);

                var localForward = GetVertexFwd(interpolatedPositions, i);
                var localUp = Vector3.Cross(localForward, Vector3.up);
                var localRight = Vector3.Cross(localForward, localUp);

                for (var j = 0; j < segments; ++j)
                {
                    var t = theta * j;
                    var vert = interpolatedPositions[i] + localUp * (Mathf.Sin(t) * dia) +
                               localRight * (Mathf.Cos(t) * dia);
                    var x = i * segments + j;
                    verts[x] = vert;
                    uvs[x] = uvScale * new Vector2(t / (Mathf.PI * 2), (float)i * positions.Length / subdivisions);
                    normals[x] = (vert - interpolatedPositions[i]).normalized;
                    if (i >= interpolatedPositions.Length - 1) continue;

                    if (inside) normals[x] = -normals[x];
                    if (inside)
                    {
                        tris[x * 6] = x;
                        tris[x * 6 + 1] = x + segments;
                        tris[x * 6 + 2] = x + 1;

                        tris[x * 6 + 3] = x;
                        tris[x * 6 + 4] = x + segments - 1;
                        tris[x * 6 + 5] = x + segments;
                    }
                    else
                    {
                        tris[x * 6] = x + 1;
                        tris[x * 6 + 1] = x + segments;
                        tris[x * 6 + 2] = x;

                        tris[x * 6 + 3] = x + segments;
                        tris[x * 6 + 4] = x + segments - 1;
                        tris[x * 6 + 5] = x;
                    }
                }
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 GetVertexFwd(IReadOnlyList<Vector3> inputPositions, int i)
        {
            var lastPosition = i <= 0 ? inputPositions[i] : inputPositions[i - 1];
            var thisPosition = i < inputPositions.Count - 1 ? inputPositions[i + 1] : inputPositions[i];

            return (lastPosition - thisPosition).normalized;
        }

        private void OnDrawGizmos()
        {
            if (!showNodesInEditor) return;
            
            Gizmos.color = Color.red;
            for (var i = 0; i < positions.Length; ++i)
            {
                var dia = Mathf.Lerp(startWidth, endWidth, (float)i / positions.Length);
                Gizmos.DrawSphere(transform.position + positions[i], dia);
            }
        }

        private int PropHashCode()
        {
            return positions.Aggregate(0, (total, it) => total ^ it.GetHashCode()) ^ positions.GetHashCode() ^
                   segments.GetHashCode() ^ subdivisions.GetHashCode() ^ startWidth.GetHashCode() ^
                   endWidth.GetHashCode();
        }

        private void LateUpdate()
        {
            if (!shouldRender) return;
            if (lastUpdate == PropHashCode()) return;

            meshFilter.mesh = CreateMesh();
        }
#endif
    }
}
