using UnityEngine;
using System.Collections.Generic;

public class EvilActionScript : RobotScript
{    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DoStart();
        
        // Initialize alignments - killers see all others as "Prey"
        foreach (int botId in managerScript.robots.Keys)
        {
            if(botId != robotId) 
                alignments[botId] = Constants.ALIGNMENT_PREY;
        }
    }

    // Update is called once per frame
    void Update()
    {
        DoUpdate();
    }
}
