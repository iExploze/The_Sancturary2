using Fusion;
using TheSancturary.FusionPrototype;
using UnityEngine;

namespace TheSancturary.Inventory
{
    public enum ItemActionPresentation : byte
    {
        None,
        Use,
        DryFire,
        Fire,
        Reload
    }

    /// <summary>
    /// State-authoritative execution for equipped provisional items. Replicated
    /// action state is intentionally presentation-only; all consequential state
    /// changes remain on state authority.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(FusionNetworkPlayer))]
    [RequireComponent(typeof(NetworkPlayerInventory))]
    public sealed class NetworkItemUseController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private FusionNetworkPlayer player;
        [SerializeField] private NetworkPlayerInventory inventory;

        [Header("Authoritative Targeting")]
        [SerializeField, Min(0.1f)] private float maximumUseDistance =
            ItemGameplayRules.RevivalMaximumDistance;
        [SerializeField] private LayerMask useCollisionMask = ~0;

        [Networked] public ushort ActionSequence { get; private set; }
        [Networked] public byte ActionPresentationCode { get; private set; }
        [Networked] public byte ActionUseKindCode { get; private set; }
        [Networked] public ushort ActionInstanceId { get; private set; }
        [Networked] public ushort ActiveUseInstanceId { get; private set; }
        [Networked] public byte ActiveUseKindCode { get; private set; }
        [Networked] private byte ActiveUseColumn { get; set; }
        [Networked] private byte ActiveUseRow { get; set; }
        [Networked] private NetworkBool ActiveUseRotated { get; set; }
        [Networked] public NetworkBehaviourId ActiveTarget { get; private set; }
        [Networked] public TickTimer ActiveUseTimer { get; private set; }
        [Networked] private TickTimer UseCooldownTimer { get; set; }
        [Networked] private TickTimer AdrenalineTimer { get; set; }

        public ItemActionPresentation LastAction =>
            (ItemActionPresentation)ActionPresentationCode;
        public InventoryItemUseKind LastActionUseKind =>
            (InventoryItemUseKind)ActionUseKindCode;
        public InventoryItemUseKind ActiveUseKind =>
            (InventoryItemUseKind)ActiveUseKindCode;
        public bool HasActiveUse => ActiveUseInstanceId != 0;
        public float ActiveUseRemainingSeconds =>
            Runner != null && HasActiveUse
                ? ActiveUseTimer.RemainingTime(Runner) ?? 0f
                : 0f;
        public bool IsAdrenalineActive =>
            Runner != null && !AdrenalineTimer.ExpiredOrNotRunning(Runner);
        public float AdrenalineRemainingSeconds =>
            Runner != null ? AdrenalineTimer.RemainingTime(Runner) ?? 0f : 0f;

        public override void Spawned()
        {
            ResolveReferences();
        }

        /// <summary>
        /// Advances authoritative timed-use state after the owning player has
        /// processed inventory commands and pending damage for this tick.
        /// FusionNetworkPlayer is the sole caller so a cancel cannot race a
        /// same-tick completion on a separate NetworkBehaviour update.
        /// </summary>
        public void TickAuthoritative()
        {
            if (!HasStateAuthority)
                return;

            ResolveReferences();
            if (player == null || inventory == null || player.IsDeadOrPending)
            {
                CancelAllAuthoritative();
                return;
            }

            if (!HasActiveUse)
                return;

            if (!inventory.IsInstanceEquippedAuthoritative(
                    ActiveUseInstanceId) ||
                !inventory.TryGetItemAuthoritative(
                    ActiveUseInstanceId,
                    out NetworkInventoryEntry activeEntry,
                    out _) ||
                activeEntry.Column != ActiveUseColumn ||
                activeEntry.Row != ActiveUseRow ||
                (bool)activeEntry.Rotated !=
                (bool)ActiveUseRotated ||
                player.IsLockerInputLocked)
            {
                CancelActiveUseAuthoritative();
                return;
            }

            if (!IsActiveUseStillValid())
            {
                CancelActiveUseAuthoritative();
                return;
            }

            if (ActiveUseTimer.Expired(Runner))
                CompleteActiveUseAuthoritative();
        }

