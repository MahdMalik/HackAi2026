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
    public (int, GameObject)[] nClosestPlayers;
    public GameObject friendliestPlayer;
    public GameObject hostilestPlayer;
    public GameObject unsurestPlayer;



    public override void OnEpisodeBegin()
    {
        playerStatuses = new Dictionary<int, float[]>();
        timeUntilNextAction = UnityEngine.Random.Range(nextActionRange.min, nextActionRange.max);
        currentAction = PlayerActions.MoveRandomly;
        currentTargetWaypoint = new Vector2(transform.position.x, transform.position.y);
        nClosestPlayers = new (int, GameObject)[CLOSEST_PLAYERS_CHECKED];
        SetnClosestPlayers();
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
    private void SetnClosestPlayers()
    {
        nClosestPlayers = new (int, GameObject)[CLOSEST_PLAYERS_CHECKED];

        foreach(int plrId in playerStatuses.Keys)
        {
            if(plrId == id)
            {
                continue;
            }
            GameObject plrObj = managerBud.getPlayerObjFromId(plrId);
            string loopingStatus = "Placing";

            (int, GameObject) tempObj = (-1, null);
            for(int i = 0; i < nClosestPlayers.Length; i++)
            {
                (int, GameObject) arrayObj = nClosestPlayers[i];
                if(loopingStatus == "Placing")
                {
                    if(arrayObj.Item2 == null)
                    {
                        nClosestPlayers[i] = (plrId, plrObj);
                        break;
                    }
                    if((transform.position - plrObj.transform.position).sqrMagnitude < (transform.position - arrayObj.Item2.transform.position).sqrMagnitude)
                    {
                        loopingStatus = "Propagating";
                        tempObj = arrayObj;
                        nClosestPlayers[i] = (plrId, plrObj);
                    }
                }
                else
                {
                    (int, GameObject) tempTempObj = tempObj;
                    tempObj = arrayObj;
                    nClosestPlayers[i] = tempTempObj;
                }
            }
        }
    }

    private void ReturnFriendliestPlayer()
    {
        float maxFriendlyProb = -1;
        foreach((int, GameObject) player in nClosestPlayers)
        {
            if(currentRole == PlayerRoles.Civillian || currentRole == PlayerRoles.Hero)
            {
                float friendlyProb = playerStatuses[player.Item1][(int) PlayerRoles.Civillian] + playerStatuses[player.Item1][(int) PlayerRoles.Hero];
                if(friendlyProb > maxFriendlyProb)
                {
                    friendliestPlayer = player.Item2;
                    maxFriendlyProb = friendlyProb;
                }
            }
        }
    }

    // dangerousest!
    private void ReturnDangerousestPlayer()
    {
        float maxHostileProb = -1;
        foreach((int, GameObject) player in nClosestPlayers)
        {
            if(currentRole == PlayerRoles.Civillian || currentRole == PlayerRoles.Hero)
            {
                float hostileProb = playerStatuses[player.Item1][(int) PlayerRoles.Killer];
                if(hostileProb > maxHostileProb)
                {
                    hostilestPlayer = player.Item2;
                    maxHostileProb = hostileProb;
                }
            }
        }
    }

    float CalculateEntropy(float[] probabilities)
    {
        float entropy = 0;
        for(int i = 0; i < probabilities.Length; i++)
        {
            entropy -= probabilities[i] * math.log2(probabilities[i]);
        }
        return entropy;
    }

    void ReturnMostUnknownPlayer()
    {
        float maxEntropy = -1;
        foreach((int, GameObject) player in nClosestPlayers)
        {
            float entropy = CalculateEntropy(playerStatuses[player.Item1]);
            if(entropy > maxEntropy)
            {
                unsurestPlayer = player.Item2;
                maxEntropy = entropy;
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