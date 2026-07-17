using NUnit.Framework;
using TheSancturary.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TheSancturary.Tests
{
    public sealed class TirednessVignetteRulesTests
    {
        [Test]
        public void EffectIsInvisibleAboveFadeInThreshold()
        {
            Assert.That(TirednessVignetteRules.CalculateIntensity(0.75f, 0.5f, 0.42f), Is.EqualTo(0f));
        }

        [Test]
        public void IntensityIncreasesSmoothlyAsNormalizedStaminaApproachesZero()
        {
            var moderate = TirednessVignetteRules.CalculateIntensity(0.4f, 0.5f, 0.42f);
            var low = TirednessVignetteRules.CalculateIntensity(0.2f, 0.5f, 0.42f);
            var empty = TirednessVignetteRules.CalculateIntensity(0f, 0.5f, 0.42f);

            Assert.That(moderate, Is.GreaterThan(0f));
            Assert.That(low, Is.GreaterThan(moderate));
            Assert.That(empty, Is.GreaterThan(low));
        }

        [Test]
        public void IntensityIsClampedToConfiguredMaximum()
        {
            var intensity = TirednessVignetteRules.CalculateIntensity(-10f, 0.5f, 0.35f);

            Assert.That(intensity, Is.EqualTo(0.35f).Within(0.001f));
        }

        [Test]
        public void PresentationUsesNormalizedStamina()
        {
            var first = TirednessVignetteRules.NormalizeStamina(25f, 50f);
            var second = TirednessVignetteRules.NormalizeStamina(100f, 200f);

            Assert.That(first, Is.EqualTo(0.5f));
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void OnlyOwnerMayControlVignette()
        {
            Assert.That(TirednessVignetteRules.ShouldControlVignette(true), Is.True);
            Assert.That(TirednessVignetteRules.ShouldControlVignette(false), Is.False);
        }

        [Test]
        public void RemotePresenterKeepsVolumeDisabled()
        {
            var gameObject = new GameObject("RemoteVignetteTest");
            var definition = ScriptableObject.CreateInstance<PlayerMovementDefinition>();
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var vignette = profile.Add<Vignette>(true);
            var volume = gameObject.AddComponent<Volume>();
            var presenter = gameObject.AddComponent<LocalTirednessVignette>();

            try
            {
                var serialized = new SerializedObject(presenter);
                serialized.FindProperty("movementDefinition").objectReferenceValue = definition;
                serialized.FindProperty("volume").objectReferenceValue = volume;
                serialized.FindProperty("volumeProfile").objectReferenceValue = profile;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                presenter.ConfigureForOwner(false);

                Assert.That(presenter.ControlsVignette, Is.False);
                Assert.That(volume.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(vignette);
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(definition);
            }
        }
    }
}
