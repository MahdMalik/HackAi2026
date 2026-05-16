using System;
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

    void Start()
    {
        UI.gameStartSignal += StartGame;
    }

    void StartGame()
    {
        timeRemaining = gameTime;
        gameStarted = true;
        numKillers = totalKillers;
        numSurvivors = totalSurvivors;
    }

    // Update is called once per frame
    void Update()
    {
        if(gameStarted)
        {
            timeRemaining -= Time.deltaTime;
            if(timeRemaining < 0)
            {
                gameStarted = false;
                gameEndSignal.Invoke("Time Up!");
            }
        }
    }
}
