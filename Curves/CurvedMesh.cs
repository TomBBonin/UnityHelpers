/* 
 * The first step in my AntHill project. A tunnel built on a 
 * CatmullRom curve creating a tweakable cylinder around the curve.
 * 
 * Use :
 * 
 * Vector3[] points = { new Vector3(x, y, z), new Vector3(x, y, z), new Vector3(x, y, z), new Vector3(x, y, z)}; // or use Transform[] set in editor
 * Tunnel tunnel = new Tunnel(TunnelMaterial);
 * tunnel.Loop             = false;
 * tunnel.FlipWallNormals  = false;
 * tunnel.Resolution_Curve = 10;
 * tunnel.Resolution_Walls = 10;
 * tunnel.Radius           = 10;
 * tunnel.UpdatePoints(points);
 * tunnel.Rebuild();
 * 
 * https://github.com/tombbonin 
 * 
 * Credit for the basis of the curved mesh creation code goes to 
 * Dimitris Doukas : http://dev.doukasd.com/2012/07/customizable-procedural-cylinder-for-unity3d-editor/
 */

using UnityEngine;

namespace Curves
{
    public class CurvedMeshView
    {
        public GameObject Obj;
        public CurvedMesh Parent;
        public Mesh Mesh;
        public MeshRenderer MeshRenderer;
        public MeshFilter MeshFilter;

        public CurvedMeshView(GameObject obj)
        {
            Obj = obj;
            MeshRenderer = Obj.AddComponent<MeshRenderer>();
            MeshFilter = Obj.AddComponent<MeshFilter>();
        }

        public void UpdateMesh(Mesh mesh)
        {
            Mesh = mesh;
            MeshFilter.mesh = mesh;
        }
    }

    public class CurvedMesh
    {
        public GameObject Obj;
        public CurvedMeshView View;
        public GameObject ViewObj;

        public Material Material;
        private readonly Curve _curve;

        public CurvedMesh(Curve curve, Material mat, Transform parent = null)
        {
            Obj = new GameObject { name = "Path" };
            if (parent != null)
                Obj.transform.SetParent(parent, false);

            Material = mat;
            _curve = curve;
            CreateView(Material);
            Rebuild();
        }

        protected void CreateView(Material mat)
        {
            ViewObj = new GameObject { name = "View" };
            ViewObj.transform.SetParent(Obj.transform, false);

            View = new CurvedMeshView(ViewObj)
            {
                Parent = this,
                MeshRenderer = { material = mat }
            };
        }

        public void Rebuild()
        {
            if (_curve.ControlPoints == null)
            {
                Debug.LogError("Curve Control Points are NULL");
                return;
            }

            View.UpdateMesh(GetCurvedMesh());
        }

