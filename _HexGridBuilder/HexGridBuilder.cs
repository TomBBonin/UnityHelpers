/*
 * I wrote this with the intention that it would help me build Hex Grid based games in the future.
 * With lots of help from Red Blob Games, this script should contain most of the functionality you 
 * would need for games of several genres such as strategy, tower defense or prototyping board games.
 * 
 * I do not claim that this is bug free, or uses the best approach for any particular feature. But
 * I currently haven't ran into any and so far it has done the job!
 * You can find and download a demo show casing how this works over at http://tombbonin.com/gridbuilder/
 * 
 * With performance and efficiency in mind, i am trying to stay away from the standard Unity Component approach
 * where every class extends MonoBehaviour and handles its own updates (the death of performance). 
 * The pipe dream is to handle the simulation / game entirely in our own code with a Data Oriented philosophy
 * and only using Unity as a graphics renderer (+ UI) via updating transform positions and rotations.
 * 
 * https://github.com/tombbonin
 * 
 * ----------------
 * Red Blob links :
 * http://www.redblobgames.com/grids/hexagons/
 * http://www.redblobgames.com/pathfinding/a-star/implementation.html#csharp
 */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class HexTileView
{
    public GameObject   Obj;
    public HexTile      parent;
    public Mesh         mesh;
    public MeshRenderer meshRenderer;
    public MeshFilter   meshFilter;

    public HexTileView(GameObject obj)
    {
        Obj = obj;
        meshRenderer = Obj.AddComponent<MeshRenderer>();
        meshFilter   = Obj.AddComponent<MeshFilter>();
    }

    public void Execute(float deltatime) { }
}

public enum TileAccessibility
{
    Accessible,
    Blocked
}

[RequireComponent(typeof(MeshCollider))]
public class HexTileCollider : MonoBehaviour
{   // For the sake of ease this class had remained a component to detect mouse clicks.
    // It would be interesting to try how much slower / faster it would be to handle this ourselves.
    // Get mouse position, transform it to world position, then to grid position and test input for clicks,
    // and build state for hover.
    public HexTile Tile;
    public MeshCollider MCollider;

    public void Awake()
    {
        MCollider = GetComponent<MeshCollider>();
    }

    public void OnMouseOver()  { Tile.OnMouseOver();  }
    public void OnMouseEnter() { Tile.OnMouseEnter(); }
    public void OnMouseExit()  { Tile.OnMouseExit();  }
}

public class HexTile 
{
    public HexGrid     Grid;

    public GameObject  Obj;
    public HexTileView View;
    public GameObject  ViewObj;
    public HexTileView Overlay;
    public GameObject  OverlayObj;
    public HexTileView GridOverlay;
    public GameObject  GridOverlayObj;
    public HexTileCollider HexCollider;

    public Vector2     Pos; // Grid Coord
    public int         Owner;
    public TileAccessibility Accessibility;

    public HexTile(GameObject obj)
    {
        Obj = obj;
    }

    public void Execute(float deltaTime) 
    {
        View.Execute(deltaTime);
        Overlay.Execute(deltaTime);
        GridOverlay.Execute(deltaTime);
    }

    public HexTileView CreateView(Mesh mesh, Material[] mats, string name, string layer)
    {
        var viewObj = new GameObject();
        viewObj.name = name;
        viewObj.layer = LayerMask.NameToLayer(layer);
        viewObj.transform.parent = Obj.transform;
        viewObj.transform.localPosition = new Vector3(0, 0, 0);

        HexTileView view = new HexTileView(viewObj);
        view.mesh = mesh;
        view.parent = this;
        view.meshFilter.mesh = mesh;
        view.meshRenderer.materials = mats;
        return view;
    }

    public void GetNeighbours(List<HexTile> neighbours, int distance)
    {
        for (int x = -distance; x <= distance; x++)
        {
            for (int y = Mathf.Max(-distance, -x - distance); y <= Mathf.Min(distance, -x + distance); y++)
            {
                int offset = ((int)Pos.x & 1) == 0 ? -(x & 1) : (x & 1);
                Vector2 posToCheck = new Vector2(Pos.x + x, Pos.y + (-x - y + (x + offset) / 2));
                HexTile neighbour = null;
                if (Grid.Tiles.TryGetValue(posToCheck, out neighbour) && posToCheck != Pos)
                    neighbours.Add(neighbour);
            }
        }
    }

    public void OnMouseOver()  
    {
        if(Input.GetMouseButtonDown(0))
            Grid.EventDispatcher(this, GridEvent.TileMouseDownL);
        if (Input.GetMouseButtonDown(1))
            Grid.EventDispatcher(this, GridEvent.TileMouseDownR);
        if (Input.GetMouseButtonDown(2))
            Grid.EventDispatcher(this, GridEvent.TileMouseDownM); 
    }
    public void OnMouseEnter() { Grid.EventDispatcher(this, GridEvent.TileMouseEnter); }
    public void OnMouseExit()  { Grid.EventDispatcher(this, GridEvent.TileMouseExit);  }
}

// This state is important as, actors are removed at the end of the grid execution, make sure
// once an actor is destroyed not to process it
public enum GridActorState
{
    Normal,
    Destroyed
}

public class HexGridActor 
{
    public HexGrid    Grid;
    public GameObject Obj;
    public GameObject View;
    public Vector2    Pos;
    public int        Owner;
    public HexTile    Tile;
    public GridActorState State;

    public HexGridActor(GameObject obj)
    {
        Obj = obj;
    }

    virtual public void Execute(float deltaTime)
    {
    }
}

public class GridEventInfo : EventArgs
{
    public HexTile   Tile  { get; set; }
    public GridEvent Event { get; set; }
}

public enum GridEvent
{
    TileMouseDownL,
    TileMouseDownR,
    TileMouseDownM,
    TileMouseEnter,
    TileMouseExit
}

public class HexGrid
{
    #region Variable Declaration

