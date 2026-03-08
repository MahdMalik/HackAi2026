using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [HideInInspector]
    public List<RobotScript> robots;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject evilBots = transform.parent.Find("EvilFellas").gameObject;
        GameObject survivorBots = transform.parent.Find("NormalFellas").gameObject;

        foreach (GameObject evilBot in evilBots.transform)
        {
            robots.Add(evilBot.GetComponent<EvilActionScript>());
        }

        foreach (GameObject survivorBot in survivorBots.transform)
        {
            robots.Add(survivorBot.GetComponent<EvilActionScript>());
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
