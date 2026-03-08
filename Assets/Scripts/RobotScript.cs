using System.Collections.Generic;
using UnityEngine;

public class RobotScript : MonoBehaviour
{
    public GameObject manager;
    public float speed;
    protected GameManager managerScript;
    
    public string currentAction = "Move Random";

    public int robotId;

    public Dictionary<int, string> alignments;

    protected RobotScript closestBot = null;

    protected RobotScript secondClosestBot = null;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected void DoStart()
    {
        managerScript = manager.GetComponent<GameManager>();
        alignments = new Dictionary<int, string>();
    }

    // Update is called once per frame
    protected void DoUpdate()
    {
        switch (currentAction)
        {
            case "Move Random":
                float randomXVel = Random.Range(-speed, speed) * Time.deltaTime;
                float randomYVel = Random.Range(-speed, speed) * Time.deltaTime;
                transform.position += new Vector3(randomXVel, randomYVel, 0);
                break;
        }
    }
}
