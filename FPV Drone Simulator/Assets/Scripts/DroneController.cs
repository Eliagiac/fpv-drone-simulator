using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneController : MonoBehaviour
{
    public float MotorSpeed = 100f;
    public float MaxThrottle = 2.5f;

    public Motor FrontLeftMotor;
    public Motor FrontRightMotor;
    public Motor BackLeftMotor;
    public Motor BackRightMotor;

    [SerializeField] private Transform _frontLeftMotorTransform;
    [SerializeField] private Transform _frontRightMotorTransform;
    [SerializeField] private Transform _backLeftMotorTransform;
    [SerializeField] private Transform _backRightMotorTransform;

    private Rigidbody _rb;


    private void Start()
    {
        _rb = GetComponent<Rigidbody>();

        FrontLeftMotor = new(_frontLeftMotorTransform.position);
        FrontRightMotor = new(_frontRightMotorTransform.position);
        BackLeftMotor = new(_backLeftMotorTransform.position);
        BackRightMotor = new(_backRightMotorTransform.position);
    }

    private void Update()
    {
        Throttle(Mathf.Clamp(Input.GetAxis("Vertical"), 0, MaxThrottle));
    }

    private void FixedUpdate()
    {
        ApplyThrust();
    }


    private void Throttle(float amount)
    {
        FrontLeftMotor.SetPower(amount);
        FrontRightMotor.SetPower(amount);
        BackLeftMotor.SetPower(amount);
        BackRightMotor.SetPower(amount);
    }

    private void Pitch(float amount)
    {
        // Pitch upwards.
        if (amount > 0)
        {
            FrontLeftMotor.AddPower(amount);
            FrontRightMotor.AddPower(amount);
        }

        // Pitch downwards.
        else
        {
            BackLeftMotor.AddPower(amount);
            BackRightMotor.AddPower(amount);
        }
    }

    private void Roll(float amount)
    {
        // Roll right.
        if (amount > 0) 
        {
            FrontLeftMotor.AddPower(amount);
            BackLeftMotor.AddPower(amount);
        }

        // Roll left.
        else
        {
            FrontRightMotor.AddPower(amount);
            BackRightMotor.AddPower(amount);
        }
    }

    private void Yaw(float amount)
    {
        // Yaw right.
        if (amount > 0)
        {
            FrontRightMotor.AddPower(amount);
            BackLeftMotor.AddPower(amount);
        }

        // Yaw left.
        else
        {
            FrontLeftMotor.AddPower(amount);
            BackRightMotor.AddPower(amount);
        }
    }

    private void ApplyThrust()
    {
        Debug.Log($"{FrontLeftMotor.GetPower()} {FrontRightMotor.GetPower()} {BackLeftMotor.GetPower()} {BackRightMotor.GetPower()}");

        _rb.AddForceAtPosition(BaseThrust() * FrontLeftMotor.GetPower(), FrontLeftMotor.Position);
        _rb.AddForceAtPosition(BaseThrust() * FrontRightMotor.GetPower(), FrontRightMotor.Position);
        _rb.AddForceAtPosition(BaseThrust() * BackLeftMotor.GetPower(), BackLeftMotor.Position);
        _rb.AddForceAtPosition(BaseThrust() * BackRightMotor.GetPower(), BackRightMotor.Position);
    }

    private Vector3 BaseThrust() => transform.forward * MotorSpeed * Time.fixedDeltaTime;
}
