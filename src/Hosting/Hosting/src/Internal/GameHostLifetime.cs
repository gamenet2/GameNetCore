// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Contoso.GameNetCore.Hosting.Internal
{
    internal class GameHostLifetime : IDisposable
    {
        readonly CancellationTokenSource _cts;
        readonly ManualResetEventSlim _resetEvent;
        readonly string _shutdownMessage;

        bool _disposed = false;
        bool _exitedGracefully = false;

        public GameHostLifetime(CancellationTokenSource cts, ManualResetEventSlim resetEvent, string shutdownMessage)
        {
            _cts = cts;
            _resetEvent = resetEvent;
            _shutdownMessage = shutdownMessage;

            AppDomain.CurrentDomain.ProcessExit += ProcessExit;
            Console.CancelKeyPress += CancelKeyPress;
        }

        internal void SetExitedGracefully() =>
            _exitedGracefully = true;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            AppDomain.CurrentDomain.ProcessExit -= ProcessExit;
            Console.CancelKeyPress -= CancelKeyPress;
        }

        void CancelKeyPress(object sender, ConsoleCancelEventArgs eventArgs)
        {
            Shutdown();
            // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
            eventArgs.Cancel = true;
        }

        void ProcessExit(object sender, EventArgs eventArgs)
        {
            Shutdown();
            if (_exitedGracefully)
            {
                // On Linux if the shutdown is triggered by SIGTERM then that's signaled with the 143 exit code.
                // Suppress that since we shut down gracefully. https://github.com/aspnet/AspNetCore/issues/6526
                Environment.ExitCode = 0;
            }
        }

        void Shutdown()
        {
            if (!_cts.IsCancellationRequested)
            {
                if (!string.IsNullOrEmpty(_shutdownMessage))
                    Console.WriteLine(_shutdownMessage);
                try
                {
                    _cts.Cancel();
                }
                catch (ObjectDisposedException) { }
            }

            // Wait on the given reset event
            _resetEvent.Wait();
        }
    }
}
