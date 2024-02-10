using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Motor
{
    public Vector3 Position;

    private float _currentPower;

    public Motor(Vector3 position)
    {
        Position = position;
    }


    public float GetPower() => _currentPower;

    public void SetPower(float power) => _currentPower = power;

    public void AddPower(float power) => _currentPower += power;
}
