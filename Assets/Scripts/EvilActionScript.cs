using UnityEngine;
using System.Collections.Generic;

public class EvilActionScript : RobotScript
{    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DoStart();
        foreach (int botId in managerScript.robots.Keys)
        {
            if(botId != robotId) alignments[botId] = "Prey";
        }
    }

    // Update is called once per frame
    void Update()
    {
        DoUpdate();
    }

}
