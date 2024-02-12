using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
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
    [SerializeField, Range(1, 10)] private float _checkpointsReachedMultiplier = 5f;
    [SerializeField] private float _distanceToCheckpointPathMaxBonus = 2f;
    [SerializeField] private float _checkpointReachedWeight = 1f;
    [SerializeField] private float _maxElevationWeight = 0.1f;

    [Header("Stats")]
    [SerializeField] protected int NextCheckpoint;


    protected Rigidbody _rb;
    private List<Transform> _checkpoints = new();
    private List<Transform> _checkpointsReached = new();
    private float _maxElevationReached;


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
    protected Vector3[] NextCheckpointsPositionDifference => Enumerable.Range(0, 3).Select(i => NextCheckpointPositionDifference(i)).ToArray();
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

        _maxElevationReached = Math.Max(_maxElevationReached, HeightFromGround);
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
        score += DistanceBonus(NextCheckpointPositionDifference(0).magnitude, _checkpointReachedWeight);

        // Add a bonus based on how close the drone is to the path leading to the next checkpoint.
        score += DistanceBonus(DistanceToCheckpointPath(), _distanceToCheckpointPathMaxBonus);

        score += _maxElevationWeight - Math.Min(_maxElevationWeight, (_maxElevationReached / 20f) * _maxElevationWeight);

        for (int i = 0; i < NextCheckpoint; i++)
        {
            score += _checkpointReachedWeight;
            score *= _checkpointsReachedMultiplier;
        }

        return score;


        double DistanceBonus(double distance, float maxBonus) => (maxBonus + 0.5 * maxBonus) * (1 / (1 + Math.Pow(Math.E, -0.1 * (-distance + 10))));

        double DistanceToCheckpointPath()
        {
            Vector3 nextCheckpointCentre = new(
                _checkpoints[NextCheckpoint].position.x,
                _checkpoints[NextCheckpoint].position.y + (NextCheckpointsSize[0] / 2f),
                _checkpoints[NextCheckpoint].position.z);

            Vector3 directionToNextCheckpoint = (_checkpoints[NextCheckpoint].position - transform.position).normalized;
            Vector3 checkpointDirection = _checkpoints[NextCheckpoint].eulerAngles;

            // First determine wether the drone has passed the checkpoint.
            bool passed = Vector3.Angle(directionToNextCheckpoint, checkpointDirection) > 90;

            // Then find the distance to a ray starting from the checkpoint.
            Ray ray = new Ray(nextCheckpointCentre, passed ? -checkpointDirection : checkpointDirection);
            float distance = Vector3.Cross(ray.direction, transform.position - ray.origin).magnitude;

            return distance;
        }
    }


    protected void ResetRotation(float z = 0) => transform.eulerAngles = new(CameraAngle - 90, 0, z);

    protected void ResetPosition() => transform.position = new(0, 0, 0);


    private Vector3 NextCheckpointPositionDifference(int i) =>
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
