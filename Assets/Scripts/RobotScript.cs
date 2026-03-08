using System.Collections.Generic;
using UnityEngine;

public class RobotScript : MonoBehaviour
{
    public GameObject manager;
    protected GameManager managerScript;
    
    public string currentAction = "Move Random";
    public string lastAction = "Move Random";
    public int robotId;
    public string agentType; // "Killer", "Survivor", or "Hero"
    
    public Dictionary<int, string> alignments = new Dictionary<int, string>();

    
    public RobotScript closestBot = null;
    public RobotScript secondClosestBot = null;
    
    // Action history for reward calculation (stores last 2 actions with their timestep)
    public List<ActionRecord> actionHistory = new List<ActionRecord>();
    public struct ActionRecord
    {
        public int actionId;
        public float timestamp;
    }
    
    // Tracking for observations
    public bool isAlive = true;
    public int killCount = 0;
    public float lastActionTime = 0f;
    
    // Screen bounds for keeping agents visible
    private Camera mainCamera;
    private Vector2 screenBounds;
    private float boundsPadding = 0.5f; // padding from screen edge
    
    // Simple movement tracking so we can reason about
    // who was moving toward/away from whom on collisions.
    private Vector3 lastPosition;
    public Vector2 lastMoveDirection = Vector2.zero;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    // we also call it from Awake to reduce race conditions
    protected void DoStart()
    {
        if (manager != null && managerScript == null)
        {
            managerScript = manager.GetComponent<GameManager>();
        }
        else if (manager == null)
        {
            Debug.Log("CONDUCTOR WE HAVE A PROBLEM!");
        }
        
        // Initialize camera and screen bounds
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        if (mainCamera != null)
        {
            float height = mainCamera.orthographicSize * 2f;
            float width = height * mainCamera.aspect;
            screenBounds = new Vector2(width / 2f, height / 2f);
        }
        
        lastPosition = transform.position;
    }

    void Awake()
    {
        // try to populate managerScript early; manager may be assigned later by initializer
        DoStart();
    }

    // Update is called once per frame
    protected void DoUpdate()
    {
        if (!isAlive) return;
        
        // Track previous position so we can derive a movement vector
        Vector3 prevPos = transform.position;
        
        // Keep nearest bots up to date even if the agent isn't currently requesting
        UpdateNearestBots();
        
        switch (currentAction)
        {
            case "Move Random":
                float randomXVel = Random.Range(-Constants.speed, Constants.speed) * Time.deltaTime;
                float randomYVel = Random.Range(-Constants.speed, Constants.speed) * Time.deltaTime;
                transform.position += new Vector3(randomXVel, randomYVel, 0);
                break;
                
            case "Move Towards":
                if (closestBot != null && closestBot.isAlive)
                {
                    Vector3 direction = (closestBot.transform.position - transform.position).normalized;
                    transform.position += direction * Constants.speed * Time.deltaTime;
                }
                break;
                
            case "Move Away":
                if (closestBot != null && closestBot.isAlive)
                {
                    Vector3 direction = (transform.position - closestBot.transform.position).normalized;
                    transform.position += direction * Constants.speed * Time.deltaTime;
                }
                break;
                
            case "Attack":
                // Attack logic handled by agent
                break;
        }
        
        // Compute last movement direction for collision‑based logic
        Vector3 delta = transform.position - prevPos;
        lastMoveDirection = new Vector2(delta.x, delta.y);
        
        // Clamp position to screen bounds to keep agent visible.
        // Note: clamping can cause agents to "stick" in corners if their
        // chosen action keeps trying to move further out of bounds; the
        // decision interval will eventually change their action.
        ClampPositionToScreenBounds();
    }
    
