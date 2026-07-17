using UnityEngine;

namespace TheSancturary.Player
{
    [CreateAssetMenu(fileName = "PlayerMovementDefinition", menuName = "The Sancturary/Player Movement Definition")]
    public sealed class PlayerMovementDefinition : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Min(0f), Tooltip("Maximum grounded movement speed while walking.")]
        private float walkSpeed = 4.5f;

        [SerializeField, Min(0f), Tooltip("Maximum grounded movement speed while actively sprinting.")]
        private float sprintSpeed = 7f;

        [SerializeField, Min(0f), Tooltip("Maximum grounded movement speed while crouching.")]
        private float crouchSpeed = 2.25f;

        [SerializeField, Min(0f), Tooltip("Vertical height reached by a jump.")]
        private float jumpHeight = 1.2f;

        [SerializeField, Tooltip("Downward acceleration applied by the authoritative server.")]
        private float gravity = -22f;

        [Header("Stamina")]
        [SerializeField, Min(0.01f), Tooltip("Total stamina available to the player.")]
        private float maximumStamina = 100f;

        [SerializeField, Min(0f), Tooltip("Stamina consumed per second of active sprinting.")]
        private float sprintDrainPerSecond = 25f;

        [SerializeField, Min(0f), Tooltip("Stamina restored per second after the recovery delay.")]
        private float staminaRecoveryPerSecond = 20f;

        [SerializeField, Min(0f), Tooltip("Time after sprinting stops before stamina begins recovering.")]
        private float staminaRecoveryDelay = 1f;

        [SerializeField, Min(0f), Tooltip("Stamina required before an exhausted player may sprint again.")]
        private float exhaustedRecoveryThreshold = 20f;

        [Header("Crouching")]
        [SerializeField, Min(0.1f), Tooltip("Authoritative CharacterController height while standing.")]
        private float standingControllerHeight = 1.8f;

        [SerializeField, Min(0.1f), Tooltip("Authoritative CharacterController height while crouching.")]
        private float crouchingControllerHeight = 1.1f;

        [SerializeField, Min(0f), Tooltip("Owning player's local camera height while standing.")]
        private float standingCameraHeight = 1.6f;

        [SerializeField, Min(0f), Tooltip("Owning player's local camera height while crouching.")]
        private float crouchingCameraHeight = 1f;

        [SerializeField, Min(0.01f), Tooltip("Response speed for local camera and remote body crouch transitions.")]
        private float crouchTransitionSpeed = 10f;

        [Header("Tiredness Vignette")]
        [SerializeField, Range(0.01f, 1f), Tooltip("Normalized stamina below which the vignette begins fading in.")]
        private float vignetteFadeInThreshold = 0.5f;

        [SerializeField, Range(0f, 1f), Tooltip("Strongest tiredness vignette intensity at zero stamina.")]
        private float maximumVignetteIntensity = 0.42f;

        [SerializeField, Min(0.01f), Tooltip("How quickly the vignette responds to stamina changes.")]
        private float vignetteResponseSpeed = 5f;

        [SerializeField, Range(0f, 0.25f), Tooltip("Subtle intensity variation while fully exhausted. Set to zero to disable.")]
        private float exhaustedPulseStrength = 0.04f;

        [SerializeField, Min(0f), Tooltip("Cycles per second for the optional exhausted pulse.")]
        private float exhaustedPulseSpeed = 2.5f;

        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;
        public float CrouchSpeed => crouchSpeed;
        public float JumpHeight => jumpHeight;
        public float Gravity => gravity;
        public float MaximumStamina => maximumStamina;
        public float SprintDrainPerSecond => sprintDrainPerSecond;
        public float StaminaRecoveryPerSecond => staminaRecoveryPerSecond;
        public float StaminaRecoveryDelay => staminaRecoveryDelay;
        public float ExhaustedRecoveryThreshold => exhaustedRecoveryThreshold;
        public float StandingControllerHeight => standingControllerHeight;
        public float CrouchingControllerHeight => crouchingControllerHeight;
        public float StandingCameraHeight => standingCameraHeight;
        public float CrouchingCameraHeight => crouchingCameraHeight;
        public float CrouchTransitionSpeed => crouchTransitionSpeed;
        public float VignetteFadeInThreshold => vignetteFadeInThreshold;
        public float MaximumVignetteIntensity => maximumVignetteIntensity;
        public float VignetteResponseSpeed => vignetteResponseSpeed;
        public float ExhaustedPulseStrength => exhaustedPulseStrength;
        public float ExhaustedPulseSpeed => exhaustedPulseSpeed;

        private void OnValidate()
        {
            walkSpeed = Mathf.Max(0f, walkSpeed);
            sprintSpeed = Mathf.Max(walkSpeed, sprintSpeed);
            crouchSpeed = Mathf.Clamp(crouchSpeed, 0f, walkSpeed);
            jumpHeight = Mathf.Max(0f, jumpHeight);
            gravity = Mathf.Min(-0.01f, gravity);

            maximumStamina = Mathf.Max(0.01f, maximumStamina);
            sprintDrainPerSecond = Mathf.Max(0f, sprintDrainPerSecond);
            staminaRecoveryPerSecond = Mathf.Max(0f, staminaRecoveryPerSecond);
            staminaRecoveryDelay = Mathf.Max(0f, staminaRecoveryDelay);
            exhaustedRecoveryThreshold = Mathf.Clamp(exhaustedRecoveryThreshold, 0f, maximumStamina);

            standingControllerHeight = Mathf.Max(0.1f, standingControllerHeight);
            crouchingControllerHeight = Mathf.Clamp(crouchingControllerHeight, 0.1f, standingControllerHeight);
            standingCameraHeight = Mathf.Max(0f, standingCameraHeight);
            crouchingCameraHeight = Mathf.Clamp(crouchingCameraHeight, 0f, standingCameraHeight);
            crouchTransitionSpeed = Mathf.Max(0.01f, crouchTransitionSpeed);

            vignetteFadeInThreshold = Mathf.Clamp(vignetteFadeInThreshold, 0.01f, 1f);
            maximumVignetteIntensity = Mathf.Clamp01(maximumVignetteIntensity);
            vignetteResponseSpeed = Mathf.Max(0.01f, vignetteResponseSpeed);
            exhaustedPulseStrength = Mathf.Clamp(exhaustedPulseStrength, 0f, maximumVignetteIntensity);
            exhaustedPulseSpeed = Mathf.Max(0f, exhaustedPulseSpeed);
        }
    }
}
