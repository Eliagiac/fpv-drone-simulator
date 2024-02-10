using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DroneController : MonoBehaviour
{
    [Header("Drone Settings")]
    public float CameraAngle = 35f;

    public float MotorPower = 100f;

    public float MaxPitch = 1.0f;
    public float MaxRoll = 1.0f;
    public float MaxYaw = 1.0f;


    [Header("References")]
    [SerializeField] private Transform _camera;


    private Rigidbody _rb;


    public Vector2 Cyclic { get; private set; }
    public float Pedals { get; private set; }
    public float Throttle { get; private set; }


    private void Start()
    {
        _rb = GetComponent<Rigidbody>();

        ResetRotation();
        _camera.localEulerAngles = new(90 - CameraAngle, 0, 0);
    }

    private void FixedUpdate()
    {
        ApplyThrottle();
        ApplyRotation();
    }


    private void ApplyThrottle()
    {
        Vector3 throttle = transform.forward * Throttle * MotorPower * Time.fixedDeltaTime;

        _rb.AddForce(throttle);
    }

    private void ApplyRotation()
    {
        float pitch = -Cyclic.y * MaxPitch * Time.fixedDeltaTime;
        float yaw = Cyclic.x * MaxYaw * Time.fixedDeltaTime;
        float roll = Pedals * MaxRoll * Time.fixedDeltaTime;

        _rb.MoveRotation(transform.rotation * Quaternion.Euler(pitch, roll, yaw));
    }

    private void ResetRotation(float z = 0) => transform.eulerAngles = new(CameraAngle - 90, 0, z);

    private void ResetPosition() => transform.position = new(0, 0, 0);


    private void OnCyclic(InputValue value) => Cyclic = value.Get<Vector2>();

    private void OnPedals(InputValue value) => Pedals = value.Get<float>();

    // The throttle is bewteen 0 and 1, not -1 and 1.
    private void OnThrottle(InputValue value) => Throttle = (value.Get<float>() + 1) / 2f;

    private void OnResetRotation() => ResetRotation(transform.eulerAngles.z);

    private void OnResetPosition()
    {
        ResetRotation();
        ResetPosition();

        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }
}
