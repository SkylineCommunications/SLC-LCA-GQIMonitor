using GQI.Caches;
using Skyline.DataMiner.Analytics.GenericInterface;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace GQI
{
    internal sealed class CommandReceiver : IDisposable
    {
        private const int BufferSize = 64;
        private static readonly TimeSpan CancelTimeout = TimeSpan.FromSeconds(5);

        private readonly Cache _cache;
        private readonly CancellationTokenSource _cts;
        private readonly Thread _thread;

        private bool _disposed = false;

        public CommandReceiver(Cache cache)
        {
            _cache = cache;
            _cts = new CancellationTokenSource();
            _thread = new Thread(Receive)
            {
                Name = "GQI Monitor command receiver",
                IsBackground = true,
            };
            _thread.Start();
        }

        private NamedPipeServerStream CreateServer()
        {
            try
            {
                return new NamedPipeServerStream(
                    GQIMonitor.Info.AppName,
                    PipeDirection.InOut,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous
                );
            }
            catch (Exception ex)
            {
                throw new GenIfException($"Failed to create server: {ex.Message}");
            }
        }

        private void Receive()
        {
            Logger.Log("Receive command thread started");
            try
            {
                while (true)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    using (var server = CreateServer())
                    {
                        Logger.Log("Command server created");

                        _cts.Token.ThrowIfCancellationRequested();
                        WaitForConnection(server);
                        Logger.Log("Command client connected");

                        _cts.Token.ThrowIfCancellationRequested();
                        var command = ReadCommand(server);

                        if (command is null)
                            continue;

                        _cts.Token.ThrowIfCancellationRequested();
                        var response = ExecuteCommand(command);

                        _cts.Token.ThrowIfCancellationRequested();
                        WriteResponse(server, response);
                    }
                }
            }
            catch (Exception ex)
            {
                // Ignore exceptions for now
                Logger.Log($"Error receiving commands: {ex}");
            }
            Logger.Log("Receive command thread ended");
        }

        private void WaitForConnection(NamedPipeServerStream server)
        {
            server.WaitForConnectionAsync(_cts.Token).GetAwaiter().GetResult();
        }

        private static string ReadCommand(NamedPipeServerStream server)
        {
            using (var reader = new StreamReader(server, Encoding.UTF8, true, BufferSize, leaveOpen: true))
            {
                return reader.ReadLine();
            }
        }

        private static void WriteResponse(NamedPipeServerStream server, string response)
        {
            using (var writer = new StreamWriter(server, Encoding.UTF8, BufferSize, leaveOpen: true))
            {
                writer.WriteLine(response);
                writer.Flush();
            }
        }

        private string ExecuteCommand(string command)
        {
            Logger.Log($"Executing command '{command}'...");
            try
            {
                switch (command)
                {
                    case "reload":
                        _cache.Metrics.Clear();
                        _cache.MetricsAnalysis.Clear();
                        return "OK";
                    default:
                        return "Unknown command";
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed executing command: {ex}");
                return $"Fail: {ex.Message}";
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _cts.Cancel();
            _thread.Join(CancelTimeout);
            _cts.Dispose();
        }
    }
}
