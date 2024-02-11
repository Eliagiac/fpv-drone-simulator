using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class NeuralNetwork
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

    /// <summary>
    /// Create a child network from two parents.
    /// </summary>
    /// <remarks>
    /// The parents must have the same network size.
    /// </remarks>
    public NeuralNetwork(NeuralNetwork parent1, NeuralNetwork parent2) : this(parent1.GetSize())
    {

    }


    public int[] GetSize() => Nodes.Select(layer => layer.Length).ToArray();


    public void RandomizeWeightsAndBiases(double biasRange, double weightRange)
    {
        Random rng = new Random();
        
        for (int i = 1; i < Nodes.Length; i++)
        {
            for (int j = 0; j < Nodes[i].Length; j++)
            {
                Biases[i][j] = rng.NextDouble() * biasRange;

                for (int k = 0; k < Nodes[i - 1].Length; k++)
                {
                    Weights[i][j][k] = rng.NextDouble() * weightRange;
                }
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