        public bool TryUseEquippedAuthoritative()
        {
            ResolveReferences();
            if (!HasStateAuthority || player == null || inventory == null ||
                player.IsDeadOrPending || player.IsLockerInputLocked ||
                HasActiveUse || !UseCooldownTimer.ExpiredOrNotRunning(Runner) ||
                !inventory.TryGetEquippedItemAuthoritative(
                    out NetworkInventoryEntry entry,
                    out InventoryItemDefinition definition))
                return false;

            ushort instanceId = entry.InstanceId;
            switch (definition.UseKind)
            {
                case InventoryItemUseKind.FlashlightToggle:
                    inventory.ToggleEquippedUseAuthoritative();
                    BeginCooldown(definition);
                    EmitAction(
                        ItemActionPresentation.Use,
                        definition.UseKind,
                        instanceId);
                    return true;

                case InventoryItemUseKind.FullHeal:
                    if (!ItemGameplayRules.CanUseMedKit(
                            !player.IsDead,
                            player.Health,
                            player.MaximumHealth))
                        return false;

                    return BeginTimedUse(entry, definition, default);

                case InventoryItemUseKind.Adrenaline:
                    if (!ItemGameplayRules.CanApplyAdrenaline(
                            !player.IsDead,
                            IsAdrenalineActive))
                        return false;

                    return BeginTimedUse(entry, definition, default);

                case InventoryItemUseKind.RevivalSyringe:
                    if (!TryAcquireRevivalTarget(out FusionNetworkPlayer target))
                        return false;

                    return BeginTimedUse(entry, definition, target.Id);

                case InventoryItemUseKind.Crowbar:
                case InventoryItemUseKind.FireAxe:
                    NetworkBehaviourId obstacleId = default;
                    if (TryAcquireToolTarget(
                            definition.ItemId,
                            out NetworkToolObstacle obstacle))
                        obstacleId = obstacle.Id;

                    return BeginTimedUse(entry, definition, obstacleId);

                case InventoryItemUseKind.Firearm:
                    if (!inventory.TryDecrementLoadedAmmunitionAuthoritative(
                            instanceId,
                            out bool wasLoaded,
                            out _))
                        return false;

                    BeginCooldown(definition);
                    EmitAction(
                        wasLoaded
                            ? ItemActionPresentation.Fire
                            : ItemActionPresentation.DryFire,
                        definition.UseKind,
                        instanceId);
                    return true;

                case InventoryItemUseKind.None:
                default:
                    return false;
            }
        }

        public void PresentReloadAuthoritative(ushort firearmInstanceId)
        {
            if (!HasStateAuthority ||
                !inventory.TryGetItemAuthoritative(
                    firearmInstanceId,
                    out _,
                    out InventoryItemDefinition definition) ||
                definition.UseKind != InventoryItemUseKind.Firearm)
                return;

            EmitAction(
                ItemActionPresentation.Reload,
                definition.UseKind,
                firearmInstanceId);
        }

        public void CancelActiveUseAuthoritative()
        {
            if (!HasStateAuthority)
                return;

            ActiveUseInstanceId = 0;
            ActiveUseKindCode = (byte)InventoryItemUseKind.None;
            ActiveUseColumn = 0;
            ActiveUseRow = 0;
            ActiveUseRotated = false;
            ActiveTarget = default;
            ActiveUseTimer = TickTimer.None;
        }

        public void CancelAllAuthoritative()
        {
            if (!HasStateAuthority)
                return;

            CancelActiveUseAuthoritative();
            AdrenalineTimer = TickTimer.None;
        }

        private bool BeginTimedUse(
            NetworkInventoryEntry entry,
            InventoryItemDefinition definition,
            NetworkBehaviourId target)
        {
            ActiveUseInstanceId = entry.InstanceId;
            ActiveUseKindCode = (byte)definition.UseKind;
            ActiveUseColumn = entry.Column;
            ActiveUseRow = entry.Row;
            ActiveUseRotated = entry.Rotated;
            ActiveTarget = target;
            ActiveUseTimer = definition.UseDuration > 0f
                ? TickTimer.CreateFromSeconds(Runner, definition.UseDuration)
                : TickTimer.None;
            BeginCooldown(definition);
            EmitAction(
                ItemActionPresentation.Use,
                definition.UseKind,
                entry.InstanceId);

            if (definition.UseDuration <= 0f)
                CompleteActiveUseAuthoritative();
            return true;
        }

