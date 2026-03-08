using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using System.Collections.Generic;

public class EvilAgentScript : Agent
{
    private RobotScript robotScript;
    private GameManager gameManager;
    
    // Cached references
    private VectorSensor vectorSensor;
    private float episodeStartTime;
    private float lastRewardTime;
    
    public override void Initialize()
    {
        robotScript = GetComponent<RobotScript>();
        robotScript.agentType = Constants.AGENT_TYPE_KILLER;
        // sometimes the prefab has the Agent component and Initialize fires before the SceneInitializer
        // assigns the manager; attempt to recover by finding the object if still null
        if (robotScript.manager == null)
        {
            GameObject gmObj = GameObject.Find("GameManager");
            if (gmObj != null)
            {
                robotScript.manager = gmObj;
            }
            else
            {
                Debug.LogWarning("EvilScript.Initialize: manager not assigned and GameManager not found");
            }
        }
        gameManager = robotScript.manager?.GetComponent<GameManager>();
        Debug.Log($"EvilScript.Initialize id={robotScript.robotId} mgr={(robotScript.manager!=null)}");
            }

    public override void OnEpisodeBegin()
    {
        episodeStartTime = Time.time;
        lastRewardTime = Time.time;
        
        // Ensure gameManager is ready FIRST
        if (gameManager == null)
        {
            GameObject gmObj = GameObject.Find("GameManager");
            if (gmObj != null)
            {
                gameManager = gmObj.GetComponent<GameManager>();
                Debug.Log($"EvilAgentScript.OnEpisodeBegin id={robotScript.robotId}: Found gameManager");
            }
            else
            {
                Debug.LogWarning($"EvilAgentScript.OnEpisodeBegin id={robotScript.robotId}: GameManager not found!");
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
        robotScript.killCount = 0;
        robotScript.currentAction = "Move Random";
        robotScript.actionHistory.Clear();
        
        // Reinitialize alignments to all "Prey"
        robotScript.alignments.Clear();
        if (gameManager != null && gameManager.robots != null)
        {
            foreach (int botId in gameManager.robots.Keys)
            {
                if (botId != robotScript.robotId)
                    robotScript.alignments[botId] = Constants.ALIGNMENT_PREY;
            }
        }
        
        // ensure the agent begins decisions immediately
        if (robotScript != null && gameManager != null) RequestDecision();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Debug.Log($"CollectObservations: robotScript={robotScript != null}, gm={gameManager != null}, robots={gameManager?.robots != null}");
        // ALWAYS add exactly 12 observations, no matter what
        int obsCount = 0;
        
        try
        {
            // Ensure gameManager is set
            if (gameManager == null)
            {
                GameObject gmObj = GameObject.Find("GameManager");
                if (gmObj != null)
                    gameManager = gmObj.GetComponent<GameManager>();
            }
            
            if (gameManager != null && gameManager.robots != null)
            {
                // Update nearest bots
                robotScript.UpdateNearestBots();
                
                // 1. Nearest bot info: [bot_id, last_action_encoded, exists]
                if (robotScript.closestBot != null)
                {
                    sensor.AddObservation(robotScript.closestBot.robotId);
                    sensor.AddObservation(EncodeLastAction(robotScript.closestBot.lastAction));
                    sensor.AddObservation(1f); // exists
                    obsCount += 3;
                }
                else
                {
                    sensor.AddObservation(0f);
                    sensor.AddObservation(0f);
                    sensor.AddObservation(0f);
                    obsCount += 3;
                }
                
                // 2. Second nearest bot info
                if (robotScript.secondClosestBot != null)
                {
                    sensor.AddObservation(robotScript.secondClosestBot.robotId);
                    sensor.AddObservation(EncodeLastAction(robotScript.secondClosestBot.lastAction));
                    sensor.AddObservation(1f); // exists
                    obsCount += 3;
                }
                else
                {
                    sensor.AddObservation(0f);
                    sensor.AddObservation(0f);
                    sensor.AddObservation(0f);
                    obsCount += 3;
                }
                
                // 3. Time left
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
                
                // 5. Alignment counts for self perception
                Dictionary<string, int> alignmentCounts = robotScript.GetAlignmentCounts();
                sensor.AddObservation(alignmentCounts[Constants.ALIGNMENT_NEUTRAL] / (float)gameManager.robots.Count);
                sensor.AddObservation(alignmentCounts[Constants.ALIGNMENT_FRIENDLY] / (float)gameManager.robots.Count);
                sensor.AddObservation(alignmentCounts[Constants.ALIGNMENT_PREY] / (float)gameManager.robots.Count);
                sensor.AddObservation(alignmentCounts[Constants.ALIGNMENT_PREDATOR] / (float)gameManager.robots.Count);
                obsCount += 4;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EvilAgentScript.CollectObservations exception: {ex.Message}");
            Debug.Log($"So then the obs count is {obsCount}");
        }
        
        // Guarantee 12 observations, pad with zeros if needed
        while (obsCount < Constants.KILLER_OBS_SIZE)
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
        
        // Branch 0: Movement (0-4)
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
        AddReward(-0.001f);
        
        // Request the next decision to continue the episode
        RequestDecision();
    }

    private void ExecuteMovementAction(int actionIndex)
    {
        switch (actionIndex)
        {
            case 0: // Run away from nearest
                robotScript.currentAction = "Move Away";
                break;
            case 1: // Follow nearest
                robotScript.currentAction = "Move Towards";
                break;
            case 2: // Attack nearest (treated as follow for now)
                robotScript.currentAction = "Move Towards";
                CheckAndExecuteKill(robotScript.closestBot);
                break;
            case 3: // Follow 2nd nearest
                if (robotScript.secondClosestBot != null)
                {
                    robotScript.closestBot = robotScript.secondClosestBot;
                    robotScript.currentAction = "Move Towards";
                }
                else
                    robotScript.currentAction = "Move Random";
                break;
            case 4: // Move random
                robotScript.currentAction = "Move Random";
                break;
        }
        
        robotScript.lastAction = robotScript.currentAction;
    }

    private void ExecuteAlignmentAction(RobotScript targetBot, int alignmentActionIndex)
    {
        if (targetBot == null) return;
        
        // Alignment action: 4 possible alignments + 1 "no change"
        // 0-3: Change to Neutral, Friendly, Prey, Predator
        // 4: No change (this counts as 5 options, but we encode as 0-4)
        
        if (alignmentActionIndex == 4) return; // No change
        
        string[] alignmentOptions = {
            Constants.ALIGNMENT_NEUTRAL,
            Constants.ALIGNMENT_FRIENDLY,
            Constants.ALIGNMENT_PREY,
            Constants.ALIGNMENT_PREDATOR
        };
        
        if (alignmentActionIndex < alignmentOptions.Length)
            robotScript.alignments[targetBot.robotId] = alignmentOptions[alignmentActionIndex];
    }

    private void CheckAndExecuteKill(RobotScript targetBot)
    {
        if (targetBot == null || !targetBot.isAlive) return;
        
        float distance = Vector3.Distance(transform.position, targetBot.transform.position);
        if (distance < 1f && gameManager != null) // Kill range
        {
            // Delegate full kill handling (rewards + penalties) to GameManager
            gameManager.HandleKill(robotScript, targetBot);
        }
    }

    public void OnDeath()
    {
        if (robotScript.isAlive)
        {
            AddReward(-10f);
            gameManager.AssignRewardToLastActions(robotScript.robotId, -10f);
            robotScript.Die();
        }
    }
    
    /// <summary>
    /// Called by GameManager when this killer successfully kills another robot.
    /// Handles kill count and alignment‑based reward assignment.
    /// </summary>
    public void OnKill(RobotScript targetBot)
    {
        if (targetBot == null) return;
        
        robotScript.killCount++;
        
        // Reward based on this killer's perceived alignment of the target
        string alignmentToTarget;
        if (!robotScript.alignments.TryGetValue(targetBot.robotId, out alignmentToTarget))
        {
            alignmentToTarget = Constants.ALIGNMENT_PREY;
        }
        
        if (alignmentToTarget == Constants.ALIGNMENT_PREY ||
            alignmentToTarget == Constants.ALIGNMENT_PREDATOR)
        {
            AddReward(5f);
            gameManager.AssignRewardToLastActions(robotScript.robotId, 5f);
        }
    }

    public void OnSurvival()
    {
        // Apply +3 to all actions taken
        foreach (var actionRecord in robotScript.actionHistory)
        {
            AddReward(0.3f); // Scale down: 3 / ~10 actions average
        }
        gameManager.AssignRewardToAllActions(robotScript.robotId, 3f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = Random.Range(0, 5); // Movement
        discreteActionsOut[1] = Random.Range(0, 5); // Alignment 1
        discreteActionsOut[2] = Random.Range(0, 5); // Alignment 2
    }

    private float EncodeLastAction(string action)
    {
        switch (action)
        {
            case "Move Random": return 0f;
            case "Move Away": return 1f;
            case "Move Towards": return 2f;
            case "Attack": return 3f;
            default: return 0f;
        }
    }
}
