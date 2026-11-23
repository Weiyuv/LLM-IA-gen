
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerCapsuleController : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Seguir NPC")]
    [SerializeField] private NpcCapsuleController npc;
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float followDistance = 2f;

    [Header("Pulo")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;

    [Header("Câmera")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;
    private Vector3 verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (npc == null)
            npc = FindObjectOfType<NpcCapsuleController>();
    }

    private void Update()
    {
        Move();
        HandleJump();
    }

    private void Move()
    {
        if (npc != null &&
            npc.CurrentIntent == NpcCapsuleController.NPCIntent.missao)
        {
            FollowNPC();
            return;
        }

        MoveWithInput();
    }

    private void FollowNPC()
    {
        if (npc == null)
            return;

        Vector3 direction = npc.transform.position - transform.position;
        direction.y = 0f;

        float distance = direction.magnitude;

        if (distance <= followDistance)
            return;

        direction.Normalize();

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        controller.Move(
            direction * followSpeed * Time.deltaTime
        );
    }

    private void MoveWithInput()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed ||
                Keyboard.current.upArrowKey.isPressed)
                input.y += 1f;

            if (Keyboard.current.sKey.isPressed ||
                Keyboard.current.downArrowKey.isPressed)
                input.y -= 1f;

            if (Keyboard.current.dKey.isPressed ||
                Keyboard.current.rightArrowKey.isPressed)
                input.x += 1f;

            if (Keyboard.current.aKey.isPressed ||
                Keyboard.current.leftArrowKey.isPressed)
                input.x -= 1f;
        }

        if (Gamepad.current != null)
            input += Gamepad.current.leftStick.ReadValue();

        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 direction;

        if (cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            direction = forward * input.y + right * input.x;
        }
        else
        {
            direction = transform.forward * input.y +
                        transform.right * input.x;
        }

        if (direction.sqrMagnitude > 0.001f)
        {
            direction.Normalize();

            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        controller.Move(
            direction * moveSpeed * Time.deltaTime
        );
    }

    private void HandleJump()
    {
        if (controller.isGrounded && verticalVelocity.y < 0f)
            verticalVelocity.y = -2f;

        bool jumpPressed = false;

        if (Keyboard.current != null)
            jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;

        if (Gamepad.current != null)
            jumpPressed |= Gamepad.current.buttonSouth.wasPressedThisFrame;

        if (jumpPressed && controller.isGrounded)
        {
            verticalVelocity.y =
                Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity.y += gravity * Time.deltaTime;

        controller.Move(
            verticalVelocity * Time.deltaTime
        );
    }
}
