using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    public int Population = 20;

    [SerializeField] private GameObject _dronePrefab;

    private float _genTimer = 0;
    private int _genCount = 0;
    private AIController[] _genDrones;
    private AIController[] _previousGenDrones;

    private void Start()
    {
        ResetPopulation();
    }

    public void Update()
    {
        _genTimer += Time.deltaTime;
        
        if (_genTimer >= 5) 
        {
            _genTimer = 0;
            _genCount++;

            ResetPopulation(false);
        }
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
            // Sort the drones by fitness score in descending order (higher is better).
            _previousGenDrones = _previousGenDrones.OrderBy(drone => drone.Fitness()).Reverse().ToArray();
            Debug.Log($"Best fitness in generation {_genCount}: {_previousGenDrones[0].Fitness()}");
        }

        System.Random rng = new System.Random();

        _genDrones = new AIController[Population];
        for (int i = 0; i < Population; i++)
        {
            AIController drone = Instantiate(_dronePrefab, transform).GetComponent<AIController>();

            drone.NeuralNetwork = new(new[] { 18, 13, 4 });

            if (random) drone.NeuralNetwork.RandomizeWeightsAndBiases(0.2, 0.5);

            else
            {
                if (i < 1) drone.NeuralNetwork = new(_previousGenDrones[0].NeuralNetwork);

                else if (i < Mathf.Round(Population * 0.4f)) 
                    CreateChildNetwork(Mathf.Min(5, _previousGenDrones.Length));

                else if (i < Mathf.Round(Population * 0.4f)) 
                    CreateChildNetwork(Mathf.Min(10, _previousGenDrones.Length));

                else CreateChildNetwork(Mathf.Min(20, _previousGenDrones.Length));

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

            _genDrones[i] = drone;
        }

        _previousGenDrones = _genDrones;
    }
}