    public GameObject Obj;
    public GameObject TilesObj;
    public GameObject ActorsObj;
    public Dictionary<Vector2, HexTile> Tiles = new Dictionary<Vector2, HexTile>();
    public Dictionary<int, MeshRenderer> Borders = new Dictionary<int, MeshRenderer>();
    public List<HexGridActor> Actors = new List<HexGridActor>();
    public List<HexGridActor> ActorsToRemove = new List<HexGridActor>();
    public Vector2 Size;
    public float HexTileRadius;
    public float HexTileWidth;

    #endregion

    public HexGrid(GameObject obj, GameObject tilesObj, GameObject actorsObj, Vector2 size, float radius, float width)
    {
        Obj = obj;
        TilesObj = tilesObj;
        ActorsObj = actorsObj;
        Size = size;
        HexTileRadius = radius;
        HexTileWidth = width;
    }

    public void Execute(float deltaTime)
    {
        // Execute Tiles
        foreach (KeyValuePair<Vector2, HexTile> entry in Tiles)
            entry.Value.Execute(deltaTime);
        // Execute Actors
        foreach (HexGridActor actor in Actors)
            actor.Execute(deltaTime);

        ProcessActorsToRemove();
    }

    /////////////////////////
    //// Event system
    /////////////////////////

    public event EventHandler<GridEventInfo> TileMouseDownL;
    public event EventHandler<GridEventInfo> TileMouseDownR;
    public event EventHandler<GridEventInfo> TileMouseDownM;
    public event EventHandler<GridEventInfo> TileMouseEnter;
    public event EventHandler<GridEventInfo> TileMouseExit;

    public void EventDispatcher(HexTile tile, GridEvent gridEvent)
    {
        EventHandler<GridEventInfo> eventToDispatch = null;
        switch (gridEvent)
        {
            case GridEvent.TileMouseDownL: { eventToDispatch = TileMouseDownL; break; }
            case GridEvent.TileMouseDownR: { eventToDispatch = TileMouseDownR; break; }
            case GridEvent.TileMouseDownM: { eventToDispatch = TileMouseDownM; break; }
            case GridEvent.TileMouseEnter: { eventToDispatch = TileMouseEnter; break; }
            case GridEvent.TileMouseExit:  { eventToDispatch = TileMouseExit;  break; }
        }

        if (eventToDispatch == null) return;

        var eventInfo = new GridEventInfo() { Tile = tile, Event = gridEvent };
        eventToDispatch(this, eventInfo);
    }

    /////////////////////////
    //// Helpers
    /////////////////////////

    public void GetTileNeighbours(out List<HexTile> neighbours, Vector2 pos, int distance = 1)
    {
        neighbours = new List<HexTile>();
        if (distance == 0) 
            return;
        for (int x = -distance; x <= distance; x++)
        {
            for (int y = Mathf.Max(-distance, -x - distance); y <= Mathf.Min(distance, -x + distance); y++)
            {
                int offset = ((int)pos.x & 1) == 0 ? -(x & 1) : (x & 1);
                Vector2 posToCheck = new Vector2(pos.x + x, pos.y + (-x - y + (x + offset) / 2));
                HexTile neighbour = null;
                if (Tiles.TryGetValue(posToCheck, out neighbour) && posToCheck != pos)
                    neighbours.Add(neighbour);
            }
        }
    }

    public void GetTileNeighbours(out List<Vector2> neighbours, Vector2 pos, int distance = 1)
    {
        neighbours = new List<Vector2>();
        if (distance == 0)
            return;
        for (int x = -distance; x <= distance; x++)
        {
            for (int y = Mathf.Max(-distance, -x - distance); y <= Mathf.Min(distance, -x + distance); y++)
            {
                int offset = ((int)pos.x & 1) == 0 ? -(x & 1) : (x & 1);
                Vector2 posToCheck = new Vector2(pos.x + x, pos.y + (-x - y + (x + offset) / 2));
                HexTile neighbour = null;
                if (posToCheck != pos && Tiles.TryGetValue(posToCheck, out neighbour))
                    neighbours.Add(posToCheck);
            }
        }
    }

    public Vector3 ToPixel(Vector2 gridPos)
    {
        var x = (float)(gridPos.x * 1.5f * HexTileRadius);
        var y = (gridPos.y * HexTileWidth) + (((int)gridPos.x & 1) * HexTileWidth / 2f);

        return new Vector3(x, 0, y);
    }

    public Vector2 ToHex(Vector2 worldPos)
    {
        HexTile tile;
        HexTile result = null;
        float distance = float.MaxValue;

        float colSpacing = 1.5f * HexTileRadius;
        float rowSpacing = HexTileWidth;

        int guessX = Mathf.FloorToInt(worldPos.x / colSpacing);
        int guessY = Mathf.FloorToInt(worldPos.y / rowSpacing);

        for (int x = guessX - 1; x <= guessX + 1; x++)
        {
            for (int y = guessY - 1; y <= guessY + 1; y++)
            {
                Tiles.TryGetValue(new Vector2(x, y), out tile);
                if (tile != null)
                {
                    float dx = worldPos.x - tile.Obj.transform.position.x;
                    float dy = worldPos.y - tile.Obj.transform.position.z;
                    float newdistance = (float)Mathf.Sqrt(dx * dx + dy * dy);

                    if (newdistance < distance)
                    {
                        distance = newdistance;
                        result = tile;
                    }
                }
            }
        }
        return result.Pos;
    }

    /////////////////////////
    //// View Manipulation
    /////////////////////////

    public void SetGridOverlayColor(Color color)
    {
        foreach(KeyValuePair<Vector2, HexTile> entry in Tiles)
        {
            HexTile tile = entry.Value;
            tile.GridOverlay.meshRenderer.material.color = color;
        }
    }

    public void SetBorderColor(int ownerNum, Color color)
    {
        MeshRenderer border = null;
        Borders.TryGetValue(ownerNum, out border);
        if (border == null)
        {
            Debug.LogError("Unknown Border Num");
            return;
        }

        border.material.color = color;
    }

