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
 * Credit for the basis of the tunnel mesh code goes to 
 * Dimitris Doukas : http://dev.doukasd.com/2012/07/customizable-procedural-cylinder-for-unity3d-editor/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TunnelView
{
    public GameObject   Obj;
    public Tunnel       Parent;
    public Mesh         Mesh;
    public MeshRenderer MeshRenderer;
    public MeshFilter   MeshFilter;

    public TunnelView(GameObject obj)
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

    public void Execute(float deltatime) { }
}

public class Tunnel 
{
    public GameObject   Obj;
    public TunnelView   View;
    public GameObject   ViewObj;

    public Material     Material;

    public bool         Loop;
    public bool         FlipWallNormals;
    public int          Resolution_Curve;
    public int          Resolution_Walls;
    public float        Radius;

    private CatmullRom  _path;
    private const int   MIN_RESOLUTION_CURVE = 1;
    private const int   MIN_RESOLUTION_WALLS = 3;
    private const float MIN_RADIUS           = 1f;

    public Tunnel(Material mat, Transform parent = null)
    {
        Obj = new GameObject();
        Obj.name = "Tunnel";
        if(parent != null)
            Obj.transform.parent = parent;

        Material = mat;
        _path = new CatmullRom();
        _path.SplineColor    = Color.blue;
        _path.TangentColor   = Color.yellow;
        _path.NormalColor    = Color.red;
        //_path.BankColor      = Color.magenta;
        //_path.CurvatureColor = Color.grey;

        Loop             = false;
        FlipWallNormals  = false;
        Resolution_Curve = MIN_RESOLUTION_CURVE;
        Resolution_Walls = MIN_RESOLUTION_WALLS;
        Radius           = MIN_RADIUS;

        CreateView(Material);
    }

    public void Execute(float deltaTime)
    {
        View.Execute(deltaTime);
    }

    protected void CreateView(Material mat)
    {
        ViewObj = new GameObject();
        ViewObj.name = "View";
        ViewObj.transform.parent = Obj.transform;
        ViewObj.transform.localPosition = new Vector3(0, 0, 0);

        View = new TunnelView(ViewObj);
        View.Parent = this;
        View.MeshRenderer.material = mat;
    }

    public void UpdatePoints(Transform[] points)
    {
        _path.ControlPoints = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
            _path.ControlPoints[i] = points[i].position;
    }

    public void UpdatePoints(Vector3[] points)
    {
        _path.ControlPoints = points;
    }

    public void Rebuild()
    {
        //sanity check
        if (Resolution_Curve < MIN_RESOLUTION_CURVE) Resolution_Curve = MIN_RESOLUTION_CURVE;
        if (Resolution_Walls < MIN_RESOLUTION_WALLS) Resolution_Walls = MIN_RESOLUTION_WALLS;
        if (Radius < MIN_RADIUS) Radius = MIN_RADIUS;

        if (_path.ControlPoints == null)
        {
            Debug.LogError("Path Control Points are NULL");
            return;
        }

        _path.SplineResolution = Resolution_Curve;
        _path.CloseLoop        = Loop;

        View.UpdateMesh(GetTunnelMesh());
    }

