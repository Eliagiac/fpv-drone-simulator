using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    public static readonly int[] NetworkSize = { 19, 16, 4 };
    public static AIManager Instance;

    public int Population = 20;

    [Header("References")]
    [SerializeField] private TextMeshProUGUI _gui;
    [SerializeField] private GameObject _dronePrefab;

    private float _genTimer = 0;
    private int _genCount = 0;
    private List<AIController> _genDrones;
    private List<AIController> _previousGenDrones;

    private static int s_currentWeightSaveFileIndex;

    public static string WeightsFilePath => Application.persistentDataPath + "/weights" + s_currentWeightSaveFileIndex + ".txt";


    public void Kill(AIController drone)
    {
        // Always maintain at least half the drones.
        if (_previousGenDrones.Count <= Population / 2) return;

        _previousGenDrones.Remove(drone);
        Destroy(drone.gameObject);
    }


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ResetPopulation();
    }

    public void Update()
    {
        _genTimer += Time.deltaTime;
        
        if (_genTimer >= 3 + (_genCount * 0.2f)) 
        {
            _genTimer = 0;
            _genCount++;

            ResetPopulation(false);
        }

        _gui.text = 
            $"Current generation: {_genCount}\n" +
            $"Alive: {_previousGenDrones.Count}";
    }

    private void ResetPopulation(bool random = true)
    {
        foreach (Transform drone in transform)
        {
            if (drone.GetComponent<AIController>() == null) continue;

            Destroy(drone.gameObject);
        }

        if (!random)
        {
            AIController previousBest = _previousGenDrones[0];

            // Sort the drones by fitness score in descending order (higher is better).
            _previousGenDrones = _previousGenDrones.OrderBy(drone => drone.Fitness()).Reverse().ToList();

            Debug.Log(
                $"Best fitness in generation {_genCount}: {_previousGenDrones[0].Fitness()} \n" +
                $"Previous best is in spot {_previousGenDrones.IndexOf(previousBest)}.");

            SaveWeights(_previousGenDrones[0].NeuralNetwork);
        }

        System.Random rng = new System.Random();

        _genDrones = new();
        for (int i = 0; i < Population; i++)
        {
            AIController drone = Instantiate(_dronePrefab, transform).GetComponent<AIController>();

            if (random)
            {
                drone.NeuralNetwork = new(NetworkSize);
                drone.NeuralNetwork.RandomizeWeightsAndBiases(0.2, 0.5);
            }

            else
            {
                // BUG: Since the best network is always maintained, it should not be possible for the top fitness score
                // to get worse from a generation to another. However, this does happen sometimes, which is not expected.
                if (i < 2) drone.NeuralNetwork = new(_previousGenDrones[i].NeuralNetwork);

                else if (i < Mathf.Round(Population * 0.2f))
                    CreateChildNetwork(Mathf.Min(5, _previousGenDrones.Count));

                else if (i < Mathf.Round(Population * 0.5f))
                    CreateChildNetwork(Mathf.Min(10, _previousGenDrones.Count));

                else if (i < Population - 2)
                    CreateChildNetwork(Mathf.Min(30, _previousGenDrones.Count));

                else
                {
                    drone.NeuralNetwork = new(NetworkSize);
                    drone.NeuralNetwork.RandomizeWeightsAndBiases(0.2, 0.5);
                }

                void CreateChildNetwork(int topLength)
                {
                    int parent1Index = rng.Next(topLength);
                    int parent2Index = rng.Next(topLength);
                    while (parent2Index == parent1Index) parent2Index = rng.Next(topLength);

                    drone.NeuralNetwork = new(
                        _previousGenDrones[parent1Index].NeuralNetwork,
                        _previousGenDrones[parent2Index].NeuralNetwork
                    );
                }
            }

            _genDrones.Add(drone);
        }

        _previousGenDrones = new(_genDrones);
    }

    private void SaveWeights(NeuralNetwork neuralNetwork)
    {
        s_currentWeightSaveFileIndex++;
        neuralNetwork.SaveWeights(WeightsFilePath);
    }
}
