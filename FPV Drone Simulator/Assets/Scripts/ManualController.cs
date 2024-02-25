using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ManualController : DroneController
{
    [Header("References")]
    [SerializeField] private Transform _camera;
    [SerializeField] private TextMeshProUGUI _gui;

    private float _updateDebugTimer;


    protected override void Start()
    {
        base.Start();
        _camera.localEulerAngles = new(90 - CameraAngle, 0, 0);
        IsReady = true;
    }

    protected override void Update()
    {
        _updateDebugTimer += Time.deltaTime;

        base.Update();
        _gui.text = $"Fitness: {Fitness()}";


        if (_updateDebugTimer > 1f)
        {
            _updateDebugTimer = 0;

            Debug.Log(
                $"Tilt: {DroneTilt}; Velocity: {DroneVelocity}; Angular velocity: {DroneAngularVelocity}; " +
                $"Distance to next 2 checkpoints: {DistanceToNextCheckpoints[0]}, {DistanceToNextCheckpoints[1]}; " +
                $"Relative direction to next 2 checkpoints: {RelativeDirectionToNextCheckpoints[0]}, {RelativeDirectionToNextCheckpoints[1]}; " +
                $"Relative rotation of next 2 checkpoints: {RelativeRotationOfNextCheckpoints[0]}, {RelativeRotationOfNextCheckpoints[1]}; " +
                $"Size of next 2 checkpoints: {NextCheckpointsSize[0]}, {NextCheckpointsSize[1]}"
            );
        }
    }


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