    public void SetOverlayColor(int ownerNum, Color color)
    {
        foreach(KeyValuePair<Vector2, HexTile> entry in Tiles)
        {
            HexTile tile = entry.Value;
            if (tile.Owner != ownerNum) continue;

            tile.Overlay.meshRenderer.material.color = color;
        }
    }

    public void SetOverlayColor(HexTile tile, Color color)
    {
        tile.Overlay.meshRenderer.material.color = color;
    }

    public void SetOverlayMaterial(int ownerNum, Material material)
    {
        foreach (KeyValuePair<Vector2, HexTile> entry in Tiles)
        {
            HexTile tile = entry.Value;
            if (tile.Owner != ownerNum) continue;

            tile.Overlay.meshRenderer.material = material;
        }
    }

    public void SetOverlayMaterial(HexTile tile, Material material)
    {
        tile.Overlay.meshRenderer.material = material;
    }

    public void SetTileMaterial(int ownerNum, Material[] materials)
    {
        foreach (KeyValuePair<Vector2, HexTile> entry in Tiles)
        {
            HexTile tile = entry.Value;
            if (tile.Owner != ownerNum) continue;

            tile.View.meshRenderer.materials = materials;
        }
    }

    public void SetTileMaterial(HexTile tile, Material[] materials)
    {
        tile.View.meshRenderer.materials = materials;
    }

    /////////////////////////
    //// Actors
    /////////////////////////

    public void GetActorsAt(Vector2 pos, out List<HexGridActor> actors)
    {
        actors = new List<HexGridActor>();

        foreach(HexGridActor actor in Actors)
        {
            if (actor.Pos == pos && actor.State != GridActorState.Destroyed)
                actors.Add(actor);
        }
    }

    public void RemoveActor(HexGridActor actor)
    {
        if (actor == null || actor.State == GridActorState.Destroyed) return;

        ActorsToRemove.Add(actor);
        actor.State = GridActorState.Destroyed;
    }

    private void ProcessActorsToRemove() 
    {
        if (ActorsToRemove.Count == 0)
            return;
       
        for (int i = 0; i < ActorsToRemove.Count; i++)
            for (int j = 0; j < Actors.Count; j++)
                if (ActorsToRemove[i] == Actors[j])
                {
                    GameObject.Destroy(Actors[j].Obj);
                    Actors.RemoveAt(j);
                    break;
                }
 
        ActorsToRemove.Clear();
    }

    /////////////////////////
    //// Path finding
    /////////////////////////
    
    // Code for the Piority Queue and Tuple found through RedBlob Games
    public class Tuple<T1, T2>
    {
        public T1 First { get; private set; }
        public T2 Second { get; private set; }
        internal Tuple(T1 first, T2 second)
        {
            First = first;
            Second = second;
        }
    }
    public static class Tuple
    {
        public static Tuple<T1, T2> New<T1, T2>(T1 first, T2 second)
        {
            var tuple = new Tuple<T1, T2>(first, second);
            return tuple;
        }
    }
    public class PriorityQueue<T>
    {
        // I'm using an unsorted array for this example, but ideally this
        // would be a binary heap. Find a binary heap class at
        // http://visualstudiomagazine.com/articles/2012/11/01/priority-queues-with-c.aspx

        private List<Tuple<T, float>> elements = new List<Tuple<T, float>>();

        public int Count
        {
            get { return elements.Count; }
        }

        public void Enqueue(T item, float priority)
        {
            elements.Add(Tuple.New(item, priority));
        }

        public T Dequeue()
        {
            int bestIndex = 0;

            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i].Second < elements[bestIndex].Second)
                {
                    bestIndex = i;
                }
            }

            T bestItem = elements[bestIndex].First;
            elements.RemoveAt(bestIndex);
            return bestItem;
        }
    }

    public void Pathfind_AStar(Vector2 start, Vector2 end, out List<Vector2> aStarPath)
    {
        aStarPath = new List<Vector2>();
        Dictionary<Vector2, Vector2> cameFrom  = new Dictionary<Vector2, Vector2>();
        Dictionary<Vector2, int>     costSoFar = new Dictionary<Vector2, int>();
        PriorityQueue<Vector2>       frontier  = new PriorityQueue<Vector2>();
        frontier.Enqueue(start, 0);
        costSoFar[start] = 0;
        cameFrom[start] = start;

        while (frontier.Count != 0)
        {
            Vector2 current = frontier.Dequeue();
            if (current == end)
                break;

            List<Vector2> neighbours = new List<Vector2>();
            GetTileNeighbours(out neighbours, current);

            for (int i = 0; i < neighbours.Count; i++)
            {
                Vector2 neighbour = neighbours[i];
                int new_cost = costSoFar[current] + GetMoveCost(current, neighbours[i]);
                if (!costSoFar.ContainsKey(neighbour) || new_cost < costSoFar[neighbour])
                {
                    costSoFar[neighbour] = new_cost;
                    float priority = new_cost + Evaluate(neighbour, end);
                    frontier.Enqueue(neighbour, priority);
                    cameFrom[neighbour] = current;
                }
            }
        }

        Vector2 currentTile = end;
        while (currentTile != start)
        {
            aStarPath.Add(currentTile);
            currentTile = cameFrom[currentTile];
        }
        aStarPath.Add(start);
        aStarPath.Reverse();
    }

    public void Pathfind_FloodFill(Vector2 start, out Dictionary<Vector2, Vector2> cameFrom)
    {
        cameFrom  = new Dictionary<Vector2, Vector2>();
        Dictionary<Vector2, int> costSoFar = new Dictionary<Vector2, int>();

        // for multiple flood points (so Agents seek the closest "evac point" for example)
        // stick everything below here in a loop, updating the start point at each iteration

        PriorityQueue<Vector2>   frontier  = new PriorityQueue<Vector2>();
        frontier.Enqueue(start, 0);
        costSoFar[start] = 0;
        cameFrom[start] = start;

        while (frontier.Count != 0)
        {
            Vector2 current = frontier.Dequeue();
            List<Vector2> neighbours = new List<Vector2>();
            GetTileNeighbours(out neighbours, current);

            for (int i = 0; i < neighbours.Count; i++)
            {
                Vector2 neighbour = neighbours[i];
                int new_cost = costSoFar[current] + GetMoveCost(current, neighbours[i]);
                if (!costSoFar.ContainsKey(neighbour) || new_cost < costSoFar[neighbour])
                {
                    costSoFar[neighbour] = new_cost;
                    frontier.Enqueue(neighbour, new_cost);
                    cameFrom[neighbour] = current;
                }
            }
        }
    }

    public static void Pathfind_Floodfill_GetPath(Vector2 pos, out List<Vector2> path, Dictionary<Vector2, Vector2> floodFill)
    {
        // This returns the path from a specific position in the floodfill to the floodpoint
        // Depending on your situation, it may be much faster to use a single floodfill algorithm and extract paths
        // from it, then to perform multiple AStars.
        path = new List<Vector2>();
        Vector2 current = pos;
        while (current != floodFill[current])
        {
            path.Add(current);
            current = floodFill[current];
        }
        path.Add(current);
    }

    // The heuristic used for the A* Algorithm
    private float Evaluate(Vector2 posA, Vector2 posB)
    {
        return (Mathf.Abs(posA.x - posB.x) + Mathf.Abs(posA.y - posB.y));
    }

    // You can attribute a move cost to tiles, 
    // By adding other enums to TileAccesibility. This would allow
    // for a higher move cost on Sand or Water for example
    private int GetMoveCost(Vector2 tileAPos, Vector2 tileBPos)
    {
        HexTile tileB = Tiles[tileBPos];
        switch (tileB.Accessibility)
        {
            case TileAccessibility.Accessible:  return 1;
            case TileAccessibility.Blocked:     return 9999; // We could avoid adding the tile to neighbours, but handling it this way avoids spreading code
        }

        return int.MaxValue;
    }   
}

