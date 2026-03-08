using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using System.Collections.Generic;

public class NormalAgentScript : Agent
{
    private RobotScript robotScript;
    private GameManager gameManager;
    
    // Cached references
    private VectorSensor vectorSensor;
    private float episodeStartTime;
    
    public override void Initialize()
    {
        robotScript = GetComponent<RobotScript>();
        robotScript.agentType = Constants.AGENT_TYPE_SURVIVOR;
        if (robotScript.manager == null)
        {
            GameObject gmObj = GameObject.Find("GameManager");
            if (gmObj != null) robotScript.manager = gmObj;
            else Debug.LogWarning("NormalScript.Initialize: manager null");
        }
        gameManager = robotScript.manager?.GetComponent<GameManager>();
        Debug.Log($"NormalScript.Initialize id={robotScript.robotId} mgr={(robotScript.manager!=null)}");
        
    }

    public override void OnEpisodeBegin()
    {
        episodeStartTime = Time.time;
        
        // Ensure gameManager is ready FIRST
        if (gameManager == null)
        {
            GameObject gmObj = GameObject.Find("GameManager");
            if (gmObj != null)
            {
                gameManager = gmObj.GetComponent<GameManager>();
                Debug.Log($"NormalAgentScript.OnEpisodeBegin id={robotScript.robotId}: Found gameManager");
            }
            else
            {
                Debug.LogWarning($"NormalAgentScript.OnEpisodeBegin id={robotScript.robotId}: GameManager not found!");
            }
        }
        
        // Reset position to random location within screen bounds
        Camera mainCamera = Camera.main;
        Vector3 spawnPos = new Vector3(Random.Range(-10f, 10f), Random.Range(-10f, 10f), 0);
        
        if (mainCamera != null)
        {
            float height = mainCamera.orthographicSize * 2f;
            float width = height * mainCamera.aspect;
            float boundsX = width / 2f - 1f; // -1 for padding
            float boundsY = height / 2f - 1f;
            spawnPos = new Vector3(
                Random.Range(-boundsX, boundsX),
                Random.Range(-boundsY, boundsY),
                0
            );
        }
        
        transform.position = spawnPos;
        
        // Reset robot state
        robotScript.isAlive = true;
        robotScript.currentAction = "Move Random";
        robotScript.actionHistory.Clear();
        
        // Survivors use "Neutral" and "Hostile" instead of the full alignment system
        robotScript.alignments.Clear();
        if (gameManager != null && gameManager.robots != null)
        {
            foreach (int botId in gameManager.robots.Keys)
            {
                if (botId != robotScript.robotId)
                    robotScript.alignments[botId] = Constants.ALIGNMENT_NEUTRAL;
            }
        }
        
        RequestDecision();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // ALWAYS add exactly 12 observations, no matter what
        int obsCount = 0;
        
        // Ensure gameManager is set BEFORE try block to avoid zero vector errors
        if (gameManager == null)
        {
            GameObject gmObj = GameObject.Find("GameManager");
            if (gmObj != null)
                gameManager = gmObj.GetComponent<GameManager>();
        }
        
        try
        {
            
            if (gameManager != null && gameManager.robots != null)
            {
                // Update nearest bots
                robotScript.UpdateNearestBots();
            
                // 1. Nearest bot info: [bot_id, last_action_encoded, exists, agent_type]
                if (robotScript.closestBot != null)
                {
                    sensor.AddObservation(robotScript.closestBot.robotId);
                    obsCount++;
                    sensor.AddObservation(EncodeLastAction(robotScript.closestBot.lastAction));
                    obsCount++;
                    sensor.AddObservation(1f); // exists
                    obsCount++;
                    sensor.AddObservation(EncodeAgentType(robotScript.closestBot.agentType));
                    obsCount++;
                }
                else
                {
                    sensor.AddObservation(0f);
                    obsCount++;
                    sensor.AddObservation(0f);
                    obsCount++;
                    sensor.AddObservation(0f);
                    obsCount++;
                    sensor.AddObservation(0f);
                    obsCount++;
                }
                
                // 2. Second nearest bot info
                if (robotScript.secondClosestBot != null)
                {
                    sensor.AddObservation(robotScript.secondClosestBot.robotId);
                    obsCount++;
                    sensor.AddObservation(EncodeLastAction(robotScript.secondClosestBot.lastAction));
                    obsCount++;
                    sensor.AddObservation(1f); // exists
                    obsCount++;
                    sensor.AddObservation(EncodeAgentType(robotScript.secondClosestBot.agentType));
                    obsCount++;
                }
                else
                {
                    sensor.AddObservation(0f);
                    obsCount++;
                    sensor.AddObservation(0f);
                    obsCount++;
                    sensor.AddObservation(0f);
                    obsCount++;
                    sensor.AddObservation(0f);
                    obsCount++;
                }
                
                // 3. Time left (normalized)
                float timeLeft = gameManager.matchTime - gameManager.currentTime;
                sensor.AddObservation(timeLeft / gameManager.matchTime);
                obsCount++;
                
                // 4. Number of bots dead
                int deadCount = 0;
                foreach (RobotScript bot in gameManager.robots.Values)
                {
                    if (!bot.isAlive) deadCount++;
                }
                sensor.AddObservation(deadCount / (float)gameManager.robots.Count);
                obsCount++;
                
                // 5. Alignment counts (Neutral vs Hostile for survivors)
                Dictionary<string, int> alignmentCounts = robotScript.GetAlignmentCounts();
                int neutralCount = alignmentCounts.ContainsKey(Constants.ALIGNMENT_NEUTRAL) ? alignmentCounts[Constants.ALIGNMENT_NEUTRAL] : 0;
                int hostileCount = alignmentCounts.ContainsKey(Constants.ALIGNMENT_HOSTILE) ? alignmentCounts[Constants.ALIGNMENT_HOSTILE] : 0;
                
                int totalOthers = gameManager.robots.Count - 1; // Exclude self
                sensor.AddObservation(neutralCount / (float)totalOthers);
                obsCount++;
                sensor.AddObservation(hostileCount / (float)totalOthers);
                obsCount++;
                
            }
            
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"NormalAgentScript.CollectObservations id={robotScript.robotId} Exception: {ex.Message}\n{ex.StackTrace}");
        }
        
