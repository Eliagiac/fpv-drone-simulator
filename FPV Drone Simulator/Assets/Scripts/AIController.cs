using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class AIController : DroneController
{
    public NeuralNetwork NeuralNetwork;

    [SerializeField] private float _maxDistanceToCheckpoint = 50;

    [SerializeField] private string _weightsFilePath = "";

    private bool UsingLoadedWeights => _weightsFilePath != "";


    protected override void Start()
    {
        base.Start();
        if (UsingLoadedWeights) NeuralNetwork = new NeuralNetwork(AIManager.NetworkSize, _weightsFilePath);
        IsReady = true;
    }

    protected override void Update()
    {
        Vector3[] distanceToNextCheckpoints = NextCheckpointsPositionDifference;
        float[] angularDistanceToNextCheckpoints = AngularDistanceToNextCheckpoints;
        float[] nextCheckpointsSize = NextCheckpointsSize;

        // Kill the drone if it gets to far from the next checkpoint.
        if (_maxDistanceToCheckpoint != 0 && distanceToNextCheckpoints[0].magnitude > _maxDistanceToCheckpoint)
        {
            AIManager.Instance.Kill(this);
            IsDead = true;
            return;
        }

        double[] outputs = NeuralNetwork.FeedForward(new double[]
        {
            DroneTilt,

            DroneVelocityX,
            DroneVelocityY,
            DroneVelocityZ,

            HeightFromGround,

            distanceToNextCheckpoints[0].x,
            distanceToNextCheckpoints[0].y,
            distanceToNextCheckpoints[0].z,
            distanceToNextCheckpoints[1].x,
            distanceToNextCheckpoints[1].y,
            distanceToNextCheckpoints[1].z,
            distanceToNextCheckpoints[2].x,
            distanceToNextCheckpoints[2].y,
            distanceToNextCheckpoints[2].z,

            angularDistanceToNextCheckpoints[0],
            angularDistanceToNextCheckpoints[1],
            angularDistanceToNextCheckpoints[2],

            nextCheckpointsSize[0],
            nextCheckpointsSize[1],
            nextCheckpointsSize[2]
        });

        Throttle = ((float)outputs[0] + 1) / 2f;
        Pedals = (float)outputs[1];
        Cyclic = new((float)outputs[2], (float)outputs[3]);
    }


    public void OnCollisionEnter(Collision collision)
    {
        // Do not destroy test drones.
        if (UsingLoadedWeights) return;

        AIManager.Instance.Kill(this);
        IsDead = true;
    }
}
