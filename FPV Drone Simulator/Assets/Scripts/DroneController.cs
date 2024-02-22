using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DroneController : MonoBehaviour
{
    public const int CheckpointsLookahead = 4;

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
    [SerializeField] private float _checkpointReachedTimeWeight = 0.5f;
    [SerializeField] private float _accuracyWeight = 0.5f;
    [SerializeField] private float _angularAccuracyWeight = 0.2f;
    [SerializeField] private float _angularDistanceToNextCheckpointMaxBonus = 0.5f;
    [SerializeField] private float _verticalDistanceToNextCheckpointMaxBonus = 0.1f;
    [SerializeField] private float _maxElevationWeight = 0.1f;
    [SerializeField] private float _totalAngleTravelledWeight = 0.03f;
    [SerializeField] private float _checkpointPassedPenalty = 0.1f;
    [SerializeField] private float _stayedAliveBonus = 1f;

    [Header("Stats")]
    [SerializeField] protected int NextCheckpoint;


    protected Rigidbody _rb;
    private List<Transform> _checkpoints = new();
    private List<Transform> _checkpointsReached = new();
    private List<float> _timeToReachCheckpoints = new() { 0 };
    private List<float> _distanceToCheckpointsCenter = new();
    private List<float> _angularDistanceToCheckpointsCenter = new();
    private float _maxElevationReached;
    private Vector3 _previousRotation;
    private float _totalAngleTravelled;
    private bool _checkpointCurrentlyPassed;
    private int _checkpointPassedCount;


    public Vector2 Cyclic { get; protected set; }
    public float Pedals { get; protected set; }
    public float Throttle { get; protected set; }


    private Vector3 DroneRotation => new(transform.eulerAngles.x + (90 - CameraAngle), transform.eulerAngles.y, transform.eulerAngles.z);
    private Vector3 DroneOrientation => Quaternion.Euler(DroneRotation) * Vector3.forward;

    protected float DroneTilt => Vector3.Angle(Vector3.down, -transform.up);
    protected Vector3 DroneVelocity => _rb.velocity;
    protected Vector3 DroneAngularVelocity => _rb.angularVelocity;
    protected float HeightFromGround
    {
        get
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
                return hit.distance;

            else return 0;
        }
    }
    protected float[] DistanceToNextCheckpoints => Enumerable.Range(0, CheckpointsLookahead).Select(i => DistanceToNextCheckpoint(i)).ToArray();
    protected Vector3[] RelativeDirectionToNextCheckpoints => Enumerable.Range(0, CheckpointsLookahead).Select(i => RelativeDirectionToNextCheckpoint(i)).ToArray();
    protected Vector3[] RelativeRotationOfNextCheckpoints => Enumerable.Range(0, CheckpointsLookahead).Select(i => RelativeRotationOfNextCheckpoint(i)).ToArray();
    protected float[] NextCheckpointsSize => Enumerable.Range(0, CheckpointsLookahead).Select(i => NextCheckpointSize(i)).ToArray();


    public bool IsReady { get; protected set; }
    public bool IsDead { get; protected set; }
    public bool IsPastCheckpoint => _checkpointCurrentlyPassed;


    protected virtual void Start()
    {
        _rb = GetComponent<Rigidbody>();

        foreach (Transform checkpoint in GameObject.FindGameObjectWithTag("Checkpoints Parent").transform)
            _checkpoints.Add(checkpoint);

        ResetRotation();
        _previousRotation = transform.eulerAngles;
    }

    protected virtual void Update()
    {
        _timeToReachCheckpoints[^1] += Time.deltaTime;


        Vector3 angleTravelled = transform.eulerAngles - _previousRotation;

        _totalAngleTravelled +=
            NormalizeAngle(Mathf.Abs(angleTravelled.x)) +
            NormalizeAngle(Mathf.Abs(angleTravelled.y)) +
            NormalizeAngle(Mathf.Abs(angleTravelled.y));

        _previousRotation = transform.eulerAngles;


        float NormalizeAngle(float angle) => angle > 180 ? angle - 360 : angle;


        Vector3 directionToNextCheckpoint = (_checkpoints[NextCheckpoint].position - transform.position).normalized;

        // Turn the rotation of the checkpoint to a unit vector representing its direction.
        Vector3 checkpointDirection = Quaternion.Euler(_checkpoints[NextCheckpoint].eulerAngles) * Vector3.forward;

        // Determine wether the drone has passed the checkpoint.
        bool passed = Vector3.Angle(directionToNextCheckpoint, checkpointDirection) > 90;

        if (passed && !_checkpointCurrentlyPassed) _checkpointPassedCount++;
        _checkpointCurrentlyPassed = passed;
    }

    protected virtual void FixedUpdate()
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
            _timeToReachCheckpoints.Add(0);

            _distanceToCheckpointsCenter.Add((other.transform.position - transform.position).magnitude);
            _angularDistanceToCheckpointsCenter.Add(
                Vector3.Angle(
                    DroneOrientation,
                    Quaternion.Euler(other.transform.eulerAngles) * Vector3.forward)
            );

            _checkpointsReached.Add(other.transform);

            // Kill the drone if this is the last checkpoint. The fitness at the time it reached it will be used.
            if (NextCheckpoint == _checkpoints.Count && this is AIController)
            {
                AIManager.Instance.Kill((AIController)this);
            }
        }
    }


    public double Fitness()
    {
        double score = 0;

        // Add a bonus based on the distance to the next checkpoint (lower is better).
        // The bonus is calculated with a sigmoid function and can reach up to just
        // below _checkpointReachedWeight when the distance is close to 0.
        score += DistanceBonus(DistanceToNextCheckpoint(0), _checkpointReachedWeight);

        // Add a bonus based on how close the drone is to the path leading to the next checkpoint.
        score += DistanceBonus(DistanceToCheckpointPath(), _distanceToCheckpointPathMaxBonus);

        // Add a bonus for how close the drone's orientation is to that of the next checkpoint.
        score += DistanceBonus(AngularDistanceToNextCheckpoint(), _angularDistanceToNextCheckpointMaxBonus, 0.04);

        score += DistanceBonus(VerticalDistanceToNextCheckpoint(), _verticalDistanceToNextCheckpointMaxBonus, 0.5);

        score += MaxElevationScore();

        score -= TotalAngleTravelledScore();

        score -= CheckpointPassedPenalty();

        if (gameObject.activeSelf) score += _stayedAliveBonus;

        for (int i = 0; i < NextCheckpoint; i++)
        {
            score += _checkpointReachedWeight;

            score += DistanceBonus(_distanceToCheckpointsCenter[i], _accuracyWeight, 0.5);
            score += DistanceBonus(_angularDistanceToCheckpointsCenter[i], _angularAccuracyWeight, 0.04);

            score -= Mathf.Pow(_timeToReachCheckpoints[i] / 10f, 2) * _checkpointReachedTimeWeight;
        }

        for (int i = 0; i < NextCheckpoint; i++) score *= _checkpointsReachedMultiplier;

        return score;


        double DistanceBonus(double distance, float maxBonus, double slope = 0.1) => (maxBonus + 0.5 * maxBonus) * (1 / (1 + Math.Pow(Math.E, -slope * (-distance + (1 / slope)))));

        double DistanceToCheckpointPath()
        {
            Vector3 nextCheckpointCenter = new(
                _checkpoints[NextCheckpoint].position.x,
                _checkpoints[NextCheckpoint].position.y + (NextCheckpointsSize[0] / 2f),
                _checkpoints[NextCheckpoint].position.z);

            Vector3 directionToNextCheckpoint = (_checkpoints[NextCheckpoint].position - transform.position).normalized;

            // Turn the rotation of the checkpoint to a unit vector representing its direction.
            Vector3 checkpointDirection = Quaternion.Euler(_checkpoints[NextCheckpoint].eulerAngles) * Vector3.forward;

            // First determine wether the drone has passed the checkpoint.
            bool passed = Vector3.Angle(directionToNextCheckpoint, checkpointDirection) > 90;

            // Then find the distance to a ray starting from the checkpoint.
            Ray ray = new Ray(nextCheckpointCenter, passed ? -checkpointDirection : checkpointDirection);
            float distance = Vector3.Cross(ray.direction, transform.position - ray.origin).magnitude;

            return distance;
        }

        double AngularDistanceToNextCheckpoint() => Vector3.Angle(
            DroneOrientation,
            Quaternion.Euler(_checkpoints[NextCheckpoint].eulerAngles) * Vector3.forward
        );

        double VerticalDistanceToNextCheckpoint() => Mathf.Abs((_checkpoints[NextCheckpoint].position.y + (NextCheckpointsSize[0] / 2f)) - transform.position.y);

        double MaxElevationScore() =>
            _maxElevationWeight - Math.Min(_maxElevationWeight, (_maxElevationReached / 20f) * _maxElevationWeight);

        double TotalAngleTravelledScore() =>
            _totalAngleTravelledWeight * _totalAngleTravelled / 100f;

        double CheckpointPassedPenalty() => _checkpointPassedPenalty * _checkpointPassedCount;
    }

    public int CheckpointsReached() => NextCheckpoint;


    protected void ResetRotation(float z = 0) => transform.eulerAngles = new(CameraAngle - 90, 0, z);

    protected void ResetPosition() => transform.position = new(0, 0, 0);


    private float DistanceToNextCheckpoint(int i) =>
        _checkpoints.Count > NextCheckpoint + i ?
        (_checkpoints[NextCheckpoint + i].position - transform.position).magnitude : 0;

    private Vector3 RelativeDirectionToNextCheckpoint(int i) =>
        _checkpoints.Count > NextCheckpoint + i ?
        (Quaternion.Inverse(Quaternion.Euler(DroneRotation)) * 
        (_checkpoints[NextCheckpoint + i].position - transform.position).normalized) : Vector3.zero;

    private Vector3 RelativeRotationOfNextCheckpoint(int i) =>
        _checkpoints.Count > NextCheckpoint + i ?
        Quaternion.FromToRotation(DroneOrientation, 
            _checkpoints[NextCheckpoint + i].forward).eulerAngles : Vector3.zero;

    private float NextCheckpointSize(int i) =>
        _checkpoints.Count > NextCheckpoint + i ?
        (_checkpoints[NextCheckpoint + i].GetChild(0).localScale.x) : 0;


    private void ApplyThrottle()
    {
        Vector3 throttle = transform.forward * Throttle * MotorPower;

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
