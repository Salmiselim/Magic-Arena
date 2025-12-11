// XRDoor_Fixed.cs – Stops perfectly at open/close angles
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class XRDoor_Fixed : MonoBehaviour
{
    [Header("Door Settings")]
    public float openAngle = 100f;      // How far it opens (degrees)
    public float openSpeed = 150f;
    public float closeSpeed = 100f;

    [Header("Handle")]
    public XRSimpleInteractable handle;

    private HingeJoint hinge;
    private JointMotor motor;
    private bool isOpen = false;

    void Awake()
    {
        hinge = GetComponentInChildren<HingeJoint>();

        // Important limits so the motor knows when to stop
        hinge.useLimits = true;
        JointLimits limits = hinge.limits;
        limits.min = 0f;
        limits.max = openAngle;
        hinge.limits = limits;

        motor = hinge.motor;
        motor.force = 5000f;
        motor.freeSpin = false;
        hinge.useMotor = false;
    }

    void OnEnable() => handle?.activated.AddListener(ToggleDoor);
    void OnDisable() => handle?.activated.RemoveListener(ToggleDoor);

    void ToggleDoor(ActivateEventArgs args)
    {
        if (isOpen)
            CloseDoor();
        else
            OpenDoor();

        // Haptic feedback
        if (args.interactorObject.transform.TryGetComponent<XRController>(out var controller))
            controller.SendHapticImpulse(0.6f, 0.15f);
    }

    public void OpenDoor()
    {
        isOpen = true;
        motor.targetVelocity = openSpeed;
        hinge.motor = motor;
        hinge.useMotor = true;
    }

    void CloseDoor()
    {
        isOpen = false;
        motor.targetVelocity = -closeSpeed;
        hinge.motor = motor;
        hinge.useMotor = true;
    }

    // This runs every frame and stops the motor when we hit the limit
    void FixedUpdate()
    {
        float currentAngle = hinge.angle;

        if (isOpen && currentAngle >= openAngle - 2f)
        {
            StopMotor();
        }
        else if (!isOpen && currentAngle <= 2f)
        {
            StopMotor();
        }
    }

    void StopMotor()
    {
        hinge.useMotor = false;
    }
}