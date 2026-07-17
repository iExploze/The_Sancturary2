using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheSancturary.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkPlayerController : NetworkBehaviour
    {
        private const float MoveSpeed = 4.5f;
        private const float JumpHeight = 1.2f;
        private const float Gravity = -22f;
        private const float MouseSensitivity = 0.12f;

        private static readonly Dictionary<ulong, NetworkPlayerController> activePlayers = new();

        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private AudioListener playerAudioListener;
        [SerializeField] private Renderer bodyRenderer;

        private Vector2 serverMoveInput;
        private float serverYaw;
        private bool serverJumpRequested;
        private float verticalVelocity;
        private float cameraPitch;
        private bool cursorLocked;

        public static IReadOnlyDictionary<ulong, NetworkPlayerController> ActivePlayers => activePlayers;

        public override void OnNetworkSpawn()
        {
            activePlayers[OwnerClientId] = this;
            characterController.enabled = IsServer;
            playerCamera.enabled = IsOwner;
            playerAudioListener.enabled = IsOwner;
            bodyRenderer.enabled = !IsOwner;

            if (IsOwner)
            {
                SetCursorLocked(true);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (activePlayers.TryGetValue(OwnerClientId, out var player) && player == this)
            {
                activePlayers.Remove(OwnerClientId);
            }

            if (IsOwner)
            {
                SetCursorLocked(false);
            }
        }

        private void Update()
        {
            if (!IsSpawned)
            {
                return;
            }

            if (IsOwner)
            {
                ReadOwnerInput();
            }

            if (IsServer)
            {
                ApplyServerMovement();
            }
        }

        private void ReadOwnerInput()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                SetCursorLocked(!cursorLocked);
            }

            var moveInput = Vector2.zero;
            if (keyboard.wKey.isPressed) moveInput.y += 1f;
            if (keyboard.sKey.isPressed) moveInput.y -= 1f;
            if (keyboard.dKey.isPressed) moveInput.x += 1f;
            if (keyboard.aKey.isPressed) moveInput.x -= 1f;
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);

            var yaw = transform.eulerAngles.y;
            if (cursorLocked && mouse != null)
            {
                var mouseDelta = mouse.delta.ReadValue() * MouseSensitivity;
                yaw += mouseDelta.x;
                cameraPitch = Mathf.Clamp(cameraPitch - mouseDelta.y, -85f, 85f);
                cameraPivot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            }

            SubmitInputRpc(moveInput, yaw, keyboard.spaceKey.wasPressedThisFrame);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SubmitInputRpc(Vector2 moveInput, float yaw, bool jumpRequested)
        {
            serverMoveInput = Vector2.ClampMagnitude(moveInput, 1f);
            serverYaw = yaw;
            serverJumpRequested |= jumpRequested;
        }

        private void ApplyServerMovement()
        {
            transform.rotation = Quaternion.Euler(0f, serverYaw, 0f);

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (serverJumpRequested && characterController.isGrounded)
            {
                verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            }

            serverJumpRequested = false;
            verticalVelocity += Gravity * Time.deltaTime;

            var planarMove = transform.right * serverMoveInput.x + transform.forward * serverMoveInput.y;
            var velocity = planarMove * MoveSpeed + Vector3.up * verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
        }

        private void SetCursorLocked(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
