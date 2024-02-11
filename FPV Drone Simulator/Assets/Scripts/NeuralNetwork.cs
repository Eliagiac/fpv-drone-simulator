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
        Random rng = new Random();

        for (int i = 1; i < Nodes.Length; i++)
        {
            for (int j = 0; j < Nodes[i].Length; j++)
            {
                double bias = rng.NextDouble() < 0.5 ? parent1.Biases[i][j] : parent2.Biases[i][j];

                int threshold = (int)Math.Round(rng.NextDouble() * Nodes[i - 1].Length);

                double[] weights = new double[Nodes[i - 1].Length];
                for (int k = 0; k < weights.Length; k++)
                {
                    if (k < threshold) weights[k] = parent1.Weights[i][j][k];
                    else weights[k] = parent2.Weights[i][j][k];
                }

                Biases[i][j] = MutateBias(bias);
                Weights[i][j] = MutateWeights(weights);
            }
        }

        double MutateBias(double bias)
        {
            // Mutate by a factor from 2 to 5.
            if (rng.NextDouble() < 0.05) bias = bias * (rng.Next(4) + 2);

            return bias;
        }

        double[] MutateWeights(double[] weights)
        {
            int selection = rng.Next(Weights.Length);

            // Mutate the selected weight by a factor from 2 to 5.
            if (rng.NextDouble() < 0.05) weights[selection] = weights[selection] * (rng.Next(4) + 2);

            return weights;
        }
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

    public double[] FeedForward(double[] inputs)
    {
        if (inputs.Length != Nodes[0].Length) throw new ArgumentException(
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

                if (i == Nodes.Length - 1) 
                    Nodes[i][j] = (ComputeNode(weightedSum, ActivationFunction.Sigmoid) * 2) - 1;

                else 
                    Nodes[i][j] = ComputeNode(weightedSum, ActivationFunction.LeakyReLU);
            }
        }

        return Nodes[^1];
    }

    private double ComputeNode(double input, ActivationFunction activationFunction)
    {
        switch (activationFunction)
        {
            case ActivationFunction.ReLU:
                return input > 0 ? input : 0;

            case ActivationFunction.LeakyReLU:
                return input > 0 ? input : input * 0.01;

            case ActivationFunction.Sigmoid:
                return 1 / (1 + Math.Pow(Math.E, -input));

            default:
                throw new NotImplementedException("The selected activation function has no implementation.");
        }
    }

    public enum ActivationFunction
    {
        ReLU,
        LeakyReLU,
        Sigmoid
    }
}