public enum RelativeTilePosition
{
    Bot,
    BotRight,
    TopRight,
    Top,
    TopLeft,
    BotLeft,

    NbNeighbours,
};

public class HexGridBuilder : MonoBehaviour 
{
    #region Variable Declaration

    public int GridSizeX = 11;
    public int GridSizeY = 8;

    public    float  HexTileRadius = 8;
    protected float _hexTileHalfWidth;
    protected float _hexTileWidth;
    protected float _hexTileSkirtLength;

    public Material[]   HexTileBaseMats;
    public Material[]   HexTileSkirtMats;
    public Material[]   HexTileOverlayMats;
    public Material[]   HexBorderOverlayMats;
    public Material     HexGridOverlayMat;

    protected const float BorderYPos = 0.5f;
    protected const float OverlayYPos = 0.01f;

    protected Mesh _hexTileMesh;
    protected Mesh _hexTileOverLayMesh;

    #endregion

    protected HexGrid CreateHexGrid() 
    {
        ComputeHexDimensions();
        BuildMeshes();

        var gridObj = new GameObject();
        gridObj.name = "Grid";
        var tilesObj = new GameObject();
        tilesObj.transform.parent = gridObj.transform;
        tilesObj.name = "Tiles";
        var actorsObj = new GameObject();
        actorsObj.transform.parent = gridObj.transform;
        actorsObj.name = "Actors";

        var grid = new HexGrid(gridObj, tilesObj, actorsObj, new Vector2(GridSizeX, GridSizeY), HexTileRadius, _hexTileWidth);

        for (int i = 0; i < GridSizeX; i++)
            for (int j = 0; j < GridSizeY; j++)
                CreateTile(i, j, grid, 0);

        CreatePlayerTerritoryBorders(grid, gridObj);
        return grid;
    }

    protected void BuildMeshes()
    {
        _hexTileMesh = GetHexMesh(HexTileRadius, _hexTileHalfWidth, _hexTileSkirtLength);
        _hexTileOverLayMesh = GetHexOverlayMesh(HexTileRadius, _hexTileHalfWidth);
    }

    protected void ComputeHexDimensions()
    {
        _hexTileSkirtLength = HexTileRadius;
        _hexTileHalfWidth = (float)Mathf.Sqrt((HexTileRadius * HexTileRadius) - ((HexTileRadius / 2f) * (HexTileRadius / 2f)));
        _hexTileWidth = 2f * _hexTileHalfWidth;
    }

    virtual protected void CreateTile(int x, int y, HexGrid grid, int owner)
    {
        var hexTileObject = new GameObject();
        var hexTile = new HexTile(hexTileObject);
        CreateHexTileAt(hexTile, x, y, grid, owner);
    }

    protected void CreateHexTileAt(HexTile hexTile, int x, int y, HexGrid grid, int owner)
    {
        var gridPos = new Vector2(x, y);
        var worldPos = grid.ToPixel(gridPos);

        hexTile.Grid = grid;
        hexTile.Pos = gridPos;
        hexTile.Owner = owner;
        hexTile.Accessibility = TileAccessibility.Accessible;
        hexTile.Obj.transform.position = worldPos;
        hexTile.Obj.transform.parent = grid.TilesObj.transform;
        hexTile.Obj.name = "Tile(" + x + ", " + y + ")";

        hexTile.HexCollider = hexTile.Obj.AddComponent<HexTileCollider>();
        hexTile.HexCollider.Tile = hexTile;
        hexTile.HexCollider.MCollider.sharedMesh = _hexTileMesh;
        hexTile.HexCollider.MCollider.convex = true;

        Material[] materials = new Material[2];
        materials[0] = HexTileBaseMats[0];
        materials[1] = HexTileSkirtMats[0];
        hexTile.View = hexTile.CreateView(_hexTileMesh, materials, "View", "Grid");
        materials = new Material[1];
        materials[0] = HexTileOverlayMats[0];
        hexTile.Overlay = hexTile.CreateView(_hexTileOverLayMesh, materials, "Overlay", "Grid");
        materials = new Material[1];
        materials[0] = HexGridOverlayMat;
        hexTile.GridOverlay = hexTile.CreateView(_hexTileOverLayMesh, materials, "Grid", "Grid");

        grid.Tiles.Add(gridPos, hexTile);
    }

