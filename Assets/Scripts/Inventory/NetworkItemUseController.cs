using Fusion;
using TheSancturary.FusionPrototype;
using TheSancturary.Monsters;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheSancturary.Inventory
{
    public struct ItemAudioEvent : INetworkStruct
    {
        public ushort Sequence;
        public NetworkString<_32> ItemId;
        public byte Stage;
    }

    public enum ItemActionPresentation : byte
    {
        None,
        Use,
        DryFire,
        Fire,
        Reload,
        Complete = 8
    }

    /// <summary>
    /// State-authoritative execution for equipped items. Replicated
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
        [Networked] public NetworkBool IsAiming { get; private set; }
        [Networked] public NetworkBool IsReloading { get; private set; }
        [Networked] private ushort ReloadSourceInstanceId { get; set; }
        [Networked] private NetworkBool StrikeResolved { get; set; }
        [Networked] private TickTimer EquipTimer { get; set; }
        [Networked] private byte AudioStages { get; set; }
        private const int AudioEventCapacity = 8;
        [Networked, Capacity(AudioEventCapacity)] private NetworkArray<ItemAudioEvent> AudioEvents => default;
        [Networked] private ushort AudioSequence { get; set; }
        private ushort _presentedAudioSequence;
        private readonly RaycastHit[] _hitBuffer = new RaycastHit[128];
        private readonly Collider[] _overlapBuffer = new Collider[64];

        public InventoryItemDefinition EquippedDefinition => inventory != null &&
            inventory.TryGetEntry(inventory.EquippedInstanceId, out var entry) &&
            inventory.TryResolveDefinition(entry.ItemId.ToString(), out var definition) ? definition : null;
        public float ActiveDuration => EquippedDefinition == null ? 0f :
            IsReloading ? EquippedDefinition.ReloadDuration : EquippedDefinition.UseDuration;

        public void SetAimRequested(bool requested)
        {
            if (IsProxy) return;
            var definition = EquippedDefinition;
            IsAiming = ItemCombatRules.CanAim(player != null && !player.IsDeadOrPending,
                player == null || player.IsLockerInputLocked || player.IsInVent,
                definition != null && definition.UseKind == InventoryItemUseKind.Firearm,
                HasActiveUse || !EquipTimer.ExpiredOrNotRunning(Runner), requested);
        }

        public void EquipmentChangedAuthoritative()
        {
            if (!HasStateAuthority) return;
            CancelActiveUseAuthoritative();
            IsAiming = false;
            EquipTimer = TickTimer.CreateFromSeconds(Runner,
                EquippedDefinition?.CombatSettings != null ? EquippedDefinition.CombatSettings.equipSeconds : 0.25f);
            if (EquippedDefinition != null) EmitActionAudio(EquippedDefinition.ItemId, 5);
        }

        public void ReloadEquippedAuthoritative()
        {
            if (!HasStateAuthority || EquippedDefinition == null) return;
            foreach (var entry in inventory.Entries)
                if (entry.IsOccupied && ItemGameplayRules.IsCompatibleAmmunition(
                    entry.ItemId.ToString(), EquippedDefinition.CompatibleAmmoItemId))
                {
                    TryBeginReloadAuthoritative(entry.InstanceId, inventory.EquippedInstanceId);
                    return;
                }
        }

        public bool TryBeginReloadAuthoritative(ushort sourceId, ushort targetId)
        {
            ResolveReferences();
            if (!HasStateAuthority || player.IsDeadOrPending || player.IsLockerInputLocked || player.IsInVent ||
                HasActiveUse || !UseCooldownTimer.ExpiredOrNotRunning(Runner) ||
                !EquipTimer.ExpiredOrNotRunning(Runner) || !inventory.IsInstanceEquippedAuthoritative(targetId) ||
                !inventory.TryGetItemAuthoritative(sourceId, out _, out var source) ||
                !inventory.TryGetItemAuthoritative(targetId, out var targetEntry, out var target) ||
                source.Category != InventoryItemCategory.Ammunition || target.UseKind != InventoryItemUseKind.Firearm ||
                targetEntry.LoadedAmmunition >= target.AmmunitionCapacity ||
                !ItemGameplayRules.CanReload(source.ItemId, target.CompatibleAmmoItemId, target.AmmunitionCapacity)) return false;
            ActiveUseInstanceId = targetId;
            ActiveUseKindCode = (byte)target.UseKind;
            ActiveUseColumn = targetEntry.Column;
            ActiveUseRow = targetEntry.Row;
            ActiveUseRotated = targetEntry.Rotated;
            ReloadSourceInstanceId = sourceId;
            IsReloading = true;
            AudioStages = 0;
            IsAiming = false;
            ActiveUseTimer = TickTimer.CreateFromSeconds(Runner, target.ReloadDuration);
            EmitAction(ItemActionPresentation.Reload, target.UseKind, targetId);
            return true;
        }

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
            _presentedAudioSequence = AudioSequence;
        }

        public override void Render()
        {
            // Use the project's replicated-event approach. Joining reconstructs state without replaying old sounds.
            int count = Mathf.Min((ushort)(AudioSequence - _presentedAudioSequence), AudioEventCapacity);
            for (int remaining = count - 1; remaining >= 0; remaining--)
            {
                ushort sequence = unchecked((ushort)(AudioSequence - remaining));
                ItemAudioEvent action = AudioEvents.Get(sequence % AudioEventCapacity);
                if (action.Sequence == sequence) PresentActionAudio(action.ItemId, action.Stage);
            }
            _presentedAudioSequence = AudioSequence;
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
                player.IsLockerInputLocked || player.IsInVent)
            {
                CancelActiveUseAuthoritative();
                return;
            }

            if (!IsActiveUseStillValid())
            {
                CancelActiveUseAuthoritative();
                return;
            }

            if (!IsReloading && (ActiveUseKind == InventoryItemUseKind.Crowbar || ActiveUseKind == InventoryItemUseKind.FireAxe))
                ResolveMeleeWindow();

            AdvanceActionAudio();

            if (ActiveUseTimer.Expired(Runner))
                CompleteActiveUseAuthoritative();
        }

        public bool TryUseEquippedAuthoritative()
        {
            ResolveReferences();
            if (!HasStateAuthority || player == null || inventory == null ||
                player.IsDeadOrPending || player.IsLockerInputLocked || player.IsInVent ||
                HasActiveUse || !UseCooldownTimer.ExpiredOrNotRunning(Runner) ||
                !EquipTimer.ExpiredOrNotRunning(Runner) ||
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
                    if (wasLoaded) ResolveShot(definition);
                    return true;

                case InventoryItemUseKind.None:
                default:
                    return false;
            }
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
            IsReloading = false;
            ReloadSourceInstanceId = 0;
            StrikeResolved = false;
            AudioStages = 0;
        }

        public void CancelAllAuthoritative()
        {
            if (!HasStateAuthority)
                return;

            CancelActiveUseAuthoritative();
            AdrenalineTimer = TickTimer.None;
            IsAiming = false;
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
            IsAiming = false;
            StrikeResolved = false;
            AudioStages = 0;
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
            if (IsReloading)
            {
                bool reloaded = inventory.CommitReloadAuthoritative(ReloadSourceInstanceId, instanceId);
                CancelActiveUseAuthoritative();
                if (reloaded) EmitAction(ItemActionPresentation.Complete, definition.UseKind, instanceId);
                return;
            }
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

            if (consume) EmitAction(ItemActionPresentation.Complete, definition.UseKind, instanceId, definition);
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
            if (IsReloading)
                return inventory.TryGetItemAuthoritative(ReloadSourceInstanceId, out _, out var source) &&
                    ItemGameplayRules.IsCompatibleAmmunition(source.ItemId, EquippedDefinition.CompatibleAmmoItemId);
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
                    return true;

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
            return FirstHit(
                player.ReplicatedViewPosition,
                direction,
                maximumUseDistance,
                out hit);
        }

        private bool FirstHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit hit, float radius = 0f)
        {
            var physics = gameObject.scene.GetPhysicsScene();
            int count = radius > 0 ? physics.SphereCast(origin, radius, direction, _hitBuffer, distance,
                useCollisionMask, QueryTriggerInteraction.Ignore) : physics.Raycast(origin, direction, _hitBuffer,
                distance, useCollisionMask, QueryTriggerInteraction.Ignore);
            hit = default;
            if (count == _hitBuffer.Length) return false;
            float closest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var candidate = _hitBuffer[i];
                if (candidate.collider == null || candidate.collider.transform.IsChildOf(player.transform) ||
                    candidate.distance >= closest) continue;
                hit = candidate;
                closest = candidate.distance;
            }
            return closest < float.PositiveInfinity;
        }

        private void ResolveShot(InventoryItemDefinition definition)
        {
            if (definition.CombatSettings == null) return;
            Quaternion rotation = Quaternion.Euler(player.LookPitch, player.LookYaw, 0);
            Vector3 origin = player.ReplicatedViewPosition;
            Vector3 muzzle = origin + rotation * (IsAiming ? definition.AdsMuzzleOffset : definition.MuzzleOffset);
            Vector3 offset = muzzle - origin;
            // A camera target never allows the barrel to tunnel through a close wall.
            if (FirstHit(origin, offset.normalized, offset.magnitude, out _)) return;
            var physics = gameObject.scene.GetPhysicsScene();
            int overlapCount = physics.OverlapSphere(muzzle, 0.025f, _overlapBuffer, useCollisionMask, QueryTriggerInteraction.Ignore);
            if (overlapCount == _overlapBuffer.Length) return;
            for (int i = 0; i < overlapCount; i++)
                if (!_overlapBuffer[i].transform.IsChildOf(player.transform)) return;
            // One uncertain shot direction, including shotgun. No fan of instant-kill pellets.
            var random = new System.Random(unchecked(Runner.Tick.Raw * 397 ^ ActionSequence ^ (int)Object.Id.Raw));
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            float radius = Mathf.Sqrt((float)random.NextDouble()) * Mathf.Tan(
                (IsAiming ? definition.AdsSpreadDegrees : definition.HipSpreadDegrees) * Mathf.Deg2Rad);
            Vector3 direction = rotation * new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 1).normalized;
            Vector3 target = FirstHit(origin, direction, definition.Range, out var cameraHit)
                ? cameraHit.point : origin + direction * definition.Range;
            Vector3 shot = target - muzzle;
            if (FirstHit(muzzle, shot.normalized, shot.magnitude + 0.015f, out var hit))
                hit.collider.GetComponentInParent<MonsterCombatState>()?.TryReceiveHitAuthoritative(this, definition, hit.point, hit.normal);
        }

        private void ResolveMeleeWindow()
        {
            var definition = EquippedDefinition;
            var settings = definition?.CombatSettings;
            if (definition == null) return;
            float progress = 1f - ActiveUseRemainingSeconds / Mathf.Max(0.01f, definition.UseDuration);
            float start = settings != null ? settings.strikeStart : 0.42f;
            float end = settings != null ? settings.strikeEnd : 0.62f;
            if (!ItemCombatRules.CanStrike(progress, start, end, StrikeResolved)) return;
            Vector3 origin = player.ReplicatedViewPosition;
            Vector3 forward = Quaternion.Euler(player.LookPitch, player.LookYaw, 0) * Vector3.forward;
            if (!FirstHit(origin, forward, settings != null ? settings.meleeReach : maximumUseDistance,
                out var hit, settings != null ? settings.meleeRadius : 0.16f)) return;
            // Confirm unobstructed contact independently of the forgiving strike volume.
            Vector3 delta = hit.point - origin;
            if (FirstHit(origin, delta.normalized, delta.magnitude + 0.01f, out var blocker) &&
                blocker.collider != hit.collider)
            {
                var targetMonster = hit.collider.GetComponentInParent<MonsterCombatState>();
                if (targetMonster == null || blocker.collider.GetComponentInParent<MonsterCombatState>() != targetMonster) return;
            }
            var monster = hit.collider.GetComponentInParent<MonsterCombatState>();
            var obstacle = hit.collider.GetComponentInParent<NetworkToolObstacle>();
            if (monster != null)
                StrikeResolved = monster.TryReceiveHitAuthoritative(this, definition, hit.point, hit.normal);
            else if (obstacle != null && obstacle.Runner == Runner)
                StrikeResolved = obstacle.TryApplyHit(definition.ItemId);
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
            ushort instanceId,
            InventoryItemDefinition knownDefinition = null)
        {
            ActionPresentationCode = (byte)presentation;
            ActionUseKindCode = (byte)useKind;
            ActionInstanceId = instanceId;
            ActionSequence = unchecked((ushort)(ActionSequence + 1));
            var definition = knownDefinition;
            if (definition == null) inventory.TryGetItemAuthoritative(instanceId, out _, out definition);
            if (definition != null && !(presentation == ItemActionPresentation.Use &&
                (useKind == InventoryItemUseKind.FireAxe || useKind == InventoryItemUseKind.Crowbar)))
                EmitActionAudio(definition.ItemId, (byte)presentation);
        }

        private void AdvanceActionAudio()
        {
            var definition = EquippedDefinition;
            if (definition == null) return;
            float progress = 1 - ActiveUseRemainingSeconds / Mathf.Max(0.01f, ActiveDuration);
            bool melee = definition.UseKind == InventoryItemUseKind.Crowbar || definition.UseKind == InventoryItemUseKind.FireAxe;
            if (IsReloading && definition.ItemId == "old_revolver")
            {
                // Six individually handled rounds; the final bit records the closing stage.
                for (int round = 0; round < definition.AmmunitionCapacity && round < 6; round++)
                {
                    byte stage = (byte)(1 << round);
                    if ((AudioStages & stage) != 0 || progress < 0.225f + round * 0.099f) continue;
                    AudioStages |= stage;
                    EmitActionAudio(definition.ItemId, 6);
                }
                if ((AudioStages & 64) == 0 && progress >= 0.86f)
                {
                    AudioStages |= 64;
                    EmitActionAudio(definition.ItemId, 7);
                }
                return;
            }
            if ((AudioStages & 1) == 0 && progress >= (melee ? 0.3f : 0.5f))
            {
                AudioStages |= 1;
                EmitActionAudio(definition.ItemId, melee ? (byte)ItemActionPresentation.Use : (byte)6);
            }
            if (IsReloading && (AudioStages & 2) == 0 && progress >= 0.86f)
            {
                AudioStages |= 2;
                EmitActionAudio(definition.ItemId, 7);
            }
        }

        private void EmitActionAudio(NetworkString<_32> itemId, byte stage)
        {
            if (!HasStateAuthority) return;
            ushort next = unchecked((ushort)(AudioSequence + 1));
            AudioEvents.Set(next % AudioEventCapacity, new ItemAudioEvent { Sequence = next, ItemId = itemId, Stage = stage });
            AudioSequence = next;
        }

        private void PresentActionAudio(NetworkString<_32> itemId, byte stage)
        {
            if (inventory == null || !inventory.TryResolveDefinition(itemId.ToString(), out var definition)) return;
            AudioClip clip = stage switch
            {
                1 => definition.UseAudioClip,
                2 => definition.DryFireAudioClip,
                3 => definition.UseAudioClip,
                4 => definition.ReloadAudioClip,
                5 => definition.EquipAudioClip,
                6 => definition.ReloadInsertAudioClip,
                7 => definition.ReloadCloseAudioClip,
                8 => definition.CompletionAudioClip,
                9 => definition.PickupAudioClip != null ? definition.PickupAudioClip : definition.EquipAudioClip,
                _ => null
            };
            GetComponent<PlayerEquipment>()?.PlayActionAudio(clip);
        }

        public void PresentPickupAuthoritative(string itemId)
        {
            if (HasStateAuthority) EmitActionAudio(itemId, 9);
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