        // Guarantee exactly 16 observations, pad with zeros if needed (MUST be outside try-catch)
        while (obsCount < Constants.SURVIVOR_OBS_SIZE)
        {
            sensor.AddObservation(0f);
            obsCount++;
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (gameManager != null && !gameManager.matchStarted) gameManager.matchStarted = true;
        
        if (!robotScript.isAlive)
        {
            EndEpisode();
            return;
        }
        
        // Discrete actions: [movement, alignment1, alignment2]
        int[] discreteActions = actions.DiscreteActions.Array;
        
        // Branch 0: Movement (0-3)
        int movementAction = discreteActions[0];
        ExecuteMovementAction(movementAction);
        
        // Branch 1-2: Alignment changes for closest 2 bots
        int alignmentAction1 = discreteActions[1];
        int alignmentAction2 = discreteActions[2];
        ExecuteAlignmentAction(robotScript.closestBot, alignmentAction1);
        ExecuteAlignmentAction(robotScript.secondClosestBot, alignmentAction2);
        
        // Record the composite action
        int compositeAction = movementAction * 256 + alignmentAction1 * 16 + alignmentAction2;
        robotScript.RecordAction(compositeAction);
        
        // Small penalty for each step to encourage efficiency
        AddReward(-0.002f);
        
        // Request the next decision to continue the episode
        RequestDecision();
    }

    private void ExecuteMovementAction(int actionIndex)
    {
        switch (actionIndex)
        {
            case 0: // Run away from nearest threat
                robotScript.currentAction = "Move Away";
                break;
            case 1: // Follow nearest (cooperative)
                robotScript.currentAction = "Move Towards";
                break;
            case 2: // Follow 2nd nearest
                if (robotScript.secondClosestBot != null)
                {
                    robotScript.closestBot = robotScript.secondClosestBot;
                    robotScript.currentAction = "Move Towards";
                }
                else
                    robotScript.currentAction = "Move Random";
                break;
            case 3: // Move random
                robotScript.currentAction = "Move Random";
                break;
        }
        
        robotScript.lastAction = robotScript.currentAction;
    }

    private void ExecuteAlignmentAction(RobotScript targetBot, int alignmentActionIndex)
    {
        if (targetBot == null) return;
        
        // Survivors only have 2 alignments: Neutral (0) or Hostile (1), plus No Change (2)
        switch (alignmentActionIndex)
        {
            case 0:
                robotScript.alignments[targetBot.robotId] = Constants.ALIGNMENT_NEUTRAL;
                break;
            case 1:
                robotScript.alignments[targetBot.robotId] = Constants.ALIGNMENT_HOSTILE;
                break;
            case 2:
                // No change
                break;
        }
    }

    public void OnDeath()
    {
        if (robotScript.isAlive)
        {
            AddReward(-15f);
            gameManager.AssignRewardToLastActions(robotScript.robotId, -15f);
            robotScript.Die();
        }
    }

    public void OnSurvival()
    {
        // Apply +5 to all actions taken
        foreach (var actionRecord in robotScript.actionHistory)
        {
            AddReward(0.5f); // Scale down
        }
        gameManager.AssignRewardToAllActions(robotScript.robotId, 5f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = Random.Range(0, 4); // Movement
        discreteActionsOut[1] = Random.Range(0, 3); // Alignment 1
        discreteActionsOut[2] = Random.Range(0, 3); // Alignment 2
    }

    private float EncodeLastAction(string action)
    {
        switch (action)
        {
            case "Move Random": return 0f;
            case "Move Away": return 1f;
            case "Move Towards": return 2f;
            default: return 0f;
        }
    }
    
    private float EncodeAgentType(string agentType)
    {
        switch (agentType)
        {
            case Constants.AGENT_TYPE_KILLER: return 1f;
            case Constants.AGENT_TYPE_SURVIVOR: return 0f;
            case Constants.AGENT_TYPE_HERO: return 2f;
            default: return 0f;
        }
    }
}
