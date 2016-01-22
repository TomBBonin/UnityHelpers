/*
 * Extracted from the AgentSimulation_Movement Script
 * This shows just the code for flocking. If you want to see
 * how to set up mutliple movement behaviours, check it out.
 * 
 * This flocking was used in my Agent Simulation at www.tombbonin.com/disastercraft
 * 
 * https://github.com/tombbonin
 */

using UnityEngine;

public class Flocking 
{
    private const float FlockingWeight_Cohesion = 2f;
    private const float FlockingWeight_Alignment = 3.5f;
    private const float FlockingWeight_Separation = 5f;
    private const float Agent_MaxSpeed = 15f;

    static Vector2 FlockingBehaviour(ref Agent agent)
    {
        Vector2 agentPos = agent.Pos;
        Vector2 steeringForce = new Vector2();
        Vector2 moveAwayForce = new Vector2();
        Vector2 averageHeading = new Vector2();
        Vector2 centerMass = new Vector2();
        Vector2 centerMassSteeringForce = new Vector2();

        int nbAgentsNearbyToFlockWith = 0;

        for (int i = 0; i < agent.NearbyAgents.Count; i++)
        {
            Agent nearbyAgent = agent.NearbyAgents[i];
            // Move away from Neighbours
            Vector2 toAgent = agentPos - nearbyAgent.Pos;
            moveAwayForce += toAgent.normalized / toAgent.magnitude;

            // Align heading
            averageHeading += nearbyAgent.Heading;

            // Move Towards Center Mass
            centerMass += nearbyAgent.Pos;

            ++nbAgentsNearbyToFlockWith;
        }

        if (nbAgentsNearbyToFlockWith > 0)
        {
            // average Heading
            averageHeading /= nbAgentsNearbyToFlockWith;

            // Average Center Mass
            centerMass /= nbAgentsNearbyToFlockWith;
            centerMassSteeringForce = GoTowardsPos(ref agent, centerMass);
        }

        steeringForce += FlockingWeight_Cohesion * centerMassSteeringForce.normalized;
        steeringForce += FlockingWeight_Alignment * averageHeading;
        steeringForce += FlockingWeight_Separation * moveAwayForce;

        return steeringForce;
    }

    static Vector2 GoTowardsPos(ref Agent agent, Vector2 posToGoTowards)
    {
        Vector2 posDif = posToGoTowards - agent.Pos;
        Vector2 desiredVelocity = posDif.normalized * Agent_MaxSpeed;
        return (desiredVelocity - agent.Velocity);
    }
}