        protected Mesh GetCurvedMesh()
        {
            var positionsOnCurve = _curve.GetCurvePoints();
            const int wallResolution = 10;
            const float radius = 0.1f;

            //calculate how many vertices we need
            var numVertexColumns = wallResolution + 1; //+1 for welding
            var numVertexRows = positionsOnCurve.Length;

            //calculate sizes
            var numVertices = numVertexColumns * numVertexRows;
            var numUVs = numVertices; //always
            var numSideTris = wallResolution * _curve.Resolution * _curve.ControlPoints.Length * 2; //for one cap
            var trisArrayLength = numSideTris * 3;  //3 places in the array for each tri

            //initialize arrays
            var vertices = new Vector3[numVertices];
            var uvs = new Vector2[numUVs];
            var tris = new int[trisArrayLength];

            //precalculate increments to improve performance
            var angleStep = 2 * Mathf.PI / wallResolution;
            var uvStepH = 1.0f / wallResolution;
            var uvStepV = 1.0f / _curve.Resolution;

            for (var j = 0; j < positionsOnCurve.Length; j++)
            {
                var curvePos = positionsOnCurve[j];
                for (var i = 0; i < numVertexColumns; i++)
                {
                    //calculate angle for that vertex on the unit circle
                    var angle = i * angleStep;

                    //"fold" the sheet around as a cylinder by placing the first and last vertex of each row at the same spot
                    if (i == numVertexColumns - 1)
                        angle = 0;

                    //position current vertex
                    var verticalVertPos = new Vector3(radius * Mathf.Cos(angle), 0, radius * Mathf.Sin(angle));
                    vertices[j * numVertexColumns + i] = Quaternion.LookRotation(curvePos.Normal, curvePos.Tangent) * verticalVertPos + curvePos.Position;

                    //calculate UVs
                    uvs[j * numVertexColumns + i] = new Vector2(i * uvStepH, j * uvStepV);

                    //create the tris				
                    if (j == 0 || i >= numVertexColumns - 1)
                    {
                        //nothing to do on the first and last "floor" on the tris, capping is done below
                        //also nothing to do on the last column of vertices
                        continue;
                    }

                    //create 2 tris below each vertex
                    //6 seems like a magic number. For every vertex we draw 2 tris in this for-loop, therefore we need 2*3=6 indices in the Tris array
                    //offset the base by the number of slots we need for the bottom cap tris. Those will be populated once we draw the cap
                    int baseIndex = (j - 1) * wallResolution * 6 + i * 6;

                    //1st tri - below and in front
                    tris[baseIndex + 0] = j * numVertexColumns + i;
                    tris[baseIndex + 1] = j * numVertexColumns + i + 1;
                    tris[baseIndex + 2] = (j - 1) * numVertexColumns + i;

                    //2nd tri - the one it doesn't touch
                    tris[baseIndex + 3] = (j - 1) * numVertexColumns + i;
                    tris[baseIndex + 4] = j * numVertexColumns + i + 1;
                    tris[baseIndex + 5] = (j - 1) * numVertexColumns + i + 1;
                }
            }


            //assign vertices, uvs and tris
            var curvedMesh = new Mesh
            {
                name = "Curved Mesh",
                vertices = vertices,
                uv = uvs
            };
            //System.Array.Reverse(tris); // flip normals
            curvedMesh.triangles = tris;
            curvedMesh.RecalculateNormals();
            curvedMesh.RecalculateBounds();
            CalculateMeshTangents(curvedMesh);
            curvedMesh.Optimize();

            return curvedMesh;
        }

        protected void CalculateMeshTangents(Mesh mesh)
        {
            // Recalculate mesh tangents
            // I found this on the internet (Unity forums?), I don't take credit for it.
            // speed up math by copying the mesh arrays
            var triangles = mesh.triangles;
            var vertices = mesh.vertices;
            var uv = mesh.uv;
            var normals = mesh.normals;

            //variable definitions
            var triangleCount = triangles.Length;
            var vertexCount = vertices.Length;

            var tan1 = new Vector3[vertexCount];
            var tan2 = new Vector3[vertexCount];
            var tangents = new Vector4[vertexCount];

            for (long a = 0; a < triangleCount; a += 3)
            {
                long i1 = triangles[a + 0];
                long i2 = triangles[a + 1];
                long i3 = triangles[a + 2];

                var v1 = vertices[i1];
                var v2 = vertices[i2];
                var v3 = vertices[i3];

                var w1 = uv[i1];
                var w2 = uv[i2];
                var w3 = uv[i3];

                var x1 = v2.x - v1.x;
                var x2 = v3.x - v1.x;
                var y1 = v2.y - v1.y;
                var y2 = v3.y - v1.y;
                var z1 = v2.z - v1.z;
                var z2 = v3.z - v1.z;

                var s1 = w2.x - w1.x;
                var s2 = w3.x - w1.x;
                var t1 = w2.y - w1.y;
                var t2 = w3.y - w1.y;

                var r = 1.0f / (s1 * t2 - s2 * t1);

                var sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                var tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan1[i3] += sdir;

                tan2[i1] += tdir;
                tan2[i2] += tdir;
                tan2[i3] += tdir;
            }

            for (long a = 0; a < vertexCount; ++a)
            {
                var n = normals[a];
                var t = tan1[a];

                Vector3.OrthoNormalize(ref n, ref t);
                tangents[a].x = t.x;
                tangents[a].y = t.y;
                tangents[a].z = t.z;

                tangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
            }

            mesh.tangents = tangents;
        }

        public void Draw()
        {
            _curve.Draw();
        }
    }
}
