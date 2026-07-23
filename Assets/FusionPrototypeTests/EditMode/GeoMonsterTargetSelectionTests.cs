using NUnit.Framework;
using TheSancturary.Monsters;

namespace TheSancturary.FusionPrototype.Tests
{
    public sealed class GeoMonsterTargetSelectionTests
    {
        [Test]
        public void NearestVisiblePlayerReplacesCurrentBest()
        {
            Assert.That(GeoMonsterTargetSelection.ShouldSelectCandidate(true, true, true, 5f, 8f), Is.True);
        }

        [Test]
        public void CloserObstructedPlayerIsIgnored()
        {
            Assert.That(GeoMonsterTargetSelection.ShouldSelectCandidate(true, true, false, 4f, 8f), Is.False);
        }

        [Test]
        public void FartherVisiblePlayerDoesNotReplaceCurrentTarget()
        {
            Assert.That(GeoMonsterTargetSelection.ShouldSelectCandidate(true, true, true, 10f, 8f), Is.False);
        }

        [Test]
        public void CloserVisiblePlayerReplacesCurrentTarget()
        {
            Assert.That(GeoMonsterTargetSelection.ShouldSelectCandidate(true, true, true, 7.98f, 8f), Is.True);
        }

        [Test]
        public void DeadPlayerIsIgnored()
        {
            Assert.That(GeoMonsterTargetSelection.ShouldSelectCandidate(true, false, true, 2f, 8f), Is.False);
        }

        [Test]
        public void InvalidOrDisconnectedPlayerIsIgnored()
        {
            Assert.That(GeoMonsterTargetSelection.ShouldSelectCandidate(false, true, true, 2f, 8f), Is.False);
        }
    }
}
