using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using NUnitAssert = NUnit.Framework.Assert;

namespace TheSancturary.FusionPrototype.Tests
{
    internal static class FusionPlayModeTestSession
    {
        public static IEnumerator ResetExistingSession()
        {
            NetworkRunner[] runners = NetworkRunner.Instances
                .Where(runner => runner != null &&
                    (runner.IsRunning || runner.IsStarting))
                .ToArray();
            Task[] shutdownTasks = runners
                .Select(runner => runner.Shutdown(destroyGameObject: false))
                .ToArray();

            float shutdownTimeout = Time.realtimeSinceStartup + 10f;
            while (shutdownTasks.Any(task => !task.IsCompleted) &&
                   Time.realtimeSinceStartup < shutdownTimeout)
                yield return null;

            NUnitAssert.That(
                shutdownTasks.All(task => task.IsCompleted),
                Is.True,
                "The previous Fusion test session did not shut down in time.");
            NUnitAssert.That(
                shutdownTasks.All(task => !task.IsFaulted),
                Is.True,
                "The previous Fusion test session faulted during shutdown.");

            if (runners.Length > 0)
            {
                float menuTimeout = Time.realtimeSinceStartup + 10f;
                while (FusionSessionManager.Instance != null &&
                       SceneManager.GetActiveScene().path !=
                       FusionSessionManager.MenuScenePath &&
                       Time.realtimeSinceStartup < menuTimeout)
                    yield return null;
            }

            FusionSessionManager manager = FusionSessionManager.Instance;
            if (manager != null)
            {
                Object.Destroy(manager.gameObject);
                yield return null;
            }

            NUnitAssert.That(FusionSessionManager.HasActiveRunner, Is.False);
        }
    }
}
