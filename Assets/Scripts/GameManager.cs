using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private bool gameStarted;
    private const int gameTime = 5;
    public float timeRemaining;
    
    private const int totalKillers = 5;
    private const int totalSurvivors = 10;

    public int numKillers;
    public int numSurvivors;

    public static event Action<String> gameEndSignal;

    private readonly Vector2 mapBoundsX = new Vector2(-8.8f, 8.8f);
    private readonly Vector2 mapBoundsY = new Vector2(-4.8f, 4.8f);

    public GameObject survivorPrefab;

    public Dictionary<int, GameObject> players;

    void Start()
    {
        UI.gameStartSignal += StartGame;
        players = new Dictionary<int, GameObject>();
    }

    Vector2 CreateNewPosition()
    {
        float xPos = UnityEngine.Random.Range(mapBoundsX.x, mapBoundsX.y);
        float yPos = UnityEngine.Random.Range(mapBoundsY.x, mapBoundsY.y);

        foreach (GameObject plr in players.Values)
        {
            float xDist = Mathf.Abs(xPos - plr.transform.position.x);
            float yDist = Mathf.Abs(yPos - plr.transform.position.y);
            float distance = Mathf.Pow(Mathf.Pow(xDist, 2) + Mathf.Pow(yDist, 2), 0.5f);

            if(distance <= 1)
            {
                return CreateNewPosition();
            }
        }

        return new Vector2(xPos, yPos);
    }

    float[] GenerateProbabilities()
    {
        float firstProbLogit = UnityEngine.Random.Range(0f, 1f);
        float secondProbLogit = UnityEngine.Random.Range(0f, 1f);
        float thirdProbLogit = UnityEngine.Random.Range(0f, 1f);

        float dividingFactor = firstProbLogit + secondProbLogit + thirdProbLogit;
        return new float[] { firstProbLogit / dividingFactor, secondProbLogit / dividingFactor, thirdProbLogit / dividingFactor };
    }

    void SetupPlayers()
    {
        for(int i = 0; i < totalSurvivors; i++)
        {
            GameObject newSurvivor = Instantiate(survivorPrefab);
            newSurvivor.transform.position = CreateNewPosition();
            newSurvivor.GetComponent<Player>().id = i;
            players[newSurvivor.GetComponent<Player>().id] = newSurvivor;
        }


        foreach(int plrId in players.Keys)
        {
            Player plrScript = players[plrId].GetComponent<Player>();
            foreach(int otherPlrId in players.Keys)
            {
                if(plrId == otherPlrId)
                {
                    continue;
                }
                plrScript.playerStatuses[otherPlrId] = GenerateProbabilities();
            }
        }
    }



    void StartGame()
    {
        timeRemaining = gameTime;
        gameStarted = true;
        numKillers = totalKillers;
        numSurvivors = totalSurvivors;
        SetupPlayers();
    }

    void EndGame()
    {
        gameStarted = false;
        gameEndSignal.Invoke("Time Up!");
        foreach(GameObject plr in players.Values)
        {
            Destroy(plr);
        }
        players.Clear();
    }

    // Update is called once per frame
    void Update()
    {
        if(gameStarted)
        {
            timeRemaining -= Time.deltaTime;
            if(timeRemaining < 0)
            {
                EndGame();
            }
        }
    }
}
