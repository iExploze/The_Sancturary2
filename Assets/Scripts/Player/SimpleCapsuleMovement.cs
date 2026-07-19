using UnityEngine;
using UnityEngine.InputSystem;

namespace TheSancturary.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class SimpleCapsuleMovement : MonoBehaviour
    {
        private const float GroundedDownwardVelocity = -2f;

        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -22f;
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
        [SerializeField] private Camera playerCamera;

        private CharacterController characterController;
        private float cameraPitch;
        private float verticalVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = gameObject.AddComponent<CharacterController>();
            }

            var capsuleCollider = GetComponent<CapsuleCollider>();
            if (capsuleCollider != null && capsuleCollider.enabled)
            {
                characterController.center = capsuleCollider.center;
                characterController.radius = capsuleCollider.radius;
                characterController.height = capsuleCollider.height;
                capsuleCollider.enabled = false;
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (playerCamera != null && !playerCamera.transform.IsChildOf(transform))
            {
                playerCamera.transform.SetParent(transform);
                playerCamera.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            }

            if (playerCamera != null)
            {
                cameraPitch = playerCamera.transform.localEulerAngles.x;
                if (cameraPitch > 180f)
                {
                    cameraPitch -= 360f;
                }
            }

            SetCursorLocked(true);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                SetCursorLocked(Cursor.lockState != CursorLockMode.Locked);
            }

            UpdateCameraLook();

            var horizontal = 0f;
            var vertical = 0f;
            if (keyboard.wKey.isPressed) vertical += 1f;
            if (keyboard.sKey.isPressed) vertical -= 1f;
            if (keyboard.dKey.isPressed) horizontal += 1f;
            if (keyboard.aKey.isPressed) horizontal -= 1f;

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = GroundedDownwardVelocity;
            }

            if (keyboard.spaceKey.wasPressedThisFrame && characterController.isGrounded)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;

            var direction = (transform.forward * vertical + transform.right * horizontal).normalized;
            var velocity = direction * moveSpeed + Vector3.up * verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
        }

        private void UpdateCameraLook()
        {
            var mouse = Mouse.current;
            if (playerCamera == null || mouse == null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            var mouseDelta = mouse.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(Vector3.up, mouseDelta.x);

            cameraPitch = Mathf.Clamp(cameraPitch - mouseDelta.y, -85f, 85f);
            playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        private void OnDisable()
        {
            SetCursorLocked(false);
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            jumpHeight = Mathf.Max(0f, jumpHeight);
            gravity = Mathf.Min(-0.01f, gravity);
            mouseSensitivity = Mathf.Max(0f, mouseSensitivity);
        }
    }
}