    private void ClampPositionToScreenBounds()
    {
        // Get camera if not already initialized
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        if (mainCamera == null)
            return; // No camera, can't clamp
        
        // Calculate screen bounds if needed
        if (screenBounds == Vector2.zero)
        {
            float height = mainCamera.orthographicSize * 2f;
            float width = height * mainCamera.aspect;
            screenBounds = new Vector2(width / 2f, height / 2f);
        }
        
        // Get camera position
        Vector3 cameraPos = mainCamera.transform.position;
        
        // Clamp position to screen bounds with padding
        Vector3 unclampedPos = transform.position;
        Vector3 clampedPos = unclampedPos;
        clampedPos.x = Mathf.Clamp(clampedPos.x, cameraPos.x - screenBounds.x + boundsPadding, cameraPos.x + screenBounds.x - boundsPadding);
        clampedPos.y = Mathf.Clamp(clampedPos.y, cameraPos.y - screenBounds.y + boundsPadding, cameraPos.y + screenBounds.y - boundsPadding);
        clampedPos.z = 0; // Keep z at 0
        
        // If we had to clamp, gently nudge the agent back toward screen center
        // so they don't visually "stick" in the exact corner.
        if ((clampedPos - unclampedPos).sqrMagnitude > 1e-6f)
        {
            Vector3 center = cameraPos;
            Vector3 inward = (center - clampedPos).normalized;
            clampedPos += inward * 0.01f;
        }
        
        transform.position = clampedPos;
    }
    
    public void RecordAction(int actionId)
    {
        actionHistory.Add(new ActionRecord { actionId = actionId, timestamp = Time.time });
        // Keep only last 10 actions
        if (actionHistory.Count > 10)
            actionHistory.RemoveAt(0);
    }
    
    public void GetLastTwoActions(out int lastActionId, out int secondLastActionId)
    {
        lastActionId = -1;
        secondLastActionId = -1;
        
        if (actionHistory.Count >= 1)
            lastActionId = actionHistory[actionHistory.Count - 1].actionId;
        if (actionHistory.Count >= 2)
            secondLastActionId = actionHistory[actionHistory.Count - 2].actionId;
    }
    
    public Dictionary<string, int> GetAlignmentCounts()
    {
        Dictionary<string, int> counts = new Dictionary<string, int>
        {
            { Constants.ALIGNMENT_NEUTRAL, 0 },
            { Constants.ALIGNMENT_FRIENDLY, 0 },
            { Constants.ALIGNMENT_PREY, 0 },
            { Constants.ALIGNMENT_PREDATOR, 0 },
            { Constants.ALIGNMENT_HOSTILE, 0 }
        };
        
        foreach (var alignment in alignments.Values)
        {
            if (counts.ContainsKey(alignment))
                counts[alignment]++;
        }
        
        return counts;
    }
    
    public void UpdateNearestBots()
    {
        closestBot = null;
        secondClosestBot = null;
        float closestDistance = float.MaxValue;
        float secondClosestDistance = float.MaxValue;

        // Ensure managerScript is available by any means
        if (managerScript == null)
        {
            if (manager != null)
            {
                managerScript = manager.GetComponent<GameManager>();
            }
            else
            {
                // Last resort: search for GameManager in scene
                GameObject gm = GameObject.Find("GameManager");
                if (gm != null)
                {
                    manager = gm;
                    managerScript = gm.GetComponent<GameManager>();
                }
            }
        }
        
        if(managerScript == null)
        {
            Debug.LogWarning($"UpdateNearestBots id={robotId}: managerScript=null, manager={manager}, robotId={robotId}");
            return;
        }
        if(managerScript.robots == null)
        {
            Debug.LogWarning($"UpdateNearestBots id={robotId}: robots dict is null");
            return;
        }
        
        int checkedCount = 0;
        int skippedCount = 0;
        foreach (RobotScript bot in managerScript.robots.Values)
        {
            checkedCount++;
            if (bot.robotId == robotId || !bot.isAlive) 
            { 
                skippedCount++;
                continue; 
            }
            
            float distance = Vector3.Distance(transform.position, bot.transform.position);
            
            if (distance < closestDistance)
            {
                secondClosestBot = closestBot;
                secondClosestDistance = closestDistance;
                closestBot = bot;
                closestDistance = distance;
            }
            else if (distance < secondClosestDistance)
            {
                secondClosestBot = bot;
                secondClosestDistance = distance;
            }
        }
        
        // if (checkedCount > 0)
        //     Debug.Log($"UpdateNearestBots id={robotId}: checked={checkedCount} skipped={skippedCount} alive, closest={(closestBot!=null?closestBot.robotId:-1)} second={(secondClosestBot!=null?secondClosestBot.robotId:-1)}");
    }
    
