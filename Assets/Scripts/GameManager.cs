using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int matchTime = 30;
    public float currentTime = 0;
    
    [HideInInspector]
    public Dictionary<int, RobotScript> robots;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        GameObject evilBots = transform.parent.Find("EvilFellas").gameObject;
        GameObject survivorBots = transform.parent.Find("NormalFellas").gameObject;

        int ids = 0;
        foreach (GameObject evilBot in evilBots.transform)
        {
            EvilActionScript botScript = evilBot.GetComponent<EvilActionScript>();
            robots[ids] = botScript;
            botScript.robotId = ids;
            ids += 1;

        }

        foreach (GameObject survivorBot in survivorBots.transform)
        {
            NormalActionScript botScript = survivorBot.GetComponent<NormalActionScript>();
            robots[ids] = botScript;
            botScript.robotId = ids;
            ids += 1;
        }
    }

    // Update is called once per frame
    void Update()
    {
        currentTime += Time.deltaTime;
        if(currentTime > matchTime)
        {
            Debug.Log("End the match!");
        }
    }
}
