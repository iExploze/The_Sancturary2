using UnityEngine;

namespace TheSancturary.Player
{
    public static class TirednessVignetteRules
    {
        public static float NormalizeStamina(float currentStamina, float maximumStamina)
        {
            return maximumStamina <= 0f
                ? 0f
                : Mathf.Clamp01(currentStamina / maximumStamina);
        }

        public static float CalculateIntensity(
            float normalizedStamina,
            float fadeInThreshold,
            float maximumIntensity)
        {
            normalizedStamina = Mathf.Clamp01(normalizedStamina);
            fadeInThreshold = Mathf.Clamp(fadeInThreshold, 0.0001f, 1f);
            maximumIntensity = Mathf.Clamp01(maximumIntensity);
            if (normalizedStamina >= fadeInThreshold)
            {
                return 0f;
            }

            var tiredness = 1f - normalizedStamina / fadeInThreshold;
            var smoothTiredness = tiredness * tiredness * (3f - 2f * tiredness);
            return Mathf.Clamp(smoothTiredness * maximumIntensity, 0f, maximumIntensity);
        }

        public static bool ShouldControlVignette(bool isOwner)
        {
            return isOwner;
        }
    }
}
