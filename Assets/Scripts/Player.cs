using System;
using System.Collections.Generic;
using Unity.MLAgents;
using System.Linq;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Unity.Mathematics;

public enum PlayerRoles
{
    Civillian,
    Killer,
    Hero
} 


public enum PlayerActions
{
    FollowTrustedPlayer,
    MoveRandomly,
    FleeFromDanger,
    UseMeatshield,
    InvestigatePlayer,
    ChargeAway
}

public class Player : Agent
{
    public readonly (int min, int max) nextActionRange = (1, 4);
    
    public int id;
    public Dictionary<int, float[]> playerStatuses;
    private float timeUntilNextAction;
    private PlayerActions currentAction;

    private PlayerRoles currentRole;

    public override void OnEpisodeBegin()
    {
        playerStatuses = new Dictionary<int, float[]>();
        timeUntilNextAction = UnityEngine.Random.Range(nextActionRange.min, nextActionRange.max);
        currentAction = PlayerActions.MoveRandomly;
    }


    public override void CollectObservations(VectorSensor sensor)
    {
        
    }


    protected override void Awake()
    {
        base.Awake();
        OnEpisodeBegin();
    }

    public void SetRole(PlayerRoles theRole)
    {
        currentRole = theRole;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        
    }

    void Update()
    {
        timeUntilNextAction -= Time.deltaTime;
        if(timeUntilNextAction < 0)
        {
            timeUntilNextAction = UnityEngine.Random.Range(nextActionRange.min, nextActionRange.max);
            // RequestDecision();
            currentAction = (PlayerActions) UnityEngine.Random.Range(0, Enum.GetNames(typeof(PlayerActions)).Length);
        }
    }
}