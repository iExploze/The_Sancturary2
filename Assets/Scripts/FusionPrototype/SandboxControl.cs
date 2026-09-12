using Fusion;
using UnityEngine;

namespace TheSancturary.FusionPrototype
{
    /// <summary>Uses the existing player interaction request, ownership, range and LOS validation.</summary>
    [RequireComponent(typeof(NetworkObject), typeof(InteractionTarget), typeof(BoxCollider))]
    public sealed class SandboxControl : NetworkBehaviour, IInteractable, IAuthoritativeInteractable
    {
        public enum Action : byte { SelectMonster, SpawnMonster, DespawnMonster, ResetEncounter, Restock, HurtSelf, DownSelf }
        [SerializeField] private SandboxSession session;
        [SerializeField] private Action action;
        [SerializeField] private string label;
        [Networked] private TickTimer Cooldown { get; set; }
        public InteractionTarget PromptTarget => GetComponent<InteractionTarget>();
        public override void Spawned() => PromptTarget.Configure(label, "Use", InteractionTargetStatus.Available, null, this);
        public bool TryGetActionText(NetworkPlayerInventory viewerInventory, string interactionVerb, out string actionText)
        {
            actionText = action == Action.SelectMonster || action == Action.SpawnMonster || action == Action.ResetEncounter
                ? $"F — {label} ({session.MonsterName})" : $"F — {label}";
            return Object != null && Object.IsValid;
        }
        public bool RequestInteraction(FusionNetworkPlayer requestingPlayer)
        {
            if (requestingPlayer == null || !requestingPlayer.HasInputAuthority) return false;
            requestingPlayer.RequestInteraction(this); return true;
        }
        public bool TryInteractAuthoritative(FusionNetworkPlayer requestingPlayer)
        {
            if (!HasStateAuthority || session == null || !Cooldown.ExpiredOrNotRunning(Runner)) return false;
            if (!session.Execute(action, requestingPlayer)) return false;
            Cooldown = TickTimer.CreateFromSeconds(Runner, 0.75f); return true;
        }
    }
}
