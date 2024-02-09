using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneController : MonoBehaviour
{
    public float MotorSpeed = 100f;
    public float MaxThrottle = 2.5f;

    [SerializeField] private Transform _frontLeftMotor;
    [SerializeField] private Transform _frontRightMotor;
    [SerializeField] private Transform _backLeftMotor;
    [SerializeField] private Transform _backRightMotor;

    private Rigidbody _rb;

    private Vector3 _upDirection;

    private float _currentThrottle = 0f;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _upDirection = transform.forward;
    }

    private void FixedUpdate()
    {
        UpdateThrottle();
        Debug.Log(_currentThrottle);
        ApplyThrust(_currentThrottle, _currentThrottle, _currentThrottle, _currentThrottle);
    }

    private float UpdateThrottle()
    {
        _currentThrottle += Input.GetAxis("Vertical");
        _currentThrottle = Mathf.Clamp( _currentThrottle, 0, MaxThrottle);

        return _currentThrottle;
    }

    public void ApplyThrust(float frontLeft, float frontRight, float backLeft, float backRight)
    {
        _rb.AddForceAtPosition(BaseThrust() * frontLeft, _frontLeftMotor.position);
        _rb.AddForceAtPosition(BaseThrust() * frontRight, _frontRightMotor.position);
        _rb.AddForceAtPosition(BaseThrust() * backLeft, _backLeftMotor.position);
        _rb.AddForceAtPosition(BaseThrust() * backRight, _backRightMotor.position);

        Vector3 BaseThrust() => _upDirection * MotorSpeed * Time.fixedDeltaTime;
    }
}