    virtual protected HexGridActor CreateActor(string objName, Vector2 pos, HexGrid grid, int owner, GameObject actorView)
    {
        var actorObj = new GameObject();
        actorObj.name = objName;
        var actor = new HexGridActor(actorObj);
        CreateHexGridActorAt(actor, pos, grid, owner, actorView);
        return actor;
    }

    protected void CreateHexGridActorAt(HexGridActor actor, Vector2 pos, HexGrid grid, int owner, GameObject actorView)
    {
        var gridPos  = pos;
        var worldPos = grid.ToPixel(gridPos);

        actor.Obj.transform.position = worldPos;
        actor.Obj.transform.parent = grid.ActorsObj.transform;
        actor.Grid = grid;
        actor.View = Instantiate(actorView) as GameObject;
        actor.View.name = "View";
        actor.View.transform.parent = actor.Obj.transform;
        actor.View.transform.localPosition = actorView.transform.position;
        actor.View.transform.localRotation = actorView.transform.rotation;
        actor.Pos = gridPos;
        actor.Owner = owner;
        actor.Tile = grid.Tiles[gridPos];
        actor.State = GridActorState.Normal;

        grid.Actors.Add(actor);
    }

    protected void CreatePlayerTerritoryBorders(HexGrid grid, GameObject gridObj)
    {
        Dictionary<int, List<HexTile>> territories = new Dictionary<int, List<HexTile>>();
        foreach (KeyValuePair<Vector2, HexTile> entry in grid.Tiles)
        {
            HexTile hexTile = entry.Value;

            List<HexTile> territory = null;
            territories.TryGetValue(hexTile.Owner, out territory);

            if (territory == null)
                territories[hexTile.Owner] = new List<HexTile>();
            territories[hexTile.Owner].Add(hexTile);
        }

        var territoryBorders = new GameObject();
        territoryBorders.transform.position = new Vector3();
        territoryBorders.transform.parent = gridObj.transform;
        territoryBorders.name = "Territory Borders";

        foreach (KeyValuePair<int, List<HexTile>> entry in territories)
        {
            List<HexTile> tiles = entry.Value;
            Mesh territoryBorderMesh = new Mesh();
            BuildTerritoryMesh(grid, ref territoryBorderMesh, tiles, HexTileRadius, BorderYPos, _hexTileHalfWidth);

            var territoryBorder = new GameObject();
            territoryBorder.transform.parent = territoryBorders.transform;
            territoryBorder.transform.localPosition = new Vector3(0, 0, 0);
            territoryBorder.name = "Border " + entry.Key.ToString();
            MeshRenderer meshRenderer = territoryBorder.AddComponent<MeshRenderer>();
            meshRenderer.material = HexBorderOverlayMats[entry.Key];
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.useLightProbes = false;
            meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            MeshFilter meshFilter = territoryBorder.AddComponent<MeshFilter>();
            meshFilter.mesh = territoryBorderMesh;

            grid.Borders.Add(entry.Key, meshRenderer);
        }
    }

    #region HEX MESH CONSTRUCTION
    
    protected static Mesh GetHexMesh(float radius, float halfWidth, float skirtLength)
    {
        Vector3[] vertices = new Vector3[18];
        Vector3[] normals = new Vector3[18];
        Vector2[] UV = new Vector2[18];

        GetCapVertices(vertices, radius, halfWidth, skirtLength);
        GetSkirtVertices(vertices, radius, halfWidth, skirtLength);
        GetNormals(normals);
        GetCapUVs(UV);
        GetSkirtUVs(UV);

        Mesh hexMesh = new Mesh { name = "HexTile Mesh" };
        hexMesh.vertices = vertices;
        hexMesh.normals = normals;
        hexMesh.uv = UV;

        hexMesh.subMeshCount = 2;

        int[] triangles = GetCapTriangles();
        hexMesh.SetTriangles(triangles, 0);
        triangles = GetSkirtTriangles();
        hexMesh.SetTriangles(triangles, 1);

        return hexMesh;
    }

    protected static int[] GetCapTriangles()
    {
        return new int[]
        {
            1, 0, 5, 2, 4, 3, 2, 1, 4, 1, 5, 4//,
            //19, 23, 18, 20, 21, 22, 20, 23, 19, 20, 22, 23
        };
    }

    protected static int[] GetSkirtTriangles()
    {
        return new int[]
        {
            7, 12, 6, 7, 13, 12,
            8, 13, 7, 8, 14, 13,
            9, 14, 8, 9, 15, 14,
            10, 15, 9, 10, 16, 15,
            11, 16, 10, 11, 17, 16,
            6, 17, 11, 6, 12, 17
        };
    }

    protected static void GetCapVertices(Vector3[] vertices, float radius, float halfWidth, float skirtLength)
    {
        // TOP
        vertices[0] = new Vector3( radius / 2f, 0, -halfWidth);  // botRight
        vertices[1] = new Vector3( radius     , 0, 0);           // right
        vertices[2] = new Vector3( radius / 2f, 0, halfWidth);   // topright
        vertices[3] = new Vector3(-radius / 2f, 0, halfWidth);   // topleft
        vertices[4] = new Vector3(-radius     , 0, 0);           // left
        vertices[5] = new Vector3(-radius / 2f, 0, -halfWidth);  // botleft

        // BOT
        //vertices[18] = new Vector3( radius / 2f, -skirtLength, -halfWidth);  // botRight
        //vertices[19] = new Vector3( radius     , -skirtLength, 0);           // right
        //vertices[20] = new Vector3( radius / 2f, -skirtLength, halfWidth);   // topright
        //vertices[21] = new Vector3(-radius / 2f, -skirtLength, halfWidth);   // topleft
        //vertices[22] = new Vector3(-radius     , -skirtLength, 0);           // left
        //vertices[23] = new Vector3(-radius / 2f, -skirtLength, -halfWidth);  // botleft
    }