    private float MovementDotTowards(RobotScript mover, RobotScript target)
    {
        if (mover == null || target == null) return 0f;
        
        Vector2 moveDir = mover.lastMoveDirection;
        Vector2 toTarget = (Vector2)(target.transform.position - mover.transform.position);
        
        if (moveDir.sqrMagnitude < 1e-6f || toTarget.sqrMagnitude < 1e-6f)
            return 0f;
        
        moveDir.Normalize();
        toTarget.Normalize();
        return Vector2.Dot(moveDir, toTarget);
    }
    
    private void ResolveKillerVsKillerCollision(RobotScript a, RobotScript b)
    {
        if (managerScript == null) return;
        
        float aDot = MovementDotTowards(a, b);
        float bDot = MovementDotTowards(b, a);
        
        const float TOWARDS_THRESH = 0.3f;
        const float AWAY_THRESH = -0.3f;
        
        bool aTowards = aDot > TOWARDS_THRESH;
        bool aAway    = aDot < AWAY_THRESH;
        bool bTowards = bDot > TOWARDS_THRESH;
        bool bAway    = bDot < AWAY_THRESH;
        
        // Only one attacking (towards) and the other clearly fleeing (away) results in a kill.
        if (aAway && bTowards)
        {
            managerScript.HandleKill(b, a); // b (chaser) kills a (fleeing)
        }
        else if (bAway && aTowards)
        {
            managerScript.HandleKill(a, b); // a (chaser) kills b (fleeing)
        }
        // Otherwise (both toward, both away, or mostly sideways) no kill occurs.
    }
    
    private void ResolveKillerVsSurvivorCollision(RobotScript killer, RobotScript survivor)
    {
        if (managerScript == null) return;
        
        float killerDot = MovementDotTowards(killer, survivor);
        const float TOWARDS_THRESH = 0.3f;
        
        // Killer must clearly be moving toward the survivor for the kill to count.
        if (killerDot > TOWARDS_THRESH)
        {
            managerScript.HandleKill(killer, survivor);
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        RobotScript other = collision.gameObject.GetComponent<RobotScript>();
        if (other == null) return;
        if (!isAlive || !other.isAlive) return;
        
        // Make sure we have a manager reference (mirrors UpdateNearestBots safety)
        if (managerScript == null)
        {
            if (manager != null)
            {
                managerScript = manager.GetComponent<GameManager>();
            }
            else
            {
                GameObject gm = GameObject.Find("GameManager");
                if (gm != null)
                {
                    manager = gm;
                    managerScript = gm.GetComponent<GameManager>();
                }
            }
        }
        if (managerScript == null || managerScript.robots == null) return;
        
        bool thisIsKiller    = agentType == Constants.AGENT_TYPE_KILLER;
        bool otherIsKiller   = other.agentType == Constants.AGENT_TYPE_KILLER;
        bool thisIsSurvivor  = agentType == Constants.AGENT_TYPE_SURVIVOR;
        bool otherIsSurvivor = other.agentType == Constants.AGENT_TYPE_SURVIVOR;
        
        if (thisIsKiller && otherIsKiller)
        {
            ResolveKillerVsKillerCollision(this, other);
        }
        else if (thisIsKiller && otherIsSurvivor)
        {
            ResolveKillerVsSurvivorCollision(this, other);
        }
        else if (thisIsSurvivor && otherIsKiller)
        {
            ResolveKillerVsSurvivorCollision(other, this);
        }
    }
    
    public void Die()
    {
        isAlive = false;
        // Keep GameObject active so Agent/episodes can reset it between matches.
    }
}