    protected Mesh GetTunnelMesh()
    {
        List<CatmullRomCurvePosition> positionsOnCurve = _path.GetAllCurveSubDivisionPositions();

        //calculate how many vertices we need
        int numVertexColumns = Resolution_Walls + 1;	            //+1 for welding
        int numVertexRows = positionsOnCurve.Count;

        //calculate sizes
        int numVertices = numVertexColumns * numVertexRows;
        int numUVs      = numVertices;							    //always
        int numSideTris = Resolution_Walls * Resolution_Curve * _path.ControlPoints.Length * 2;	//for one cap
        int trisArrayLength = numSideTris * 3;	//3 places in the array for each tri

        //initialize arrays
        Vector3[] Vertices = new Vector3[numVertices];
        Vector2[] UVs = new Vector2[numUVs];
        int[] Tris = new int[trisArrayLength];

        //precalculate increments to improve performance
        float angleStep = 2 * Mathf.PI / Resolution_Walls;
        float uvStepH = 1.0f / Resolution_Walls;
        float uvStepV = 1.0f / Resolution_Curve;

        for (int j = 0; j < positionsOnCurve.Count; j++)
        {
            CatmullRomCurvePosition curvePos = positionsOnCurve[j];
            for (int i = 0; i < numVertexColumns; i++)
            {
                //calculate angle for that vertex on the unit circle
                float angle = i * angleStep;

                //"fold" the sheet around as a cylinder by placing the first and last vertex of each row at the same spot
                if (i == numVertexColumns - 1)
                    angle = 0;

                //position current vertex
                Vector3 verticalVertPos = new Vector3(Radius * Mathf.Cos(angle), 0, Radius * Mathf.Sin(angle));
                Vertices[j * numVertexColumns + i] = Quaternion.LookRotation(curvePos.Normal, curvePos.Tangent) * verticalVertPos + curvePos.Pos;

                //calculate UVs
                UVs[j * numVertexColumns + i] = new Vector2(i * uvStepH, j * uvStepV);

                //create the tris				
                if (j == 0 || i >= numVertexColumns - 1)
                {
                    //nothing to do on the first and last "floor" on the tris, capping is done below
                    //also nothing to do on the last column of vertices
                    continue;
                }
                else
                {
                    //create 2 tris below each vertex
                    //6 seems like a magic number. For every vertex we draw 2 tris in this for-loop, therefore we need 2*3=6 indices in the Tris array
                    //offset the base by the number of slots we need for the bottom cap tris. Those will be populated once we draw the cap
                    int baseIndex = (j - 1) * Resolution_Walls * 6 + i * 6;

                    //1st tri - below and in front
                    Tris[baseIndex + 0] = j * numVertexColumns + i;
                    Tris[baseIndex + 1] = j * numVertexColumns + i + 1;
                    Tris[baseIndex + 2] = (j - 1) * numVertexColumns + i;

                    //2nd tri - the one it doesn't touch
                    Tris[baseIndex + 3] = (j - 1) * numVertexColumns + i;
                    Tris[baseIndex + 4] = j * numVertexColumns + i + 1;
                    Tris[baseIndex + 5] = (j - 1) * numVertexColumns + i + 1;
                }
            }
        }

        //assign vertices, uvs and tris
        Mesh tunnelMesh = new Mesh { name = "Tunnel Mesh" };
        tunnelMesh.vertices = Vertices;
        tunnelMesh.uv = UVs;
        if (FlipWallNormals)
            System.Array.Reverse(Tris);
        tunnelMesh.triangles = Tris;
        tunnelMesh.RecalculateNormals();
        tunnelMesh.RecalculateBounds();
        calculateMeshTangents(tunnelMesh);

        return tunnelMesh;
    }

    protected void calculateMeshTangents(Mesh mesh)
    {
        // Recalculate mesh tangents
        // I found this on the internet (Unity forums?), I don't take credit for it.
        //speed up math by copying the mesh arrays
        int[]     triangles = mesh.triangles;
        Vector3[] vertices  = mesh.vertices;
        Vector2[] uv        = mesh.uv;
        Vector3[] normals   = mesh.normals;

        //variable definitions
        int triangleCount = triangles.Length;
        int vertexCount   = vertices.Length;

        Vector3[] tan1     = new Vector3[vertexCount];
        Vector3[] tan2     = new Vector3[vertexCount];
        Vector4[] tangents = new Vector4[vertexCount];

        for (long a = 0; a < triangleCount; a += 3)
        {
            long i1 = triangles[a + 0];
            long i2 = triangles[a + 1];
            long i3 = triangles[a + 2];

            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];
            Vector3 v3 = vertices[i3];

            Vector2 w1 = uv[i1];
            Vector2 w2 = uv[i2];
            Vector2 w3 = uv[i3];

            float x1 = v2.x - v1.x;
            float x2 = v3.x - v1.x;
            float y1 = v2.y - v1.y;
            float y2 = v3.y - v1.y;
            float z1 = v2.z - v1.z;
            float z2 = v3.z - v1.z;

            float s1 = w2.x - w1.x;
            float s2 = w3.x - w1.x;
            float t1 = w2.y - w1.y;
            float t2 = w3.y - w1.y;

            float r = 1.0f / (s1 * t2 - s2 * t1);

            Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
            Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

            tan1[i1] += sdir;
            tan1[i2] += sdir;
            tan1[i3] += sdir;

            tan2[i1] += tdir;
            tan2[i2] += tdir;
            tan2[i3] += tdir;
        }

        for (long a = 0; a < vertexCount; ++a)
        {
            Vector3 n = normals[a];
            Vector3 t = tan1[a];

            //Vector3 tmp = (t - n * Vector3.Dot(n, t)).normalized;
            //tangents[a] = new Vector4(tmp.x, tmp.y, tmp.z);
            Vector3.OrthoNormalize(ref n, ref t);
            tangents[a].x = t.x;
            tangents[a].y = t.y;
            tangents[a].z = t.z;

            tangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
        }

        mesh.tangents = tangents;
    }

    public void OnDrawGizmos()
    {
        _path.OnDrawGizmos();
    }
}
