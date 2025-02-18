using Skyline.DataMiner.Automation;
using System;
using System.IO;
using System.IO.Pipes;

namespace SLCASGQIMonitorSendCommand
{
    /// <summary>
    /// Represents a DataMiner Automation script.
    /// </summary>
    public class Script
    {
        private const int ConnectTimeoutMs = 5000;

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
            var scriptParameter = engine.GetScriptParam("command");
            var command = scriptParameter.Value;
            SendCommand(engine, command);
        }

        private void SendCommand(IEngine engine, string command)
        {
            using (var client = CreateClient(engine))
            {
                engine.Log("Client created");

                ConnectClient(engine, client);
                engine.Log("Client connected");

                using (var writer = new StreamWriter(client))
                {
                    WriteCommand(engine, writer, command);
                    engine.Log("Command written");
                }
            }
        }

        private NamedPipeClientStream CreateClient(IEngine engine)
        {
            try
            {
                return new NamedPipeClientStream(".", GQIMonitor.Info.AppName, PipeDirection.Out);
            }
            catch (Exception ex)
            {
                engine.Log($"Failed to create client: {ex}");
                engine.ExitFail(ex.Message);
                return null;
            }
        }

        private void ConnectClient(IEngine engine, NamedPipeClientStream client)
        {
            try
            {
                client.Connect(ConnectTimeoutMs);
            }
            catch (Exception ex)
            {
                engine.Log($"Failed to connect client: {ex}");
                engine.ExitFail(ex.Message);
            }
        }

        private void WriteCommand(IEngine engine, StreamWriter writer, string command)
        {
            try
            {
                writer.WriteLine(command);
            }
            catch (Exception ex)
            {
                engine.Log($"Failed to write command: {ex}");
                engine.ExitFail(ex.Message);
            }
        }
    }
}