    protected static void GetSkirtVertices(Vector3[] vertices, float radius, float halfWidth, float skirtLength)
    {
        // TOP
        vertices[6]  = new Vector3( radius / 2f, 0, -halfWidth);    // botRight
        vertices[7]  = new Vector3( radius     , 0, 0);             // right
        vertices[8]  = new Vector3( radius / 2f, 0, halfWidth);     // topright
        vertices[9]  = new Vector3(-radius / 2f, 0, halfWidth);     // topleft
        vertices[10] = new Vector3(-radius     , 0, 0);             // left
        vertices[11] = new Vector3(-radius / 2f, 0, -halfWidth);    // botleft

        // BOT
        vertices[12] = new Vector3( radius / 2f, -skirtLength, -halfWidth); // botRight
        vertices[13] = new Vector3( radius     , -skirtLength, 0);          // right
        vertices[14] = new Vector3( radius / 2f, -skirtLength, halfWidth);  // topright
        vertices[15] = new Vector3(-radius / 2f, -skirtLength, halfWidth);  // topleft
        vertices[16] = new Vector3(-radius     , -skirtLength, 0);          // left
        vertices[17] = new Vector3(-radius / 2f, -skirtLength, -halfWidth); // botleft
    }

    protected static void GetNormals(Vector3[] normals)
    {
        normals[0]  = new Vector3(0, 1, 0);
        normals[1]  = new Vector3(0, 1, 0);
        normals[2]  = new Vector3(0, 1, 0);
        normals[3]  = new Vector3(0, 1, 0);
        normals[4]  = new Vector3(0, 1, 0);
        normals[5]  = new Vector3(0, 1, 0);

        normals[6]  = new Vector3( 1, 0,  0);
        normals[7]  = new Vector3( 1, 0,  1);
        normals[8]  = new Vector3(-1, 0,  1);
        normals[9]  = new Vector3(-1, 0,  0);
        normals[10] = new Vector3(-1, 0, -1);
        normals[11] = new Vector3( 1, 0, -1);

        normals[12] = new Vector3( 1, 0,  0);
        normals[13] = new Vector3( 1, 0,  1);
        normals[14] = new Vector3(-1, 0,  1);
        normals[15] = new Vector3(-1, 0,  0);
        normals[16] = new Vector3(-1, 0, -1);
        normals[17] = new Vector3( 1, 0, -1);

        //normals[18] = new Vector3(0, -1, 0);
        //normals[19] = new Vector3(0, -1, 0);
        //normals[20] = new Vector3(0, -1, 0);
        //normals[21] = new Vector3(0, -1, 0);
        //normals[22] = new Vector3(0, -1, 0);
        //normals[23] = new Vector3(0, -1, 0);
    }

    protected static void GetCapUVs(Vector2[] UV)
    {
        // TOP
        UV[0] = new Vector2(0.75f, 0.93f); // botRight
        UV[1] = new Vector2(1.0f , 0.5f);  // right
        UV[2] = new Vector2(0.75f, 0.07f); // topright
        UV[3] = new Vector2(0.25f, 0.07f); // topleft
        UV[4] = new Vector2(0    , 0.5f);  // left
        UV[5] = new Vector2(0.25f, 0.93f); // botleft

        //// BOT
        //UV[18] = new Vector2(0.735f, 0.97f);  // botRight
        //UV[19] = new Vector2(1     , 0.5f);   // right
        //UV[20] = new Vector2(0.735f, 0.04f);  // topright
        //UV[21] = new Vector2(0.265f, 0.04f);  // topleft
        //UV[22] = new Vector2(0     , 0.5f);   // left
        //UV[23] = new Vector2(0.265f, 0.97f);  // botleft
    }

    protected static void GetSkirtUVs(Vector2[] UV)
    {
        UV[6]  = new Vector2(0.99f, 0.99f); // botRight
        UV[7]  = new Vector2(0.01f, 0.99f); // right
        UV[8]  = new Vector2(0.99f, 0.99f); // topright
        UV[9]  = new Vector2(0.01f, 0.99f); // topleft
        UV[10] = new Vector2(0.99f, 0.99f); // left
        UV[11] = new Vector2(0.01f, 0.99f); // botleft

        UV[12] = new Vector2(0.99f, 0.01f); // botRight
        UV[13] = new Vector2(0.01f, 0.01f); // right
        UV[14] = new Vector2(0.99f, 0.01f); // topright
        UV[15] = new Vector2(0.01f, 0.01f); // topleft
        UV[16] = new Vector2(0.99f, 0.01f); // left
        UV[17] = new Vector2(0.01f, 0.01f); // botleft
    }

    #endregion

    #region OVERLAY MESH CONSTRUCTION

    protected static Mesh GetHexOverlayMesh(float radius, float halfWidth)
    {
        Vector3[] vertices = new Vector3[6];
        Vector3[] normals = new Vector3[6];
        Vector2[] UV = new Vector2[6];

        GetOverlayVertices(vertices, radius, halfWidth);
        GetOverlayNormals(normals);
        GetOverlayUVs(UV);

        Mesh hexOverLayMesh = new Mesh { name = "HexTileOverlay Mesh" };
        hexOverLayMesh.vertices = vertices;
        hexOverLayMesh.normals = normals;
        hexOverLayMesh.uv = UV;

        int[] triangles = GetOverlayCapTriangles();
        hexOverLayMesh.SetTriangles(triangles, 0);
        return hexOverLayMesh;
    }

    protected static int[] GetOverlayCapTriangles()
    {
        return new int[] { 1, 0, 5, 2, 4, 3, 2, 1, 4, 1, 5, 4 };
    }

