using GQI.Caches;
using Skyline.DataMiner.Analytics.GenericInterface;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace GQI
{
    internal sealed class CommandReceiver : IDisposable
    {
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
                return new NamedPipeServerStream(GQIMonitor.Info.AppName, PipeDirection.In);
            }
            catch (Exception ex)
            {
                throw new GenIfException($"Failed to create server: {ex.Message}");
            }
        }

        private void Receive()
        {
            Logger.Log("Receive thread started");
            try
            {
                while (true)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    using (var server = CreateServer())
                    {
                        Logger.Log("Server created");

                        _cts.Token.ThrowIfCancellationRequested();
                        WaitForConnection(server);
                        Logger.Log("Client connected");

                        _cts.Token.ThrowIfCancellationRequested();
                        using (var reader = new StreamReader(server))
                        {
                            while (true)
                            {
                                var command = reader.ReadLine();
                                _cts.Token.ThrowIfCancellationRequested();

                                if (command is null)
                                    break;

                                ExecuteCommand(command);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Ignore exceptions for now
                Logger.Log($"Error receiving commands: {ex}");
            }
        }

        private void WaitForConnection(NamedPipeServerStream server)
        {
            server.WaitForConnectionAsync(_cts.Token).GetAwaiter().GetResult();
        }

        private void ExecuteCommand(string command)
        {
            Logger.Log($"Executing command: {command}");
            switch (command)
            {
                case "reload":
                    _cache.Metrics.Clear();
                    _cache.MetricsAnalysis.Clear();
                    break;
                default:
                    return;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _cts.Cancel();
            _thread.Join();
            _cts.Dispose();
        }
    }
}
