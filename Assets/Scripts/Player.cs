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
    private Vector2 currentTargetWaypoint;
    public GameManager managerBud;

    public const int CLOSEST_PLAYERS_CHECKED = 3;
    public (int, GameObject)[] nClosetsPlayers;
    public GameObject friendliestPlayer;



    public override void OnEpisodeBegin()
    {
        playerStatuses = new Dictionary<int, float[]>();
        timeUntilNextAction = UnityEngine.Random.Range(nextActionRange.min, nextActionRange.max);
        currentAction = PlayerActions.MoveRandomly;
        currentTargetWaypoint = new Vector2(transform.position.x, transform.position.y);
        nClosetsPlayers = new (int, GameObject)[CLOSEST_PLAYERS_CHECKED];
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

    // will do later, not much of a point doing it right now
    private void SetNClosestPlayers()
    {
        nClosetsPlayers = new (int, GameObject)[CLOSEST_PLAYERS_CHECKED];

        foreach(int plrId in playerStatuses.Keys)
        {
            if(plrId == id)
            {
                continue;
            }
            GameObject plrObj = managerBud.getPlayerObjFromId(plrId);
            string loopingStatus = "Placing";

            (int, GameObject) tempObj = (-1, null);
            for(int i = 0; i < nClosetsPlayers.Length; i++)
            {
                (int, GameObject) arrayObj = nClosetsPlayers[i];
                if(loopingStatus == "Placing")
                {
                    if(arrayObj.Item2 == null)
                    {
                        nClosetsPlayers[i] = (plrId, plrObj);
                        break;
                    }
                    if((transform.position - plrObj.transform.position).sqrMagnitude < (transform.position - arrayObj.Item2.transform.position).sqrMagnitude)
                    {
                        loopingStatus = "Propagating";
                        tempObj = arrayObj;
                        nClosetsPlayers[i] = (plrId, plrObj);
                    }
                }
                else
                {
                    (int, GameObject) tempTempObj = tempObj;
                    tempObj = arrayObj;
                    nClosetsPlayers[i] = tempTempObj;
                }
            }
        }
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
        switch(currentAction)
        {
            case PlayerActions.FollowTrustedPlayer:
                break;
            default:
                break;
        }
    }
}