    protected static void GetOverlayVertices(Vector3[] vertices, float radius, float halfWidth)
    {
        // Overlay
        vertices[0] = new Vector3( radius / 2, OverlayYPos, -halfWidth);  // botRight
        vertices[1] = new Vector3( radius    , OverlayYPos, 0);           // right
        vertices[2] = new Vector3( radius / 2, OverlayYPos, halfWidth);   // topright
        vertices[3] = new Vector3(-radius / 2, OverlayYPos, halfWidth);   // topleft
        vertices[4] = new Vector3(-radius    , OverlayYPos, 0);           // left
        vertices[5] = new Vector3(-radius / 2, OverlayYPos, -halfWidth);  // botleft
    }

    protected static void GetOverlayNormals(Vector3[] normals)
    {
        normals[0] = new Vector3(0, 1, 0);
        normals[1] = new Vector3(0, 1, 0);
        normals[2] = new Vector3(0, 1, 0);
        normals[3] = new Vector3(0, 1, 0);
        normals[4] = new Vector3(0, 1, 0);
        normals[5] = new Vector3(0, 1, 0);
    }

    protected static void GetOverlayUVs(Vector2[] UV)
    {
        // TOP
        UV[0] = new Vector2(0.75f, 0.93f); // botRight
        UV[1] = new Vector2(1.0f , 0.5f);  // right
        UV[2] = new Vector2(0.75f, 0.07f); // topright
        UV[3] = new Vector2(0.25f, 0.07f); // topleft
        UV[4] = new Vector2(0    , 0.5f);  // left
        UV[5] = new Vector2(0.25f, 0.93f); // botleft
    }

    #endregion

    #region BORDER MESH CONSTRUCTION

