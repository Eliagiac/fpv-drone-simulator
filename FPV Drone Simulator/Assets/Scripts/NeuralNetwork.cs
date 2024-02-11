using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeuralNetwork : MonoBehaviour
{
    public double[][] Nodes;
    public double[][] Biases;
    public double[][][] Weights;

    public NeuralNetwork(int[] size) 
    {
        int layers = size.Length;

        Nodes = new double[layers][];
        Biases = new double[layers][];
        Weights = new double[layers][][];

        for (int i = 0; i < layers; i++)
        {
            Nodes[i] = new double[size[i]];
            Biases[i] = new double[size[i]];
            Weights[i] = new double[size[i]][];

            if (i > 0)
            {
                for (int j = 0; j < size[i]; j++) Weights[i][j] = new double[size[i - 1]];
            }
        }
    }


    public void FeedForward(double[] inputs)
    {
        if (inputs.Length != Nodes.Length) throw new ArgumentException(
            "The size of the inputs array does not match the size of the first layer of the network!");

        Nodes[0] = inputs;

        for (int i = 1; i < Nodes.Length; i++) 
        {
            for (int j = 0; j < Nodes[i].Length; j++)
            {
                double weightedSum = Biases[i][j];

                for (int k = 0; k < Nodes[i - 1].Length; k++)
                {
                    weightedSum += Nodes[i - 1][k] * Weights[i][j][k];
                }

                Nodes[i][j] = ActivationFunction(weightedSum);
            }
        }
    }

    private double ActivationFunction(double input)
    {
        return input;
    }
}
