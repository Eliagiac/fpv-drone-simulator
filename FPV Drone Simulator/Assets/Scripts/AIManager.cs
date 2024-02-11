using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    public int Population = 20;

    [SerializeField] private GameObject _dronePrefab;

    private void Start()
    {
        for (int i = 0; i < Population; i++)
        {
            AIController drone = Instantiate(_dronePrefab, transform).GetComponent<AIController>();

            drone.NeuralNetwork = new(new[] { 18, 13, 4 });
            drone.NeuralNetwork.RandomizeWeightsAndBiases(2, 5);
        }
    }

}
