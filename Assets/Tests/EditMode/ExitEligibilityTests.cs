using NUnit.Framework;
using TheSancturary.Multiplayer;

namespace TheSancturary.Tests
{
    public sealed class ExitEligibilityTests
    {
        [TestCase(true, true, true)]
        [TestCase(false, true, false)]
        [TestCase(true, false, false)]
        [TestCase(false, false, false)]
        public void OnlySpawnedPlayerObjectsCanEscape(
            bool isSpawned,
            bool isPlayerObject,
            bool expected)
        {
            Assert.That(ExitEligibility.IsEligible(isSpawned, isPlayerObject), Is.EqualTo(expected));
        }
    }
}
