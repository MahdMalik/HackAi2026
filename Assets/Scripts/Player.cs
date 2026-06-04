using System;
using System.Collections.Generic;
using Unity.MLAgents;
using System.Linq;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Analytics;

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
    public const int playerSpeed = 4;
    public const int distanceForRandomMovement = 3;
    public const float dangerThreshold = 0.45f;
    
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
    public List<GameObject> hostilestPlayers;
    public GameObject unsurestPlayer;
    private bool actionJustChanged;


    public override void OnEpisodeBegin()
    {
        playerStatuses = new Dictionary<int, float[]>();
        timeUntilNextAction = UnityEngine.Random.Range(nextActionRange.min, nextActionRange.max);
        currentAction = PlayerActions.MoveRandomly;
        currentTargetWaypoint = new Vector2(transform.position.x, transform.position.y);
        actionJustChanged = false;
        nClosestPlayers = new (int, GameObject)[CLOSEST_PLAYERS_CHECKED];
        SetNClosestPlayers();
        hostilestPlayers = new List<GameObject>();
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

    private void SetFriendliestPlayer()
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
    private void SetDangerousestPlayers()
    {
        hostilestPlayers = new List<GameObject>();
        foreach((int, GameObject) player in nClosestPlayers)
        {
            if(currentRole == PlayerRoles.Civillian || currentRole == PlayerRoles.Hero)
            {
                float hostileProb = playerStatuses[player.Item1][(int) PlayerRoles.Killer];
                if(hostileProb > dangerThreshold)
                {
                    hostilestPlayers.Add(player.Item2);
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

    void SetMostUnknownPlayer()
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

            SetNClosestPlayers();
            SetFriendliestPlayer();
            SetDangerousestPlayers();
            SetMostUnknownPlayer();

            currentAction = (PlayerActions) UnityEngine.Random.Range(0, Enum.GetNames(typeof(PlayerActions)).Length);
            actionJustChanged = true;
        }
        
        switch(currentAction)
        {
            case PlayerActions.FollowTrustedPlayer:
                currentTargetWaypoint = friendliestPlayer.transform.position;
                break;
            case PlayerActions.MoveRandomly:
                if(Vector2.Distance(transform.position, currentTargetWaypoint) < 0.01f || actionJustChanged)
                {
                    float directionAngle = UnityEngine.Random.Range(0f, 360f);
                    Vector2 addedVector = new Vector2(Mathf.Cos(directionAngle * Mathf.Deg2Rad) * distanceForRandomMovement, Mathf.Cos(directionAngle * Mathf.Deg2Rad)  * distanceForRandomMovement);
                    currentTargetWaypoint = (Vector2) transform.position + addedVector;
                }
                break;
            case PlayerActions.FleeFromDanger:
                if(actionJustChanged)
                {
                    SetDangerousestPlayers();
                    if(hostilestPlayers.Count == 0)
                    {
                        currentAction = PlayerActions.MoveRandomly;
                        return;
                    }
                }
                Vector2 directionToRun = new Vector2();
                if(hostilestPlayers.Count == 1)
                {
                    directionToRun = transform.position - hostilestPlayers[0].transform.position;
                }
                else if(hostilestPlayers.Count == 2)
                {
                    // directionToRun = 
                }

                break;
            default:
                break;
        }
        transform.position = Vector2.MoveTowards(transform.position, currentTargetWaypoint, playerSpeed * Time.deltaTime);
        actionJustChanged = false;
    }
}