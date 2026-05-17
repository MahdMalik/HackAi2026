using System;
using System.Collections.Generic;
using Unity.MLAgents;
using System.Linq;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class Player : Agent
{
    public int id;

    public Queue<int> lastTwoActions;

    public override void OnEpisodeBegin()
    {
        lastTwoActions = new Queue<int>();
    }


    public override void CollectObservations(VectorSensor sensor)
    {
        
    }


    void Start()
    {
        OnEpisodeBegin();
    }

    void Update()
    {
        
    }
}