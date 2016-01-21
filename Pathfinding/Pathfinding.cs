/*
 * Example code for pathfinding, to be modified to better fit your structure 
 * for better efficiency. This was taken from my HexGridBuilder project.
 * Credit to Red Blob sor being an invaluable source.
 * 
 * https://github.com/tombbonin
 * 
 * ----------------
 * Red Blob links :
 * http://www.redblobgames.com/grids/hexagons/
 * http://www.redblobgames.com/pathfinding/a-star/implementation.html#csharp
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

public enum TileAccessibility
{
    Accessible,
    Blocked
}

public class MyTileClass
{
    public TileAccessibility Accessibility;
}

public class Pathfinding : MonoBehaviour 
{
    // Your tile container, or grid
    Dictionary<Vector2, MyTileClass> Tiles;

    public void GetTileNeighbours(out List<Vector2> neighbours, Vector2 pos)
    {
        neighbours = new List<Vector2>();

        // Your grid specific code to get the neighbours around the tile at "pos"
    }

    // One of the fastest all around algorithms to get the shortest path from A to B
    public void Pathfind_AStar(Vector2 start, Vector2 end, out List<Vector2> aStarPath)
    {
        aStarPath = new List<Vector2>();
        Dictionary<Vector2, Vector2> cameFrom = new Dictionary<Vector2, Vector2>();
        Dictionary<Vector2, int> costSoFar = new Dictionary<Vector2, int>();
        PriorityQueue<Vector2> frontier = new PriorityQueue<Vector2>();
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

    // Much slower then the A* for a single path, however if you have many agents looking for an exit
    // mapping out the entire world once and then having each agent check it will be much, much faster
    // than performing many A*. 
    public void Pathfind_FloodFill(Vector2 start, out Dictionary<Vector2, Vector2> cameFrom)
    {
        cameFrom = new Dictionary<Vector2, Vector2>();
        Dictionary<Vector2, int> costSoFar = new Dictionary<Vector2, int>();

        // for multiple flood points (so Agents seek the closest "evac point" for example)
        // stick everything below here in a loop, updating the start point at each iteration

        PriorityQueue<Vector2> frontier = new PriorityQueue<Vector2>();
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

    // The heuristic used for the A* Algorithm
    private float Evaluate(Vector2 posA, Vector2 posB)
    {
        return (Mathf.Abs(posA.x - posB.x) + Mathf.Abs(posA.y - posB.y));
    }

    // You can attribute a move cost to tiles, 
    // By adding other enums to TileAccesibility. This would allow
    // for a higher move cost on Sand or Water for example
    // You can also make the cost different for accessibility transitions
    // for example if tileA is ground and tileB is water, then the transition is
    // costly but once you're on water then the cost is low again
    private int GetMoveCost(Vector2 tileAPos, Vector2 tileBPos)
    {
        switch (Tiles[tileBPos].Accessibility)
        {
            case TileAccessibility.Accessible: return 1;
            case TileAccessibility.Blocked: return 9999; // We could avoid adding the tile to neighbours, but handling it this way avoids spreading code
        }

        return int.MaxValue;
    }   
}
