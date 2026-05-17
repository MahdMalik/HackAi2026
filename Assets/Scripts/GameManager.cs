using System;
using System.Collections.Generic;
using Unity.Mathematics;
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

    public List<GameObject> players;

    void Start()
    {
        UI.gameStartSignal += StartGame;
    }

    Vector2 CreateNewPosition()
    {
        float xPos = UnityEngine.Random.Range(mapBoundsX.x, mapBoundsX.y);
        float yPos = UnityEngine.Random.Range(mapBoundsY.x, mapBoundsY.y);

        foreach (GameObject plr in players)
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

    void SetupPlayers()
    {
        for(int i = 0; i < totalSurvivors; i++)
        {
            GameObject newSurvivor = Instantiate(survivorPrefab);
            newSurvivor.transform.position = CreateNewPosition();
            players.Add(newSurvivor);
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
        foreach(GameObject plr in players)
        {
            Destroy(plr);
        }
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
