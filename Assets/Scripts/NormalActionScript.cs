using System.Collections.Generic;
using UnityEngine;

public class NormalActionScript : RobotScript
{
    void Start()
    {
        foreach (int botId in manager.robots.Keys)
        {
            if(botId != robotId) alignments[botId] = "Neutral";
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
