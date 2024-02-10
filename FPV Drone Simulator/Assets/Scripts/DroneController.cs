using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DroneController : MonoBehaviour
{
    public float MotorPower = 100f;

    public float MaxThrottle = 2.5f;

    public float MaxPitch = 1.0f;
    public float MaxRoll = 1.0f;
    public float MaxYaw = 1.0f;

    private Rigidbody _rb;

    public Vector2 Cyclic { get; private set; }
    public float Pedals { get; private set; }
    public float Throttle { get; private set; }


    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        ApplyThrust();
        ApplyRotation();
    }


    private void OnCyclic(InputValue value) => Cyclic = value.Get<Vector2>();

    private void OnPedals(InputValue value) => Pedals = value.Get<float>();

    // The throttle is bewteen 0 and 1, not -1 and 1.
    private void OnThrottle(InputValue value) => Throttle = (value.Get<float>() + 1) / 2f;

    //private void Throttle(float amount)
    //{
    //    FrontLeftMotor.SetPower(amount);
    //    FrontRightMotor.SetPower(amount);
    //    BackLeftMotor.SetPower(amount);
    //    BackRightMotor.SetPower(amount);
    //}

    //private void Pitch(float amount)
    //{
    //    // Pitch upwards.
    //    if (amount > 0)
    //    {
    //        FrontLeftMotor.AddPower(amount);
    //        FrontRightMotor.AddPower(amount);
    //    }

    //    // Pitch downwards.
    //    else
    //    {
    //        BackLeftMotor.AddPower(amount);
    //        BackRightMotor.AddPower(amount);
    //    }
    //}

    //private void Roll(float amount)
    //{
    //    // Roll right.
    //    if (amount > 0) 
    //    {
    //        FrontLeftMotor.AddPower(amount);
    //        BackLeftMotor.AddPower(amount);
    //    }

    //    // Roll left.
    //    else
    //    {
    //        FrontRightMotor.AddPower(amount);
    //        BackRightMotor.AddPower(amount);
    //    }
    //}

    //private void Yaw(float amount)
    //{
    //    // Yaw right.
    //    if (amount > 0)
    //    {
    //        FrontRightMotor.AddPower(amount);
    //        BackLeftMotor.AddPower(amount);
    //    }
    //
    //    // Yaw left.
    //    else
    //    {
    //        FrontLeftMotor.AddPower(amount);
    //        BackRightMotor.AddPower(amount);
    //    }
    //}

    private void ApplyThrust()
    {
        float throttle = Throttle * MaxThrottle;

        _rb.AddForce(BaseThrust() * throttle);
    }

    private void ApplyRotation()
    {

        float pitch = Cyclic.y * MaxPitch;
        float yaw = Pedals * MaxYaw;
        float roll = Cyclic.x * MaxRoll;

        _rb.MoveRotation(transform.rotation * Quaternion.Euler(pitch, yaw, roll));
    }

    private Vector3 BaseThrust() => transform.forward * MotorPower * Time.fixedDeltaTime;
}
