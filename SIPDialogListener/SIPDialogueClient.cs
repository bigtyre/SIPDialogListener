using Microsoft.Extensions.Logging;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace BigTyre.Phones
{
    /// <summary>
    /// Registers with a SIP server for each of the configured SIP accounts and publishes events when dialog messages are received.
    /// </summary>
    public class SIPDialogueClient : IDisposable
    {
        private bool _isDisposed;
        private readonly SIPTransport _sipTransport;
        private readonly List<SIPNotifierSubscriptionManager> _clients = new List<SIPNotifierSubscriptionManager>();
        private readonly XmlSerializer _xmlSerializer;
        private readonly ILoggerFactory _loggerFactory;

        private PBXSettings PBXSettings { get; }
        private List<string> MonitoredExtensions { get; }
        private SIPAccount SipAccount { get; }
        public ILogger<SIPDialogueClient> Logger { get; }

        public event EventHandler<SIPDialogNotificationEventArgs> NotificationReceived;

        private readonly SIPProtocolsEnum protocol = SIPProtocolsEnum.tcp;
        public SIPDialogueClient(
            string clientIp,
            int clientPort,
            PBXSettings pbxSettings,
            List<string> extensionsToMonitor,
            SIPAccount sipAccount,
            ILogger<SIPDialogueClient> logger,
            ILoggerFactory loggerFactory
        )
        {
            PBXSettings = pbxSettings;
            MonitoredExtensions = extensionsToMonitor;
            SipAccount = sipAccount;
            Logger = logger;
            _loggerFactory = loggerFactory;
            _xmlSerializer = new XmlSerializer(typeof(DialogInfo));

            var sipTransport = new SIPTransport() { };
            //sipTransport.ContactHost = clientIp;
            //var channel = new SIPTCPChannel(ClientIp, clientPort);
            var sipChannel = sipTransport.CreateChannel(protocol, System.Net.Sockets.AddressFamily.InterNetwork, port: 5060);
            sipTransport.AddSIPChannel(sipChannel);
            sipTransport.EnableTraceLogs();


            sipTransport.ContactHost = $"{clientIp}:{clientPort}"; // or appropriate IP/Port configuration

            _sipTransport = sipTransport;
        }

        internal void Start()
        {
            // Get the monitored extensions
            var extensions = MonitoredExtensions;
            if (extensions.Count < 1) throw new Exception("No extensions configured for monitoring.");

            // Configure PBX settings
            var pbxSettings = PBXSettings;
            var pbxPort = pbxSettings.Port;
            if (pbxPort == default) throw new Exception("PBX Port not configured.");
            var realm = pbxSettings.AuthenticationRealm ?? throw new    Exception("PBX Authentication Realm not configured.");
            var pbxIp = IPAddress.Parse(pbxSettings.IPAddress ?? throw new Exception("PBX IP Address not configured."));
            var expiry = (int)pbxSettings.ExpiryTime.TotalSeconds;

            // Configure SIP Account
            var account = SipAccount;
            var username = account.Username;
            var clientExtension = account.Extension;
            var password = account.Password;

            Logger.LogInformation("SIP Notifier starting. Server: {pbxIp}, Username: {username}, Registration Expiry Secs: {expiry}", pbxIp, username, expiry);

            // Create clients
            var pbxEndpoint = new IPEndPoint(pbxIp, pbxPort);
            var outboundProxy = new SIPEndPoint(protocol, pbxEndpoint);

            RemoveAllClients();

            foreach (var extension in extensions)
            {
                var monitoredExtensionUri = new SIPURI(
                    user: extension,
                    host: pbxIp.ToString(),
                    paramsAndHeaders: "",
                    scheme: SIPSchemesEnum.sip
                );

                var client = new SIPNotifierClient(
                    sipTransport: _sipTransport,
                    outboundProxy: outboundProxy,
                    sipEventPackage: SIPEventPackagesEnum.Dialog,
                    resourceURI: monitoredExtensionUri,
                    authUsername: clientExtension,
                    authDomain: realm,
                    authPassword: password,
                    expiry: expiry,
                    filter: ""
                );

                client.NotificationReceived += HandleSIPNotificationReceived;
                client.SubscriptionSuccessful += HandleSIPSubscriptionSuccessful;
                client.SubscriptionFailed += HandleSIPSubscriptionFailed;

                var manager = new SIPNotifierSubscriptionManager(
                    client,
                    _loggerFactory.CreateLogger<SIPNotifierSubscriptionManager>()
                );

                manager.Start();

                _clients.Add(manager);
            }
        }

        internal void Stop()
        {
            RemoveAllClients();
        }

        private void RemoveAllClients()
        {
            Logger.LogInformation("Removing all clients.");
            var clients = _clients.ToList();
            foreach (var client in clients)
            {
                client.Stop();

                var notifierClient = client.Client;
                notifierClient.NotificationReceived -= HandleSIPNotificationReceived;
                notifierClient.SubscriptionSuccessful -= HandleSIPSubscriptionSuccessful;
                notifierClient.SubscriptionFailed -= HandleSIPSubscriptionFailed;

                client.Dispose();
                _clients.Remove(client);
            }
        }

        private void HandleSIPSubscriptionSuccessful(SIPURI obj)
        {
            string extension = obj.UnescapedUser;
            Logger.LogInformation("Subscription for ext. {extension} successful.", extension);
        }

        private void HandleSIPSubscriptionFailed(SIPURI obj, SIPResponseStatusCodesEnum status, string msg)
        {
            string extension = obj.UnescapedUser;
            var statusText = Enum.GetName(status.GetType(), status);
            Logger.LogWarning("Subscription failed for ext. {extension}. {statusText} {msg}", extension, statusText, msg);
        }

        void HandleSIPNotificationReceived(SIPEventPackagesEnum eventType, string msg)
        {
            try
            {
                Logger.LogDebug("SIP Notification received. Event type: {eventType}", eventType);

                if (eventType != SIPEventPackagesEnum.Dialog)
                    return;

                Logger.LogDebug("Message received: {message}", Environment.NewLine + msg);
                var dialogInfo = DeserializeDialogInfoFromXML(msg);
                if (dialogInfo == null)
                {
                    Logger.LogWarning("Failed to deserialize message from XML.");
                    return;
                }
                OnNotificationReceived(dialogInfo);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while handling {eventName} from {eventType}: {errorMessage}.", nameof(SIPNotifierClient.NotificationReceived), eventType, ex.Message);
            }
        }

        DialogInfo DeserializeDialogInfoFromXML(string xml)
        {
            if (string.IsNullOrEmpty(xml)) 
                return null;

            xml = StripXmlNamespaces(xml);

            try
            {
                var xmlStream = new StringReader(xml);
                var deserializedObject = _xmlSerializer.Deserialize(xmlStream);
                if (!(deserializedObject is DialogInfo info)) 
                    return null;

                return info;
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to deserialize DialogInfo: {message}", ex.Message);
                return default;
            }
        }

        static string StripXmlNamespaces(string content)
        {
            return Regex.Replace(content, @"\s?xmlns=\""[^\""]+\""", "");
        }

        private void OnNotificationReceived(DialogInfo dialogInfo)
        {
            NotificationReceived?.Invoke(this, new SIPDialogNotificationEventArgs(dialogInfo));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                RemoveAllClients();
                _sipTransport.Dispose();
            }
            _isDisposed = true;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

}
