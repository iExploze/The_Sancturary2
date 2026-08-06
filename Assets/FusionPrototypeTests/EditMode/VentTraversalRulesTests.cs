using System.Collections.Generic;
using NUnit.Framework;

namespace TheSancturary.FusionPrototype.Tests
{
    public sealed class VentTraversalRulesTests
    {
        [Test]
        public void SelectorUsesFirstClearDestination()
        {
            string[] candidates = { "A", "B", "C" };

            bool found = VentDestinationSelector.TrySelectFirstClear(
                candidates,
                candidate => candidate != "A",
                out string selected);

            Assert.That(found, Is.True);
            Assert.That(selected, Is.EqualTo("B"));
        }

        [Test]
        public void SelectorRejectsWhenEveryDestinationIsBlocked()
        {
            bool found = VentDestinationSelector.TrySelectFirstClear(
                new List<string> { "A", "B" },
                _ => false,
                out string selected);

            Assert.That(found, Is.False);
            Assert.That(selected, Is.Null);
        }

        [TestCase(false, false, false, true)]
        [TestCase(true, false, false, false)]
        [TestCase(false, true, false, false)]
        [TestCase(false, false, true, false)]
        public void EntryEligibilityRejectsConflictingPlayerStates(
            bool dead,
            bool inVent,
            bool inLocker,
            bool expected)
        {
            Assert.That(VentTraversalRules.CanEnter(dead, inVent, inLocker), Is.EqualTo(expected));
        }

        [TestCase(false, true, false, true)]
        [TestCase(false, false, false, false)]
        [TestCase(true, true, false, false)]
        [TestCase(false, true, true, false)]
        public void ExitEligibilityRequiresLiveVentOccupant(
            bool dead,
            bool inVent,
            bool inLocker,
            bool expected)
        {
            Assert.That(VentTraversalRules.CanExit(dead, inVent, inLocker), Is.EqualTo(expected));
        }

        [Test]
        public void VentStatePreventsSprintAndJump()
        {
            Assert.That(VentTraversalRules.CanSprint(true, true, true, false), Is.False);
            Assert.That(VentTraversalRules.CanJump(true, true, true), Is.False);
        }
    }
}
