using Fusion;
using NUnit.Framework;
using TheSancturary.FusionPrototype;
using Assert = NUnit.Framework.Assert;

namespace TheSancturary.Tests.EditMode
{
    public sealed class LobbyRulesTests
    {
        [Test]
        public void SoloHostCanStartOnlyAfterReady()
        {
            PlayerRef host = PlayerRef.FromEncoded(1);
            Assert.That(LobbyRules.AreAllReady(new[] { host }, new PlayerRef[0]), Is.False);
            Assert.That(LobbyRules.AreAllReady(new[] { host }, new[] { host }), Is.True);
        }

        [Test]
        public void AnyUnreadyPlayerBlocksStart()
        {
            PlayerRef first = PlayerRef.FromEncoded(1);
            PlayerRef second = PlayerRef.FromEncoded(2);
            Assert.That(LobbyRules.AreAllReady(new[] { first, second }, new[] { first }), Is.False);
        }

        [Test]
        public void PlayerSlotReusesLowestEmptySlot()
        {
            Assert.That(LobbyRules.LowestAvailableSlot(new[] { 1, 3, 4 }, 4), Is.EqualTo(2));
        }

        [Test]
        public void SessionNameIsTrimmedDefaultedAndLimited()
        {
            Assert.That(FusionSessionManager.NormalizeSessionName("   "), Is.EqualTo("sancturary-prototype"));
            Assert.That(FusionSessionManager.NormalizeSessionName("  room  "), Is.EqualTo("room"));
            Assert.That(FusionSessionManager.NormalizeSessionName(new string('a', 40)).Length, Is.EqualTo(32));
        }

        [Test]
        public void OnlyHostsSeeHostOnlyControls()
        {
            Assert.That(LobbyRules.IsHostControlVisible(true), Is.True);
            Assert.That(LobbyRules.IsHostControlVisible(false), Is.False);
        }
    }
}