    protected static void BuildTerritoryMesh(HexGrid grid, ref Mesh territoryMesh, List<HexTile> territoryTiles, float radius, float territoryBorderYPos, float halfWidth)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> UV = new List<Vector2>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < territoryTiles.Count; i++)
        {
            HexTile tile = territoryTiles[i];
            HexTile neighbour = null;

            List<RelativeTilePosition> bordersAdded = new List<RelativeTilePosition>();

            for (int j = 0; j < (int)RelativeTilePosition.NbNeighbours; j++)
            {
                neighbour = GetTileNeighbour(grid, tile.Pos, (RelativeTilePosition)j);
                if (neighbour == null || neighbour.Owner != tile.Owner)
                {
                    AddBorderToTerritory(tile, (RelativeTilePosition)j, radius, territoryBorderYPos, halfWidth, ref vertices, ref normals, ref UV, ref triangles);
                    bordersAdded.Add((RelativeTilePosition)j);
                }
                neighbour = null;
            }

            for (int j = 0; j < bordersAdded.Count; j++)
            {
                switch (bordersAdded[j])
                {
                    case RelativeTilePosition.Bot:      AddSideBorders(RelativeTilePosition.BotLeft, RelativeTilePosition.BotRight, radius, territoryBorderYPos, halfWidth, bordersAdded, tile, ref vertices, ref normals, ref UV, ref triangles, true);  break;
                    case RelativeTilePosition.BotRight: AddSideBorders(RelativeTilePosition.Bot    , RelativeTilePosition.TopRight, radius, territoryBorderYPos, halfWidth, bordersAdded, tile, ref vertices, ref normals, ref UV, ref triangles, true);  break;
                    case RelativeTilePosition.TopRight: AddSideBorders(RelativeTilePosition.Top    , RelativeTilePosition.BotRight, radius, territoryBorderYPos, halfWidth, bordersAdded, tile, ref vertices, ref normals, ref UV, ref triangles, false); break;
                    case RelativeTilePosition.Top:      AddSideBorders(RelativeTilePosition.TopLeft, RelativeTilePosition.TopRight, radius, territoryBorderYPos, halfWidth, bordersAdded, tile, ref vertices, ref normals, ref UV, ref triangles, false); break;
                    case RelativeTilePosition.TopLeft:  AddSideBorders(RelativeTilePosition.Top    , RelativeTilePosition.BotLeft , radius, territoryBorderYPos, halfWidth, bordersAdded, tile, ref vertices, ref normals, ref UV, ref triangles, true);  break;
                    case RelativeTilePosition.BotLeft:  AddSideBorders(RelativeTilePosition.TopLeft, RelativeTilePosition.Bot     , radius, territoryBorderYPos, halfWidth, bordersAdded, tile, ref vertices, ref normals, ref UV, ref triangles, true);  break;
                }
            }
        }

        Vector3[] verticesArray = new Vector3[vertices.Count];
        Vector3[] normalsArray = new Vector3[normals.Count];
        Vector2[] UVArray = new Vector2[UV.Count];
        int[] trianglesArray = new int[triangles.Count];

        for (int i = 0; i < vertices.Count; i++)
            verticesArray[i] = vertices[i];
        for (int i = 0; i < normals.Count; i++)
            normalsArray[i] = normals[i];
        for (int i = 0; i < UV.Count; i++)
            UVArray[i] = UV[i];
        for (int i = 0; i < triangles.Count; i++)
            trianglesArray[i] = triangles[i];

        territoryMesh = new Mesh { name = "Territory Border Mesh" };
        territoryMesh.vertices = verticesArray;
        territoryMesh.normals = normalsArray;
        territoryMesh.uv = UVArray;
        territoryMesh.SetTriangles(trianglesArray, 0);
    }

    protected static HexTile GetTileNeighbour(HexGrid grid, Vector2 tilePos, RelativeTilePosition relPos)
    {
        Vector2 posToCheck = new Vector2();

        switch (relPos)
        {
            case RelativeTilePosition.Bot:
                posToCheck = new Vector2(tilePos.x, tilePos.y - 1);
                break;
            case RelativeTilePosition.BotRight:
                posToCheck = (tilePos.x % 2 == 0) ? new Vector2(tilePos.x + 1, tilePos.y - 1) : new Vector2(tilePos.x + 1, tilePos.y);
                break;
            case RelativeTilePosition.TopRight:
                posToCheck = (tilePos.x % 2 == 0) ? new Vector2(tilePos.x + 1, tilePos.y) : new Vector2(tilePos.x + 1, tilePos.y + 1);
                break;
            case RelativeTilePosition.Top:
                posToCheck = new Vector2(tilePos.x, tilePos.y + 1);
                break;
            case RelativeTilePosition.TopLeft:
                posToCheck = (tilePos.x % 2 == 0) ? new Vector2(tilePos.x - 1, tilePos.y) : new Vector2(tilePos.x - 1, tilePos.y + 1);
                break;
            case RelativeTilePosition.BotLeft:
                posToCheck = (tilePos.x % 2 == 0) ? new Vector2(tilePos.x - 1, tilePos.y - 1) : new Vector2(tilePos.x - 1, tilePos.y);
                break;
        }

        HexTile neighbour = null;
        grid.Tiles.TryGetValue(posToCheck, out neighbour);
        return neighbour;
    }

    protected enum TriangleUVTweak
    {
        NoTweak,
        TweakLeft,
        TweakRight
    }

    protected static void AddSideBorders(RelativeTilePosition left, RelativeTilePosition right, 
                                       float radius, float territoryBorderYPos, float halfWidth,
                                       List<RelativeTilePosition> bordersAdded, HexTile tile, 
                                       ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> UV, 
                                       ref List<int> triangles, bool invertUVs)
    {
        bool rightFound = false;
        bool leftFound = false;
        for (int k = 0; k < bordersAdded.Count; k++)
        {
            if (bordersAdded[k] == right)
                rightFound = true;
            else if (bordersAdded[k] == left)
                leftFound = true;
        }

        if (!leftFound)
        {
            if (invertUVs)
                AddBorderToTerritory(tile, left, radius, territoryBorderYPos, halfWidth, ref vertices, ref normals, ref UV, ref triangles, TriangleUVTweak.TweakLeft);
            else
                AddBorderToTerritory(tile, left, radius, territoryBorderYPos, halfWidth, ref vertices, ref normals, ref UV, ref triangles, TriangleUVTweak.TweakRight);
        }
        if (!rightFound)
        {
            if (invertUVs)
                AddBorderToTerritory(tile, right, radius, territoryBorderYPos, halfWidth, ref vertices, ref normals, ref UV, ref triangles, TriangleUVTweak.TweakRight);
            else
                AddBorderToTerritory(tile, right, radius, territoryBorderYPos, halfWidth, ref vertices, ref normals, ref UV, ref triangles, TriangleUVTweak.TweakLeft);
        }
    }

    protected static void AddBorderToTerritory(HexTile tile, RelativeTilePosition relPos, float radius, float territoryBorderYPos, float halfWidth,
                                             ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> UV, ref List<int> triangles, 
                                             TriangleUVTweak tweakUVs = TriangleUVTweak.NoTweak)
    {
        Vector3 worldPos = tile.Grid.ToPixel(tile.Pos);

        Vector3 botRight = new Vector3(worldPos.x + ( radius / 2), territoryBorderYPos, worldPos.z - halfWidth);   // botRight
        Vector3 right    = new Vector3(worldPos.x +   radius     , territoryBorderYPos, worldPos.z            );   // right
        Vector3 topRight = new Vector3(worldPos.x + ( radius / 2), territoryBorderYPos, worldPos.z + halfWidth);   // topRight 
        Vector3 topLeft  = new Vector3(worldPos.x + (-radius / 2), territoryBorderYPos, worldPos.z + halfWidth);   // topLeft
        Vector3 left     = new Vector3(worldPos.x + (-radius)    , territoryBorderYPos, worldPos.z            );   // left
        Vector3 botLeft  = new Vector3(worldPos.x + (-radius / 2), territoryBorderYPos, worldPos.z - halfWidth);   // botLeft
        Vector3 center   = new Vector3(worldPos.x                , territoryBorderYPos, worldPos.z            );   // center 

        Vector3 v1 = new Vector3();
        Vector3 v2 = new Vector3();
        Vector3 v3 = center;

        switch (relPos)
        {
            case RelativeTilePosition.Bot:      v1 = botLeft;  v2 = botRight; break;
            case RelativeTilePosition.BotRight: v1 = botRight; v2 = right;    break;
            case RelativeTilePosition.TopRight: v1 = right;    v2 = topRight; break;
            case RelativeTilePosition.Top:      v1 = topRight; v2 = topLeft;  break;
            case RelativeTilePosition.TopLeft:  v1 = topLeft;  v2 = left;     break;
            case RelativeTilePosition.BotLeft:  v1 = left;     v2 = botLeft;  break;
        }
        AddTriangle(v1, v2, v3, ref vertices, ref normals, ref UV, ref triangles, tweakUVs);
    }

    protected static void AddTriangle(Vector3 vertexA, Vector3 vertexB, Vector3 vertexC, 
                                    ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> UV, ref List<int> triangles, 
                                    TriangleUVTweak tweakUVs)
    {
        int verticeNum = vertices.Count;
        vertices.Add(vertexA);
        vertices.Add(vertexB);
        vertices.Add(vertexC);

        normals.Add(new Vector3(0, 1, 0));
        normals.Add(new Vector3(0, 1, 0));
        normals.Add(new Vector3(0, 1, 0));

        const float v = 0.5f;

        switch (tweakUVs)
        {
            case TriangleUVTweak.NoTweak:    { UV.Add(new Vector2(0, 1)); UV.Add(new Vector2(0, 1)); UV.Add(new Vector2(0, v)); break; }
            case TriangleUVTweak.TweakLeft:  { UV.Add(new Vector2(0, v)); UV.Add(new Vector2(0, 1)); UV.Add(new Vector2(0, v)); break; }
            case TriangleUVTweak.TweakRight: { UV.Add(new Vector2(0, 1)); UV.Add(new Vector2(0, v)); UV.Add(new Vector2(0, v)); break; }
        }

        triangles.Add(verticeNum);
        triangles.Add(verticeNum + 2);
        triangles.Add(verticeNum + 1);
    }
    #endregion
}
