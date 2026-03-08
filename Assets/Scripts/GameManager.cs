using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;


public class GameManager : MonoBehaviour
{
    public int matchTime = 30;
    public float currentTime = 0;
    public int matchNumber = 0;
    
    [HideInInspector]
    public Dictionary<int, RobotScript> robots;
    
    private Dictionary<int, float> pendingRewards; // For deferred reward assignment
    private bool matchEnded = false;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        robots = new Dictionary<int, RobotScript>();
        pendingRewards = new Dictionary<int, float>();
        
        // Ensure the simulation runs at normal real‑time speed in the Editor.
        // Training scripts can still override this via ML‑Agents if desired.
        Time.timeScale = 1f;
    }
    
    public void InitializeRobots()
    {
        GameObject evilBots = GameObject.Find("EvilFellas");
        if (evilBots == null)
        {
            Debug.LogError("EvilFellas not found!");
            return;
        }
        
        GameObject survivorBots = GameObject.Find("NormalFellas");
        if (survivorBots == null)
        {
            Debug.LogError("NormalFellas not found!");
            return;
        }

        int ids = 0;
        foreach (Transform evilBot in evilBots.transform)
        {
            RobotScript botScript = evilBot.gameObject.GetComponent<RobotScript>();
            if (botScript == null)
                botScript = evilBot.gameObject.GetComponent<EvilActionScript>();
            if (botScript == null)
                botScript = evilBot.gameObject.AddComponent<EvilActionScript>();
            
            robots[ids] = botScript;
            robots[ids].robotId = ids;
            robots[ids].manager = gameObject;
            ids += 1;
        }

        foreach (Transform survivorBot in survivorBots.transform)
        {
            RobotScript botScript = survivorBot.gameObject.GetComponent<RobotScript>();
            if (botScript == null)
                botScript = survivorBot.gameObject.GetComponent<NormalActionScript>();
            if (botScript == null)
                botScript = survivorBot.gameObject.AddComponent<NormalActionScript>();
            
            robots[ids] = botScript;
            robots[ids].robotId = ids;
            robots[ids].manager = gameObject;
            ids += 1;
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        #if UNITY_EDITOR
        // In the Editor, force real‑time pacing so a "30 second" match
        // actually lasts ~30 seconds for visualization, regardless of
        // any ML‑Agents timeScale changes used during training.
        Time.timeScale = 1f;
        #endif
        
        currentTime += Time.deltaTime;
        
        if(currentTime > matchTime && !matchEnded)
        {
            EndMatch();
        }
    }
    
    private void EndMatch()
    {
        matchEnded = true;
        
        // Handle survival rewards and death penalties
        foreach (KeyValuePair<int, RobotScript> robot in robots)
        {
            if (robot.Value.isAlive)
            {
                // Survivor bonus
                if (robot.Value.agentType == Constants.AGENT_TYPE_KILLER)
                {
                    EvilAgentScript evilAgent = robot.Value.GetComponent<EvilAgentScript>();
                    if (evilAgent != null) evilAgent.OnSurvival();
                }
                else if (robot.Value.agentType == Constants.AGENT_TYPE_SURVIVOR)
                {
                    NormalAgentScript normalAgent = robot.Value.GetComponent<NormalAgentScript>();
                    if (normalAgent != null) normalAgent.OnSurvival();
                }
            }
            
            // End episode for all agents
            Agent agent = robot.Value.GetComponent<Agent>();
            if (agent != null)
            {
                agent.EndEpisode();
            }
        }
        
        Debug.Log("Match ended! Restarting...");
        
        // Reset for next match
        ResetMatch();
    }
    
    private void ResetMatch()
    {
        currentTime = 0f;
        matchEnded = false;
        matchNumber++;
        
        // Reset all robots
        foreach (KeyValuePair<int, RobotScript> robot in robots)
        {
            robot.Value.isAlive = true;
            robot.Value.killCount = 0;
            robot.Value.actionHistory.Clear();
            
            // OnEpisodeBegin will handle positioning and alignment reset
        }
        
        Debug.Log($"Match {matchNumber} reset!");
    }
    
    public void AssignRewardToLastActions(int botId, float reward)
    {
        if (!robots.ContainsKey(botId)) return;
        
        RobotScript bot = robots[botId];
        Agent agent = bot.GetComponent<Agent>();
        
        if (agent == null) return;
        
        // Assign to last 2 actions
        bot.GetLastTwoActions(out int lastActionId, out int secondLastActionId);
        
        if (lastActionId >= 0)
            agent.AddReward(reward * 0.6f); // 60% to last action
        if (secondLastActionId >= 0)
            agent.AddReward(reward * 0.4f); // 40% to second-last action
    }
    
    public void AssignRewardToAllActions(int botId, float reward)
    {
        if (!robots.ContainsKey(botId)) return;
        
        RobotScript bot = robots[botId];
        Agent agent = bot.GetComponent<Agent>();
        
        if (agent == null) return;
        
        // Distribute reward across all actions
        int actionCount = bot.actionHistory.Count;
        if (actionCount > 0)
        {
            float rewardPerAction = reward / actionCount;
            for (int i = 0; i < actionCount; i++)
            {
                agent.AddReward(rewardPerAction);
            }
        }
    }
    
    public void HandleBotDeath(int botId)
    {
        if (!robots.ContainsKey(botId)) return;
        
        RobotScript bot = robots[botId];
        
        if (bot.agentType == Constants.AGENT_TYPE_KILLER)
        {
            EvilAgentScript evilAgent = bot.GetComponent<EvilAgentScript>();
            if (evilAgent != null) evilAgent.OnDeath();
        }
        else if (bot.agentType == Constants.AGENT_TYPE_SURVIVOR)
        {
            NormalAgentScript normalAgent = bot.GetComponent<NormalAgentScript>();
            if (normalAgent != null) normalAgent.OnDeath();
        }
    }
    
    /// <summary>
    /// Handle a kill event where one robot (killer) eliminates another (victim).
    /// Applies rewards to the killer (if applicable) and death penalties to victim.
    /// </summary>
    public void HandleKill(RobotScript killer, RobotScript victim)
    {
        if (killer == null || victim == null) return;
        if (!robots.ContainsKey(victim.robotId)) return;
        if (!victim.isAlive) return;
        
        // Reward killer if it is a Killer agent
        if (killer.agentType == Constants.AGENT_TYPE_KILLER)
        {
            EvilAgentScript killerAgent = killer.GetComponent<EvilAgentScript>();
            if (killerAgent != null)
            {
                killerAgent.OnKill(victim);
            }
        }
        
        // Apply death penalty to victim via existing handler
        HandleBotDeath(victim.robotId);
    }
}
