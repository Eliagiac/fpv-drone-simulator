using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneController : MonoBehaviour
{
    public float MotorSpeed = 100f;

    [SerializeField] private Transform _frontLeftMotor;
    [SerializeField] private Transform _frontRightMotor;
    [SerializeField] private Transform _backLeftMotor;
    [SerializeField] private Transform _backRightMotor;

    private Rigidbody _rb;

    private Vector3 _upDirection;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _upDirection = transform.forward;
    }

    private void FixedUpdate()
    {
        _rb.AddForceAtPosition(_upDirection * MotorSpeed * Time.fixedDeltaTime, _frontLeftMotor.position);
        _rb.AddForceAtPosition(_upDirection * MotorSpeed * Time.fixedDeltaTime, _frontRightMotor.position);
        _rb.AddForceAtPosition(_upDirection * MotorSpeed * Time.fixedDeltaTime, _backLeftMotor.position);
        _rb.AddForceAtPosition(_upDirection * MotorSpeed * Time.fixedDeltaTime, _backRightMotor.position);
    }
}
