using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class AIController : DroneController
{
    public static readonly int[] NetworkSize = { 40, 30, 20, 20, 16, 12, 8, 4 };

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
        float[] distanceToNextCheckpoints = DistanceToNextCheckpoints;
        Vector3[] relativeDirectionToNextCheckpoints = RelativeDirectionToNextCheckpoints;
        Vector3[] relativeRotationOfNextCheckpoints = RelativeRotationOfNextCheckpoints;
        float[] nextCheckpointsSize = NextCheckpointsSize;

        // Kill the drone if it gets too far from the next checkpoint.
        if (!IsTestDrone && _maxDistanceToCheckpoint != 0 && distanceToNextCheckpoints[0] > _maxDistanceToCheckpoint)
        {
            AIManager.Instance.Kill(this);
            IsDead = true;
            return;
        }

        // In order for the drone to be independent of the specific layout or position of the track/checkpoints,
        // all inputs need to be relative to both the drone's position and orientation. The only exception is the
        // drone's tilt, used to counter the effects of gravity.
        double[] outputs = NeuralNetwork.FeedForward(new double[]
        {
            DroneTilt,

            HeightFromGround,

            DroneVelocity.x,
            DroneVelocity.y,
            DroneVelocity.z,

            DroneAngularVelocity.x,
            DroneAngularVelocity.y,
            DroneAngularVelocity.z,

            distanceToNextCheckpoints[0],
            distanceToNextCheckpoints[1],
            distanceToNextCheckpoints[2],
            distanceToNextCheckpoints[3],

            relativeDirectionToNextCheckpoints[0].x,
            relativeDirectionToNextCheckpoints[0].y,
            relativeDirectionToNextCheckpoints[0].z,
            relativeDirectionToNextCheckpoints[1].x,
            relativeDirectionToNextCheckpoints[1].y,
            relativeDirectionToNextCheckpoints[1].z,
            relativeDirectionToNextCheckpoints[2].x,
            relativeDirectionToNextCheckpoints[2].y,
            relativeDirectionToNextCheckpoints[2].z,
            relativeDirectionToNextCheckpoints[3].x,
            relativeDirectionToNextCheckpoints[3].y,
            relativeDirectionToNextCheckpoints[3].z,

            relativeRotationOfNextCheckpoints[0].x,
            relativeRotationOfNextCheckpoints[0].y,
            relativeRotationOfNextCheckpoints[0].z,
            relativeRotationOfNextCheckpoints[1].x,
            relativeRotationOfNextCheckpoints[1].y,
            relativeRotationOfNextCheckpoints[1].z,
            relativeRotationOfNextCheckpoints[2].x,
            relativeRotationOfNextCheckpoints[2].y,
            relativeRotationOfNextCheckpoints[2].z,
            relativeRotationOfNextCheckpoints[3].x,
            relativeRotationOfNextCheckpoints[3].y,
            relativeRotationOfNextCheckpoints[3].z,

            nextCheckpointsSize[0],
            nextCheckpointsSize[1],
            nextCheckpointsSize[2],
            nextCheckpointsSize[3],
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
