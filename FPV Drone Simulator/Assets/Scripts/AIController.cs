using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class AIController : DroneController
{
    public static readonly int[] NetworkSize = { 15, 12, 10, 8, 4 };

    public NeuralNetwork NeuralNetwork;

    [SerializeField] private float _maxDistanceToCheckpoint = 50;

    [SerializeField] private string _weightsFilePath = "";

    private bool IsTestDrone => _weightsFilePath != "";


    protected override void Start()
    {
        base.Start();
        if (IsTestDrone) NeuralNetwork = new NeuralNetwork(NetworkSize, _weightsFilePath);
        IsReady = true;
    }

    protected override void FixedUpdate()
    {
        Vector3[] distanceToNextCheckpoints = NextCheckpointsPositionDifference;
        float[] angularDistanceToNextCheckpoints = AngularDistanceToNextCheckpoints;
        float[] nextCheckpointsSize = NextCheckpointsSize;

        // Kill the drone if it gets too far from the next checkpoint.
        if (!IsTestDrone && _maxDistanceToCheckpoint != 0 && distanceToNextCheckpoints[0].magnitude > _maxDistanceToCheckpoint)
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
            //distanceToNextCheckpoints[2].x,
            //distanceToNextCheckpoints[2].y,
            //distanceToNextCheckpoints[2].z,

            angularDistanceToNextCheckpoints[0],
            angularDistanceToNextCheckpoints[1],
            //angularDistanceToNextCheckpoints[2],

            nextCheckpointsSize[0],
            nextCheckpointsSize[1],
            //nextCheckpointsSize[2]
        });

        Throttle = ((float)outputs[0] + 1) / 2f;
        Pedals = (float)outputs[1];
        Cyclic = new((float)outputs[2], (float)outputs[3]);


        base.FixedUpdate();
    }


    public void OnCollisionEnter(Collision collision)
    {
        // Do not destroy test drones.
        if (IsTestDrone) return;

        AIManager.Instance.Kill(this);
        IsDead = true;
    }
}
