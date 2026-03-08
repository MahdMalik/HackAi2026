using System.Collections.Generic;
using UnityEngine;

public class NormalActionScript : RobotScript
{
    void Start()
    {
        DoStart();
        foreach (int botId in managerScript.robots.Keys)
        {
            if(botId != robotId) alignments[botId] = "Neutral";
        }
    }

    // Update is called once per frame
    void Update()
    {
        DoUpdate();
    }
}
