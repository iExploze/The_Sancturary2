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
    // Deterministic scene tests use the existing local Fusion path. Online host/client
    // acceptance is exercised separately; these tests must not depend on Photon login.
    [SetUpFixture]
    public sealed class IsolatedFusionPlayModeSessions
    {
        private string _previousAppId;
        [OneTimeSetUp]
        public void Begin()
        {
            var settings = Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings;
            _previousAppId = settings.AppIdFusion;
            settings.AppIdFusion = string.Empty;
        }
        [OneTimeTearDown]
        public void End() => Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings.AppIdFusion = _previousAppId;
    }

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
