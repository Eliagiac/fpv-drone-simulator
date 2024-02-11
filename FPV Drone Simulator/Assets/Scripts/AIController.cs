using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIController : DroneController
{
    public NeuralNetwork NeuralNetwork;

    private void Update()
    {
        Vector3[] distanceToNextCheckpoints = DistanceToNextCheckpoints;
        float[] angularDistanceToNextCheckpoints = AngularDistanceToNextCheckpoints;
        float[] nextCheckpointsSize = NextCheckpointsSize;


        double[] outputs = NeuralNetwork.FeedForward(new double[]
        {
            HorizontalVelocity,
            VerticalVelocity,
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
}
