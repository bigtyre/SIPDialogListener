using Microsoft.Extensions.Logging;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BigTyre.Phones
{
    /// <summary>
    /// Manages subscription and re-subscription of a SIP notifier client, ensuring it is resubscribed if the subscription fails.
    /// </summary>
    public class SIPNotifierSubscriptionManager : IDisposable
    {
        private bool disposedValue;
        private CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();
        private CancellationToken CancellationToken { get; }
        private TimeSpan SubscriptionCheckInterval { get; } = TimeSpan.FromSeconds(30);
        private TimeSpan ResubscribeTimeThreshold { get; } = TimeSpan.FromMinutes(3);
        private bool IsStarted { get; set; }
        private bool IsSubscribed { get; set; }

        public SIPNotifierSubscriptionManager(SIPNotifierClient client, ILogger<SIPNotifierSubscriptionManager> logger)
        {
            Client = client;
            Logger = logger;

            CancellationToken = CancellationTokenSource.Token;

            Client.SubscriptionSuccessful += Client_SubscriptionSuccessful;
            Client.SubscriptionFailed += Client_SubscriptionFailed;
            ResubscribeAtIntervalUntilCancelledAsync(CancellationToken);
        }

        private void Client_SubscriptionFailed(SIPURI arg1, SIPResponseStatusCodesEnum arg2, string arg3)
        {
            IsSubscribed = false;
        }

        private void Client_SubscriptionSuccessful(SIPURI obj)
        {
            IsSubscribed = true;
        }

        private async void ResubscribeAtIntervalUntilCancelledAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        Resubscribe();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Resubscribe failed: {exceptionMessage}", ex.Message);
                    }
                    await Task.Delay(SubscriptionCheckInterval, cancellationToken);
                }
            }
            catch (Exception)
            {
            }
        }

        private void Resubscribe()
        {
            if (!IsStarted)
            {
                Logger.LogDebug("Skipping resubscribe: client manager has not been started yet.");
                return;
            }

            if (IsSubscribed)
            {
                Logger.LogDebug("Skipping resubscribe: Client is already subscribed.");
                return;
            }

            var lastSubscriptionAttempt = Client.LastSubscribeAttempt;
            var now = DateTime.Now;
            var timeSinceLastAttempt = now - lastSubscriptionAttempt;

            if (timeSinceLastAttempt < ResubscribeTimeThreshold)
            {
                Logger.LogDebug("Skipping resubscribe: Last subscription attempt ({lastSubscriptionAttempt}) is within the resubscribe time threshold ({ResubscribeTimeThreshold}).", lastSubscriptionAttempt, ResubscribeTimeThreshold);
            }

            Logger.LogDebug("Attempting to resubscribe.");
            try
            {
                Client.Stop();
            }
            catch (Exception ex)
            {
                Logger.LogDebug("Failed to stop client. {message}", ex.Message);
            }

            try
            {
                Client.Start();
            }
            catch (Exception ex)
            {
                Logger.LogDebug("Failed to start client. {message}", ex.Message);
            }
            // Client.Resubscribe();
        }

        public SIPNotifierClient Client { get; }
        public ILogger<SIPNotifierSubscriptionManager> Logger { get; }

        public void Start()
        {
            Client.Start();
            IsStarted = true;
        }

        public void Stop()
        {
            Client.Stop();
            IsStarted = false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Client.SubscriptionSuccessful -= Client_SubscriptionSuccessful;
                    Client.SubscriptionFailed -= Client_SubscriptionFailed;
                    Stop();
                    CancellationTokenSource.Cancel();
                    CancellationTokenSource.Dispose();
                }

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

}
