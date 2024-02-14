using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AIManager : MonoBehaviour
{
    public static readonly int[] NetworkSize = { 20, 16, 12, 8, 4 };
    public static AIManager Instance;

    public int Population = 100;

    [Header("References")]
    [SerializeField] private TextMeshProUGUI _gui;
    [SerializeField] private Toggle _showBestDroneToggle;
    [SerializeField] private GameObject _dronePrefab;

    private float _genTimer = 0;
    private int _genCount = 0;
    private List<AIController> _genDrones;
    private List<AIController> _previousGenDrones;

    private float _updateGuiTimer = 0;

    private static int s_currentWeightSaveFileIndex;

    private float GenDuration => 5 + (_genCount * 0.01f);
    public static string WeightsFilePath => Application.persistentDataPath + "/weights" + s_currentWeightSaveFileIndex + ".txt";


    public void Kill(AIController drone)
    {
        // If the population gets too small, skip to the next generation.
        if (_previousGenDrones.Count <= Population / 5)
        {
            _genTimer = GenDuration;
            return;
        }

        _previousGenDrones.Remove(drone);
        drone.gameObject.SetActive(false);
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
        _updateGuiTimer += Time.deltaTime;

        if (_genTimer >= GenDuration) 
        {
            _genTimer = 0;
            _genCount++;

            ResetPopulation(false);
        }

        if (_updateGuiTimer >= 0.1)
        {
            if (_previousGenDrones.Count > 0 && _previousGenDrones.All(drone => drone.IsReady))
            {
                _updateGuiTimer = 0;

                _gui.text =
                $"Current generation: {_genCount}\n" +
                $"Duration: {GenDuration}\n" +
                $"Alive: {_previousGenDrones.Count}\n" +
                $"Best fitness: {_previousGenDrones.Max(drone => drone.Fitness())}\n" +
                $"Highest checkpoint reached: {_previousGenDrones.Max(drone => drone.CheckpointsReached())}";
            }

            if (_showBestDroneToggle.isOn)
            {
                List<AIController> orderedDrones = _genDrones.Where(drone => drone != null && drone.IsReady && !drone.IsDead).OrderBy(drone => drone.Fitness()).Reverse().ToList();

                foreach (MeshRenderer rendered in orderedDrones[0].gameObject.GetComponentsInChildren<MeshRenderer>()) rendered.enabled = true;

                foreach (AIController drone in orderedDrones.Skip(1))
                    foreach (MeshRenderer rendered in drone.gameObject.GetComponentsInChildren<MeshRenderer>()) rendered.enabled = false;
            }

            else
            {
                foreach (AIController drone in _genDrones)
                    if (drone != null) foreach (MeshRenderer rendered in drone.gameObject.GetComponentsInChildren<MeshRenderer>()) rendered.enabled = true;
            }
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

                else
                    CreateChildNetwork(Mathf.Min(30, _previousGenDrones.Count));

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
