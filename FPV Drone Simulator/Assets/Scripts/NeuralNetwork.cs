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


    public void FeedForward()
    {

    }
}
