using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [Header("Stats")]
    [SerializeField] protected int NextCheckpoint;


    protected Rigidbody _rb;
    private Transform[] _checkpoints;


    public Vector2 Cyclic { get; protected set; }
    public float Pedals { get; protected set; }
    public float Throttle { get; protected set; }


    protected float HorizontalVelocity => _rb.velocity.x;
    protected float VerticalVelocity => _rb.velocity.y;
    protected float HeightFromGround
    {
        get
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
                return hit.distance;

            else return 0;
        }
    }
    protected Vector3[] DistanceToNextCheckpoints => Enumerable.Range(0, 3).Select(i => DistanceToNextCheckpoint(i)).ToArray();
    protected float[] AngularDistanceToNextCheckpoints => Enumerable.Range(0, 3).Select(i => AngularDistanceToNextCheckpoint(i)).ToArray();
    protected float[] NextCheckpointsSize => Enumerable.Range(0, 3).Select(i => NextCheckpointSize(i)).ToArray();


    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _checkpoints = GameObject.FindGameObjectWithTag("Checkpoints").GetComponentsInChildren<Transform>();

        ResetRotation();
    }

    private void FixedUpdate()
    {
        ApplyThrottle();
        ApplyRotation();
    }


    protected void ResetRotation(float z = 0) => transform.eulerAngles = new(CameraAngle - 90, 0, z);

    protected void ResetPosition() => transform.position = new(0, 0, 0);


    private Vector3 DistanceToNextCheckpoint(int i) =>
        _checkpoints.Length > NextCheckpoint + i ?
        (_checkpoints[NextCheckpoint].position - transform.position) : Vector3.zero;

    private float AngularDistanceToNextCheckpoint(int i) =>
        _checkpoints.Length > NextCheckpoint + i ?
        (_checkpoints[NextCheckpoint].eulerAngles.y - transform.eulerAngles.y) : 0;

    private float NextCheckpointSize(int i) =>
        _checkpoints.Length > NextCheckpoint + i ?
        (_checkpoints[NextCheckpoint].GetChild(0).localScale.x) : 0;


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
}
