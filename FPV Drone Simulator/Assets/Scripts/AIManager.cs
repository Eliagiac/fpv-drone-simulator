using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AIManager : MonoBehaviour
{
    public static AIManager Instance;

    public static double MutationRate = 0.05;
    public static double MutationScale = 1;

    public int Population = 100;

    /// <summary>
    /// How many drones are preserved on each generation. Higher -> lower risk.
    /// </summary>
    public int Risk = 5;

    [Header("References")]
    [SerializeField] private TextMeshProUGUI _gui;
    [SerializeField] private Toggle _showBestDroneToggle;
    [SerializeField] private Toggle _useCollisionsToggle;
    [SerializeField] private GameObject _dronePrefab;
    [SerializeField] private TextMeshProUGUI _mutationRateText;
    [SerializeField] private TextMeshProUGUI _mutationScaleText;

    private float _genTimer = 0;
    private int _genCount = 0;
    private float _genDuration = 3;
    private List<AIController> _genDrones;
    private List<AIController> _previousGenDrones;

    private float _updateGuiTimer = 0;

    private static int s_currentWeightSaveFileIndex;


    public static string WeightsFilePath => Application.persistentDataPath + "/weights" + s_currentWeightSaveFileIndex + ".txt";


    public void Kill(AIController drone)
    {
        if (!_useCollisionsToggle.isOn) return;
        
        // If the population gets too small, skip to the next generation.
        if (_previousGenDrones.Count <= Population / 5)
        {
            _genTimer = _genDuration;
            return;
        }

        _previousGenDrones.Remove(drone);
        drone.gameObject.SetActive(false);
    }

    public void ChangeDuration(float difference) => _genDuration += difference;

    public void ChangeMutationRate(float difference) => MutationRate += difference;

    public void ChangeMutationScale(float difference) => MutationScale += difference;


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

        if (_genTimer >= _genDuration)
        {
            _genTimer = 0;
            _genCount++;
            _genDuration += 0.02f;

            ResetPopulation(false);
        }

        if (_updateGuiTimer >= 0.1)
        {
            if (_previousGenDrones != null && _previousGenDrones.Count > 0 && _previousGenDrones.All(drone => drone.IsReady))
            {
                _updateGuiTimer = 0;

                _gui.text =
                $"Current generation: {_genCount}\n" +
                $"Duration: {_genDuration:0.00}\n" +
                $"Alive: {_previousGenDrones.Count}\n" +
                $"Best fitness: {_previousGenDrones.Max(drone => drone.Fitness())}\n" +
                $"Highest checkpoint reached: {_previousGenDrones.Max(drone => drone.CheckpointsReached())}";

                _mutationRateText.text = $"Mutation rate: {MutationRate:0.00}";
                _mutationScaleText.text = $"Mutation scale: {MutationScale:0.00}";
            }

            if (_showBestDroneToggle.isOn)
            {
                List<AIController> orderedDrones = _genDrones.Where(drone => drone != null && drone.IsReady && !drone.IsDead).OrderBy(drone => drone.Fitness()).Reverse().ToList();

                if (orderedDrones.Count > 0)
                {
                    foreach (MeshRenderer rendered in orderedDrones[0].gameObject.GetComponentsInChildren<MeshRenderer>()) rendered.enabled = true;

                    foreach (AIController drone in orderedDrones.Skip(1))
                        foreach (MeshRenderer rendered in drone.gameObject.GetComponentsInChildren<MeshRenderer>()) rendered.enabled = false;
                }
            }

            else if (_genDrones != null)
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

        if (_previousGenDrones != null && _previousGenDrones.Count > 0)
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
        if (random || _previousGenDrones.Count == 0)
        {
            for (int i = 0; i < Population; i++)
            {
                AIController drone = Instantiate(_dronePrefab, transform).GetComponent<AIController>();

                drone.NeuralNetwork = new(AIController.NetworkSize);
                drone.NeuralNetwork.RandomizeWeightsAndBiases(0.2, 0.5);

                _genDrones.Add(drone);
            }
        }

        else
        {
            double[] fitnessScores = _previousGenDrones.Select(drone => drone.Fitness()).ToArray();
            double fitnessScoresSum = fitnessScores.Sum();

            for (int i = 0; i < Population; i++)
            {
                AIController drone = Instantiate(_dronePrefab, transform).GetComponent<AIController>();
                
                if (i < Mathf.Min(Risk, _previousGenDrones.Count)) drone.NeuralNetwork = new(_previousGenDrones[i].NeuralNetwork);

                else
                {
                    int parent1 = RandomParentWeightedByFitness();
                    int parent2 = RandomParentWeightedByFitness(parent1);

                    CreateChildNetwork(parent1, parent2);
                }

                _genDrones.Add(drone);


                int RandomParentWeightedByFitness(int excluded = -1)
                {
                    double value = rng.NextDouble() * fitnessScoresSum;

                    for (int i = 0; i < fitnessScores.Length; i++)
                    {
                        value -= fitnessScores[i];

                        if (i == excluded) return i > 0 ? i - 1 : i + i;
                        if (value <= 0) return i;
                    }

                    throw new Exception("Could not select a random parent.");
                }

                void CreateChildNetwork(int parent1, int parent2)
                    {
                        drone.NeuralNetwork = new(
                            _previousGenDrones[parent1].NeuralNetwork,
                            _previousGenDrones[parent2].NeuralNetwork
                        );
                    }
            }
        }

        _previousGenDrones = new(_genDrones);
    }

    private void SaveWeights(NeuralNetwork neuralNetwork)
    {
        s_currentWeightSaveFileIndex++;
        neuralNetwork.SaveWeights(WeightsFilePath);
    }
}
