/*
 * I intended for this script to just show compact and straight forward flocking code
 * but then i figured, someone might be interested in how to integrate a movement behaviour
 * into a larger set of behaviours that would all affect an agent's movement.
 * 
 * This is the basis for how movement works in my agent simulation at www.tombbonin.com/disastercraft
 * For just the flocking code, scroll down to the FlockingBehaviour function.
 * 
 * https://github.com/tombbonin
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This is a bitfield, it helps to store many states into one int, see below 
// how to add or query states
public enum Agent_Behaviour
{
    FLOCKING = 1 << 0
}

public class Agent
{
    public List<Agent> NearbyAgents;
    public Vector2 Pos;
    public Vector2 Velocity;
    public Vector2 Heading;
    public float Mass;
    public Transform Transform;

    public int Behaviours;

    public bool HasBehaviour(Agent_Behaviour behaviourToCheck)
    {
        return (Behaviours & (int)behaviourToCheck) != 0;
    }

    public void AddBehaviour(Agent_Behaviour behaviourToAdd)
    {
        Behaviours |= (int)behaviourToAdd;
    }

    public void RemoveBehaviour(Agent_Behaviour behaviourToRemove)
    {
        Behaviours &= (int)~behaviourToRemove;
    }
}


public class AgentSimulation_Movement 
{
    // Weights and Parameters you'll endlessly tweak until you get movement patterns that you like
    private const float BehaviourWeight_Flocking = 1f;
    private const float FlockingWeight_Cohesion   = 2f;
    private const float FlockingWeight_Alignment  = 3.5f;
    private const float FlockingWeight_Separation = 5f;


    private const float MaxSteeringForce = 100f;

    // our agent container
    List<Agent> _agents;

    // Setup
    // - Create Agents, Assign behaviours to them, add them to the container

    void Execute(float deltaTime)
    {
        for(int i = 0; i < _agents.Count; i++)
        {
            Agent agent = _agents[i];
            UpdateNearbyAgents(ref agent);

            PerformMovementBehaviours(ref agent, deltaTime);

            // Update agent object's position and rotation, and save his new world position
            agent.Transform.Translate(new Vector3(agent.Velocity.x, 0, agent.Velocity.y) * deltaTime);
            agent.Transform.LookAt(new Vector3(agent.Pos.x, 1, agent.Pos.y) + new Vector3(agent.Heading.x, 0, agent.Heading.y));
            agent.Pos = new Vector2(agent.Transform.position.x, agent.Transform.position.z);
        }
    }

    void UpdateNearbyAgents(ref Agent agent)
    {
        // look for nearby agents, add them to agent.NearbyAgents;
        // Will depend on your data structure
    }

    static void PerformMovementBehaviours(ref Agent agent, float deltaTime)
    {
        // Calculate Steering Force based on current behaviours
        Vector2 steeringForce = CalculatePrioritizedSteeringForce(ref agent, deltaTime);
        Vector2 acceleration = steeringForce / agent.Mass;
        Vector2 agentVelocity = agent.Velocity + (acceleration * deltaTime);

        agentVelocity.x = Mathf.Clamp(agentVelocity.x, -Agent_MaxSpeed, Agent_MaxSpeed);
        agentVelocity.y = Mathf.Clamp(agentVelocity.y, -Agent_MaxSpeed, Agent_MaxSpeed);

        agent.Velocity = agentVelocity;

        if (agentVelocity.SqrMagnitude() > 0.00001)
            agent.Heading = agentVelocity.normalized;
    }

    // Perform behaviours one by one if the Agent has them, and until the max steering force is reached.
    // The weight assigned to each behaviour and its position in the list are important
    static Vector2 CalculatePrioritizedSteeringForce(ref Agent agent, float deltaTime)
    {
        Vector2 steeringForce = new Vector2();
        Vector2 force = new Vector2();

        if (agent.HasBehaviour(Agent_Behaviour.FLOCKING))
        {
            force = FlockingBehaviour(ref agent) * BehaviourWeight_Flocking;
            if (!AccumulateForce(ref steeringForce, force)) return steeringForce;
        }

        //if (agent.HasBehaviour(Agent_Behaviour.BEHAVIOUR_2))
        //{
        //    force = 2ndBehaviour(ref agent) * BehaviourWeight_Behaviour2;
        //    if (!AccumulateForce(ref steeringForce, force)) return steeringForce;
        //}

        //if (agent.HasBehaviour(Agent_Behaviour.BEHAVIOUR_3))
        //{
        //    force = 3rdBehaviour(ref agent) * BehaviourWeight_Behaviour2;
        //    if (!AccumulateForce(ref steeringForce, force)) return steeringForce;
        //}

        return steeringForce;
    }

    // Adds the steering force and checks if the max has been reached
    static bool AccumulateForce(ref Vector2 totalSteeringForce, Vector2 forceToAdd)
    {
        float MagnitudeRemaining = MaxSteeringForce - totalSteeringForce.magnitude;

        if (MagnitudeRemaining <= 0.0) return false;

        float MagnitudeToAdd = forceToAdd.magnitude;

        if (MagnitudeToAdd < MagnitudeRemaining)
            totalSteeringForce += forceToAdd;
        else
            totalSteeringForce += forceToAdd.normalized * MagnitudeRemaining;

        return true;
    }

    // MOVEMENT BEHAVIOURS

    // All 3 key flocking aspects all rolled into one function,
    // for how it works youll find endless information online
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

        steeringForce += FlockingWeight_Cohesion   * centerMassSteeringForce.normalized;
        steeringForce += FlockingWeight_Alignment  * averageHeading;
        steeringForce += FlockingWeight_Separation * moveAwayForce;

        return steeringForce;
    }

    // MOVEMENT BEHAVIOUR HELPERS

    static Vector2 FleeFromPos(ref Agent agent, Vector2 posToFleeFrom)
    {
        Vector2 nextPos = agent.Pos - posToFleeFrom;
        Vector2 desiredVelocity = nextPos.normalized * Agent_MaxSpeed;
        return (desiredVelocity - agent.Velocity);
    }

    static Vector2 GoTowardsPos(ref Agent agent, Vector2 posToGoTowards)
    {
        Vector2 posDif = posToGoTowards - agent.Pos;
        Vector2 desiredVelocity = posDif.normalized * Agent_MaxSpeed;
        return (desiredVelocity - agent.Velocity);
    }
}