        private void CompleteActiveUseAuthoritative()
        {
            ushort instanceId = ActiveUseInstanceId;
            InventoryItemUseKind useKind = ActiveUseKind;
            NetworkBehaviourId targetId = ActiveTarget;

            if (!inventory.IsInstanceEquippedAuthoritative(instanceId) ||
                !inventory.TryGetItemAuthoritative(
                    instanceId,
                    out _,
                    out InventoryItemDefinition definition) ||
                definition.UseKind != useKind)
            {
                CancelActiveUseAuthoritative();
                return;
            }

            bool consume = false;
            switch (useKind)
            {
                case InventoryItemUseKind.FullHeal:
                    if (ItemGameplayRules.CanUseMedKit(
                            !player.IsDead,
                            player.Health,
                            player.MaximumHealth))
                    {
                        consume = player.TryHealFullyAuthoritative();
                    }
                    break;

                case InventoryItemUseKind.Adrenaline:
                    if (ItemGameplayRules.TryStartAdrenaline(
                            !player.IsDead,
                            IsAdrenalineActive,
                            out ItemGameplayRules.AdrenalineUseTransition
                                transition))
                    {
                        consume = transition.ConsumedInstanceCount == 1;
                    }
                    break;

                case InventoryItemUseKind.RevivalSyringe:
                    if (TryValidateRevivalTarget(
                            targetId,
                            out FusionNetworkPlayer revivalTarget))
                    {
                        consume = revivalTarget.TryReviveAtHalfHealthAuthoritative();
                    }
                    break;

                case InventoryItemUseKind.Crowbar:
                case InventoryItemUseKind.FireAxe:
                    if (TryValidateToolTarget(
                            targetId,
                            definition.ItemId,
                            out NetworkToolObstacle obstacle))
                        obstacle.TryApplyHit(definition.ItemId);
                    break;
            }

            if (consume)
            {
                if (inventory.TryConsumeInstanceAuthoritative(instanceId) &&
                    useKind == InventoryItemUseKind.Adrenaline)
                {
                    AdrenalineTimer = TickTimer.CreateFromSeconds(
                        Runner,
                        ItemGameplayRules.AdrenalineDurationSeconds);
                }
            }

            CancelActiveUseAuthoritative();
        }

        private bool TryAcquireRevivalTarget(
            out FusionNetworkPlayer revivalTarget)
        {
            revivalTarget = null;
            if (!TryGetFirstUseHit(out RaycastHit hit))
                return false;

            FusionNetworkPlayer candidate =
                hit.collider.GetComponentInParent<FusionNetworkPlayer>();
            if (!IsValidRevivalTarget(candidate, hit.distance))
                return false;

            revivalTarget = candidate;
            return true;
        }

        private bool IsActiveUseStillValid()
        {
            switch (ActiveUseKind)
            {
                case InventoryItemUseKind.FullHeal:
                    return ItemGameplayRules.CanUseMedKit(
                        !player.IsDead,
                        player.Health,
                        player.MaximumHealth);

                case InventoryItemUseKind.Adrenaline:
                    return ItemGameplayRules.CanApplyAdrenaline(
                        !player.IsDead,
                        IsAdrenalineActive);

                case InventoryItemUseKind.RevivalSyringe:
                    return TryValidateRevivalTarget(ActiveTarget, out _);

                case InventoryItemUseKind.Crowbar:
                case InventoryItemUseKind.FireAxe:
                    if (!ActiveTarget.IsValid)
                        return true;
                    if (!inventory.TryGetItemAuthoritative(
                            ActiveUseInstanceId,
                            out _,
                            out InventoryItemDefinition definition))
                        return false;
                    return TryValidateToolTarget(
                        ActiveTarget,
                        definition.ItemId,
                        out NetworkToolObstacle obstacle) &&
                        !obstacle.IsCompleted;

                default:
                    return true;
            }
        }

