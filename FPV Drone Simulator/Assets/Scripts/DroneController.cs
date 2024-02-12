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

    [Header("Fitness Function Weights")]
    [SerializeField, Range(1, 10)] private float _checkpointsReachedMultiplier = 2f;
    [SerializeField] private float _checkpointReachedWeight = 1f;
    [SerializeField] private float _elevationWeight = 0.1f;

    [Header("Stats")]
    [SerializeField] protected int NextCheckpoint;


    protected Rigidbody _rb;
    private List<Transform> _checkpoints = new();
    private List<Transform> _checkpointsReached = new();


    public Vector2 Cyclic { get; protected set; }
    public float Pedals { get; protected set; }
    public float Throttle { get; protected set; }


    protected float DroneAngleX => transform.eulerAngles.x;
    protected float DroneAngleY => transform.eulerAngles.y;
    protected float DroneAngleZ => transform.eulerAngles.z;
    protected float DroneVelocityX => _rb.velocity.x;
    protected float DroneVelocityY => _rb.velocity.y;
    protected float DroneVelocityZ => _rb.velocity.z;
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


    protected virtual void Start()
    {
        _rb = GetComponent<Rigidbody>();

        foreach (Transform checkpoint in GameObject.FindGameObjectWithTag("Checkpoints Parent").transform)
            _checkpoints.Add(checkpoint);

        ResetRotation();
    }

    private void FixedUpdate()
    {
        ApplyThrottle();
        ApplyRotation();
    }


    public void OnTriggerEnter(Collider other)
    {
        if (other.transform == _checkpoints[NextCheckpoint])
        {
            NextCheckpoint++;
            _checkpointsReached.Add(other.transform);
        }
    }


    public double Fitness()
    {
        double score = 0;

        // Add a bonus based on the distance to the next checkpoint (lower is better).
        // The bonus is calculated with a sigmoid function and can reach up to just
        // below _checkpointReachedWeight when the distance is close to 0.
        score += (_checkpointReachedWeight + 0.5 * _checkpointReachedWeight) * (1 / (1 + Math.Pow(Math.E, -0.1 * (-DistanceToNextCheckpoint(0).magnitude + 10))));

        score += _elevationWeight - Math.Min(_elevationWeight, (HeightFromGround / 20f) * _elevationWeight);

        for (int i = 0; i < NextCheckpoint; i++)
        {
            score += _checkpointReachedWeight;
            score *= _checkpointsReachedMultiplier;
        }

        return score;
    }


    protected void ResetRotation(float z = 0) => transform.eulerAngles = new(CameraAngle - 90, 0, z);

    protected void ResetPosition() => transform.position = new(0, 0, 0);


    private Vector3 DistanceToNextCheckpoint(int i) =>
        _checkpoints.Count > NextCheckpoint + i ?
        (_checkpoints[NextCheckpoint + i].position - transform.position) : Vector3.zero;

    private float AngularDistanceToNextCheckpoint(int i)
    {
        float angle = _checkpoints.Count > NextCheckpoint + i ?
        (_checkpoints[NextCheckpoint + i].eulerAngles.y - transform.eulerAngles.y) : 0;

        // Normalize the distance in the range -180 to 180 degrees.
        return angle > 180 ? angle : angle - 360;
    }

    private float NextCheckpointSize(int i) =>
        _checkpoints.Count > NextCheckpoint + i ?
        (_checkpoints[NextCheckpoint + i].GetChild(0).localScale.x) : 0;


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
