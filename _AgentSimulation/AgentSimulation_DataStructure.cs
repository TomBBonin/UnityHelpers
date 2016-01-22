/*
 * This Script will show you how I implemented my Grid and run through it,
 * in my Agent Simulation at tombbonin.com/disastercraft with a Data Oriented approach.
 *  
 * The single greatest performance increase I got in this project was switching from
 * letting Unity handle the simulation (Agents as objects, with their code running in their update)
 * to handling all of the simulation myself with a data oriented approach and only using Unity 
 * as a graphic renderer, agents being only transforms for which i updated the positions. 
 * This increased the amount of agents i can simulate in real time from 200 to 4000.
 * 
 * Below you'll find key concepts that helped do that.
 * Implementing a Grid was vital as instead of having each agent check every other agent,
 * each agent would now only check a small area around it. Read on to see how that Grid is built
 * efficiently, and how agents can quickly check their surroundings.
 * 
 * https://github.com/tombbonin
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Our main Agent class. This can become a huge bloated and complex class of fields upon fields
// of data. This is not ideal for Data Oriented programming, because the bigger the object is, 
// the less you can load into a cache line, meaning that it will have to load many lines, which
// is super slow! We'll fix that with ...
public class Agent
{
    public Vector2 Pos;
    public float   ViewDistance; 
    // Fields * 100
}

// A simplified data structure that only contains the vital information we need to locate agents
// in our grid. Thousands of these can be loaded into a single cache line, yay efficiency.
// It only contains the agent's position, his index in _agents, and the next Agent's index in _allAgentsCellData;
struct CellData
{
    public Vector2 agentPos;
    public uint agentIndex;
    public uint nextAgentIndex;
};

public class AgentSimulation_DataStructure 
{
    private const float CellSize = 24.0f;
    private const float MaxNeighboursToEvaluate = 24.0f;

    // Our list of logical Agents
    List<Agent> _agents;

    // Our Main Data structure the Grid. It maps grid coordinates to a linked list of CellData.
    // Which is basically every agent in that cell. 
    Dictionary<Vector2, CellData> _grid;

    // Our list of all the CellDatas (which houses the linkedLists of CellData referenced by the _grid)
    List<CellData> _allAgentsCellData;

    // Instead of recreating this list for every agent, it is faster to create it once, size it and then simply clear it
    // for each agent
    List<Agent> _nearbyAgents;

    void Init()
    {
        // Create and Add logical Agents to this list
        _agents = new List<Agent>();

        // Initialize NearbyAgents to a reasonable capacity
        _nearbyAgents = new List<Agent>();
        _nearbyAgents.Capacity = _agents.Count / 10;

        // Initialize AgentsCellData to the amount of agents
        _allAgentsCellData = new List<CellData>();
        _allAgentsCellData.Capacity = _agents.Count;

        // Initialize Grid
        _grid = new Dictionary<Vector2, CellData>();
    }

    void Execute(float deltaTime)
    {
        // We Rebuild our grid at every frame
        BuildGrid();

        // We update and process every agent. A good way to gain performance is not updating
        // all of the agents at every frame but only a fraction of them. However every frame lost 
        // will deminish agent's reactivity to new events, so i don't recommend staggering over more than 2-3 frames
        for (int i = 0; i < _agents.Count; i++)
        {
            Agent agent = _agents[i];
            GetNearbyAgents(ref agent, i);

            // Process Agent behaviour
        }
    }

    // Rebuilding the Grid fast is extremely important as it is one of the more costly operations when simulating 
    // thousands of Agents. First we clear it, then, for each agent :
    // - Create a new cell data pointing to this agent, containing his position and index in the main Agents list
    // - We look in the Grid to see if any agents are already mapped to this cell (if a linked list has been started in this cell)
    //     - if there are, we add this new agent to the front of that list and link him up, and add him to our list of all cellDatas
    //     - if not, this agent becomes the first element of the linked list in this cell
    // The cool thing with this approach is that, if there are no agents in a given cell, it technically doesnt exist, so it 
    // isnt hogging up any extra memory, and speeds up access time!
    private void BuildGrid()
    {
        _grid.Clear();
        _allAgentsCellData.Clear();

        for (int i = 0; i < _agents.Count; i++)
        {
            Agent agent = _agents[i];
            CellData cellData;
            cellData.agentIndex = (uint)i;
            cellData.nextAgentIndex = uint.MaxValue;
            cellData.agentPos = agent.Pos;

            // translate agent position to grid coordinates
            Vector2 agentGridCell = cellData.agentPos;
            agentGridCell.x = Mathf.Floor(agentGridCell.x / CellSize);
            agentGridCell.y = Mathf.Floor(agentGridCell.y / CellSize);

            CellData firstAgentInCell;
            if (_grid.TryGetValue(agentGridCell, out firstAgentInCell))
            {
                cellData.nextAgentIndex = firstAgentInCell.nextAgentIndex;       // Assign oldFirstAgent's Next to new Agent's next
                firstAgentInCell.nextAgentIndex = (uint)_allAgentsCellData.Count;// Assign nbAgentsInThisCell as next to oldFirstAgent
                _grid[agentGridCell] = firstAgentInCell;                         // Copy the temp cellData back into first (having changed its next index)
                _allAgentsCellData.Add(cellData);                                // Add new Cell Data to _allAgentsCellData
            }
            else
                _grid[agentGridCell] = cellData;   // Assign this cell data to gridPosition
        }
    }

    // Now that the Grid containing all of our Agents has been created, iterating through it rapidly is extremely important.
    // Each agent will need to find his nearby agents so its important to not cripple performance, for this to be extremely efficient.
    // To do that, instead of checking the entire grid and seeing who's in range, we adapted the grid's resolution to match the agent's 
    // view distance. This lets us know that regardless of where he is, we will never check more than 4 grid tiles.
    // Based on his position, and where he is within his tile, we get the 4 cells to check, then  ... (see below)
    private void GetNearbyAgents(ref Agent agent, int agentIndex)
    {
        _nearbyAgents.Clear();
        float viewDistanceSquared = agent.ViewDistance * agent.ViewDistance;

        // translate agent position to grid coordinates
        Vector2 agentPos = agent.Pos;
        Vector2 agentGridCell;
        agentGridCell.x = Mathf.Floor(agentPos.x / CellSize);
        agentGridCell.y = Mathf.Floor(agentPos.y / CellSize);

        bool right = (agentPos.x > ((agentGridCell.x * CellSize) + (0.5f * CellSize))) ? true : false; // on the right of the cell
        bool top   = (agentPos.y > ((agentGridCell.y * CellSize) + (0.5f * CellSize))) ? true : false; // on the top of the cell

        Vector2[] cellsToCheck = new Vector2[3];
        GetAdjacentCellsTo(agentGridCell, ref cellsToCheck, top, right);

        CheckNearbyAgentsInGivenCell(agentIndex, agentPos, agentGridCell, viewDistanceSquared);
        CheckNearbyAgentsInGivenCell(agentIndex, agentPos, cellsToCheck[0], viewDistanceSquared);
        CheckNearbyAgentsInGivenCell(agentIndex, agentPos, cellsToCheck[1], viewDistanceSquared);
        CheckNearbyAgentsInGivenCell(agentIndex, agentPos, cellsToCheck[2], viewDistanceSquared);
    }

    // For each cell, we run through it's associated linked list and test if each agent's position is within view distance
    // If it is we add it to the list!
    private void CheckNearbyAgentsInGivenCell(int agentIndex, Vector2 agentPos, Vector2 cellCoord, float viewDistanceSquared)
    {
        CellData currCellData;
        if (_grid.TryGetValue(cellCoord, out currCellData))
        {
            while (currCellData.nextAgentIndex != uint.MaxValue)
            {
                if (agentIndex != currCellData.agentIndex)
                {
                    float distanceSquared = MyMath.GetSquaredDistance(agentPos, currCellData.agentPos);
                    if (distanceSquared < viewDistanceSquared)
                    {
                        _nearbyAgents.Add(_agents[(int)currCellData.agentIndex]);
                        if (_nearbyAgents.Count > MaxNeighboursToEvaluate)
                            return;
                    }
                }
                currCellData = _allAgentsCellData[(int)currCellData.nextAgentIndex];
            }
        }
        else
            return;
    }

    // Based off of where the agent is in his cell, we know which surrounding cells we need to check
    private void GetAdjacentCellsTo(Vector2 agentGridCell, ref Vector2[] adjacentCells, bool top, bool right)
    {
        Vector2 tempGridCell = agentGridCell;
        if (!top && !right) // Agent is bottom left
        {
            tempGridCell.x = agentGridCell.x - 1;
            tempGridCell.y = agentGridCell.y;
            adjacentCells[0] = tempGridCell;

            tempGridCell.x = agentGridCell.x - 1;
            tempGridCell.y = agentGridCell.y - 1;
            adjacentCells[1] = tempGridCell;

            tempGridCell.x = agentGridCell.x;
            tempGridCell.y = agentGridCell.y - 1;
            adjacentCells[2] = tempGridCell;
        }
        else if (!top && right) // agent is bottom Right
        {
            tempGridCell.x = agentGridCell.x + 1;
            tempGridCell.y = agentGridCell.y;
            adjacentCells[0] = tempGridCell;

            tempGridCell.x = agentGridCell.x + 1;
            tempGridCell.y = agentGridCell.y - 1;
            adjacentCells[1] = tempGridCell;

            tempGridCell.x = agentGridCell.x;
            tempGridCell.y = agentGridCell.y - 1;
            adjacentCells[2] = tempGridCell;
        }
        else if (top && !right) // agent is top left
        {
            tempGridCell.x = agentGridCell.x - 1;
            tempGridCell.y = agentGridCell.y;
            adjacentCells[0] = tempGridCell;

            tempGridCell.x = agentGridCell.x - 1;
            tempGridCell.y = agentGridCell.y + 1;
            adjacentCells[1] = tempGridCell;

            tempGridCell.x = agentGridCell.x;
            tempGridCell.y = agentGridCell.y + 1;
            adjacentCells[2] = tempGridCell;
        }
        else if (top && right) // agent is top right
        {
            tempGridCell.x = agentGridCell.x + 1;
            tempGridCell.y = agentGridCell.y;
            adjacentCells[0] = tempGridCell;

            tempGridCell.x = agentGridCell.x + 1;
            tempGridCell.y = agentGridCell.y + 1;
            adjacentCells[1] = tempGridCell;

            tempGridCell.x = agentGridCell.x;
            tempGridCell.y = agentGridCell.y + 1;
            adjacentCells[2] = tempGridCell;
        }
    }
}

public class MyMath
{
    // We use Distance squared, because actual distance would require an extra root operation. For our purposes
    // we dont care that the distance isnt real, as long as we're comparing it to an other squared distance!
    public static float GetSquaredDistance(Vector2 pointA, Vector2 pointB)
    {
        return (((pointA.x - pointB.x) * (pointA.x - pointB.x)) + ((pointA.y - pointB.y) * (pointA.y - pointB.y)));
    }
}