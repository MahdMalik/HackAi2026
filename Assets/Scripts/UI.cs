using Unity.AppUI.UI;
using System;
using UnityEngine;
using TMPro;
using Unity.Mathematics;

public class UI : MonoBehaviour
{
    public GameManager managerBud;
    public GameObject startButton;
    public TextMeshProUGUI gameText;
    public TextMeshProUGUI gameEndText;
    public static event Action gameStartSignal;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Start()
    {
        gameText.gameObject.SetActive(false);
        gameEndText.gameObject.SetActive(false);
        GameManager.gameEndSignal += EndGameOccured;
    }

    void Update()
    {
        if(gameText.gameObject.activeInHierarchy == true)
        {
            gameText.text = $@"Time Left: {(int) math.ceil(managerBud.timeRemaining)}\nKillers Remaining: {managerBud.numKillers}\nSurvivors Remaining: {managerBud.numSurvivors}";
        }
    }

    public void StartButtonClicked()
    {
        gameStartSignal.Invoke();
        startButton.SetActive(false);
        gameEndText.gameObject.SetActive(false);
        gameText.gameObject.SetActive(true);
    }

    void EndGameOccured(string reason)
    {
        startButton.SetActive(true);
        gameText.gameObject.SetActive(false);
        gameEndText.gameObject.SetActive(true);
        gameEndText.text = $@"Game Over!\nReason: {reason}\nRemaining Players: Survivors: {managerBud.numSurvivors}, Killers: {managerBud.numKillers}\nWinner: {(managerBud.numSurvivors > 0 ? "Survivors" : "Killers")}";
    }
}