        private bool TryValidateRevivalTarget(
            NetworkBehaviourId targetId,
            out FusionNetworkPlayer revivalTarget)
        {
            revivalTarget = null;
            if (!targetId.IsValid ||
                !Runner.TryFindBehaviour(
                    targetId,
                    out NetworkBehaviour behaviour) ||
                behaviour is not FusionNetworkPlayer candidate ||
                !TryGetFirstUseHit(out RaycastHit hit) ||
                hit.collider.GetComponentInParent<FusionNetworkPlayer>() !=
                candidate ||
                !IsValidRevivalTarget(candidate, hit.distance))
                return false;

            revivalTarget = candidate;
            return true;
        }

        private bool IsValidRevivalTarget(
            FusionNetworkPlayer candidate,
            float hitDistance)
        {
            bool isRegisteredPlayerObject =
                candidate != null &&
                candidate.Object != null &&
                candidate.Object.IsValid &&
                candidate.Object.InputAuthority != PlayerRef.None &&
                Runner != null &&
                Runner.TryGetPlayerObject(
                    candidate.Object.InputAuthority,
                    out NetworkObject registeredObject) &&
                registeredObject == candidate.Object;

            return ItemGameplayRules.CanRevive(
                !player.IsDead,
                isRegisteredPlayerObject,
                candidate != null && candidate.Runner == Runner,
                candidate != null && candidate.IsDead,
                candidate == player,
                hitDistance,
                true,
                maximumUseDistance);
        }

        private bool TryAcquireToolTarget(
            string itemId,
            out NetworkToolObstacle obstacle)
        {
            obstacle = null;
            if (!TryGetFirstUseHit(out RaycastHit hit))
                return false;

            NetworkToolObstacle candidate =
                hit.collider.GetComponentInParent<NetworkToolObstacle>();
            if (candidate == null || candidate.Runner != Runner ||
                candidate.RequiredItemId !=
                ItemGameplayRules.NormalizeItemId(itemId))
                return false;

            obstacle = candidate;
            return true;
        }

        private bool TryValidateToolTarget(
            NetworkBehaviourId targetId,
            string itemId,
            out NetworkToolObstacle obstacle)
        {
            obstacle = null;
            if (!targetId.IsValid ||
                !Runner.TryFindBehaviour(
                    targetId,
                    out NetworkBehaviour behaviour) ||
                behaviour is not NetworkToolObstacle candidate ||
                !TryGetFirstUseHit(out RaycastHit hit) ||
                hit.collider.GetComponentInParent<NetworkToolObstacle>() !=
                candidate ||
                candidate.RequiredItemId !=
                ItemGameplayRules.NormalizeItemId(itemId))
                return false;

            obstacle = candidate;
            return true;
        }

        private bool TryGetFirstUseHit(out RaycastHit hit)
        {
            Vector3 direction = Quaternion.Euler(
                player.LookPitch,
                player.LookYaw,
                0f) * Vector3.forward;
            return Physics.Raycast(
                player.ReplicatedViewPosition,
                direction,
                out hit,
                maximumUseDistance,
                useCollisionMask,
                QueryTriggerInteraction.Ignore);
        }

        private void BeginCooldown(InventoryItemDefinition definition)
        {
            UseCooldownTimer = definition.UseCooldown > 0f
                ? TickTimer.CreateFromSeconds(Runner, definition.UseCooldown)
                : TickTimer.None;
        }

        private void EmitAction(
            ItemActionPresentation presentation,
            InventoryItemUseKind useKind,
            ushort instanceId)
        {
            ActionPresentationCode = (byte)presentation;
            ActionUseKindCode = (byte)useKind;
            ActionInstanceId = instanceId;
            ActionSequence = unchecked((ushort)(ActionSequence + 1));
        }

        private void ResolveReferences()
        {
            player ??= GetComponent<FusionNetworkPlayer>();
            inventory ??= GetComponent<NetworkPlayerInventory>();
            maximumUseDistance = Mathf.Max(0.1f, maximumUseDistance);
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
