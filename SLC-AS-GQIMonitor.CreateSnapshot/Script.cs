namespace CreateSnapshot
{
    using GQIMonitor;
    using Skyline.DataMiner.Automation;
    using Skyline.DataMiner.Net;
    using System;
    using System.IO;

    /// <summary>
    /// Represents a DataMiner Automation script.
    /// </summary>
    public class Script
    {
        /// <summary>
        /// The script entry point.
        /// </summary>
        /// <param name="engine">Link with SLAutomation process.</param>
        public void Run(IEngine engine)
        {
            try
            {
                RunSafe(engine);
            }
            catch (ScriptAbortException)
            {
                // Catch normal abort exceptions (engine.ExitFail or engine.ExitSuccess)
                throw; // Comment if it should be treated as a normal exit of the script.
            }
            catch (ScriptForceAbortException)
            {
                // Catch forced abort exceptions, caused via external maintenance messages.
                throw;
            }
            catch (ScriptTimeoutException)
            {
                // Catch timeout exceptions for when a script has been running for too long.
                throw;
            }
            catch (InteractiveUserDetachedException)
            {
                // Catch a user detaching from the interactive script by closing the window.
                // Only applicable for interactive scripts, can be removed for non-interactive scripts.
                throw;
            }
            catch (Exception e)
            {
                engine.ExitFail("Run|Something went wrong: " + e);
            }
        }

        private void RunSafe(IEngine engine)
        {
            var snapshotName = GetNewSnapshotName();
            var snapshotPath = $@"{Info.DocumentsPath}\Snapshots\{snapshotName}";
            var gqiProvider = GQIProviders.SLHelper;

            try
            {
                Directory.CreateDirectory(snapshotPath);
            }
            catch (Exception ex)
            {
                engine.ExitFail($"Failed to create snapshot directory: {ex.Message}");
                return;
            }

            try
            {
                var connection = engine.GetUserConnection();
                TakeApplicationsSnapshot(connection, snapshotPath);
            }
            catch (Exception ex)
            {
                engine.ExitFail($"Failed to create a snapshot of the applications: {ex.Message}");
                return;
            }

            try
            {
                gqiProvider.TakeSnapshot(snapshotPath);
            }
            catch (Exception ex)
            {
                engine.ExitFail($"Failed to create a snapshot of the logging and metrics: {ex.Message}");
                return;
            }
        }

        private string GetNewSnapshotName() => DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss_fff");

        private void TakeApplicationsSnapshot(IConnection connection, string snapshotPath)
        {
            var applicationsFetcher = new ApplicationsFetcher();
            var applications = applicationsFetcher.GetFromWebAPI(connection);

            var filePath = Path.Combine(snapshotPath, "applications.json");
            Applications.WriteToFile(filePath, applications);
        }
    }
}