using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIController : DroneController
{
    public NeuralNetwork NeuralNetwork;

    private void Update()
    {
        double[] outputs = NeuralNetwork.FeedForward(new double[]
        {
            HorizontalVelocity,
            VerticalVelocity,
            HeightFromGround,
            DistanceToNextCheckpoints[0].x,
            DistanceToNextCheckpoints[0].y,
            DistanceToNextCheckpoints[0].z,
            DistanceToNextCheckpoints[1].x,
            DistanceToNextCheckpoints[1].y,
            DistanceToNextCheckpoints[1].z,
            DistanceToNextCheckpoints[2].x,
            DistanceToNextCheckpoints[2].y,
            DistanceToNextCheckpoints[2].z,
            AngularDistanceToNextCheckpoints[0],
            AngularDistanceToNextCheckpoints[1],
            AngularDistanceToNextCheckpoints[2],
            NextCheckpointsSize[0],
            NextCheckpointsSize[1],
            NextCheckpointsSize[2]
        });

        Throttle = ((float)outputs[0] + 1) / 2f;
        Pedals = (float)outputs[1];
        Cyclic = new((float)outputs[2], (float)outputs[3]);
    }
}
