using System;
using Godot;
using Mirage.Godot.Scripts;
using Mirage.Godot.Scripts.Components.Authenticators;
using Mirage.Godot.Scripts.Components.Authenticators.SessionId;
using Mirage.Godot.Scripts.Utils;
using Mirage.Logging;

namespace Mirage.Authenticators.SessionId
{
    public partial class CreateSession : Node
    {
        private static readonly ILogger logger = LogFactory.GetLogger<CreateSession>();

        [Export] public NetworkServer? Server { get; set; }
        [Export] public NetworkClient? Client { get; set; }
        [Export] public required SessionIdAuthenticator Authenticator { get; set; }
        [Export] public bool AutoRefreshSession { get; set; } = true;

        private bool _sentRefresh = false;

        public override void _Ready()
        {
            Client?.Connected.AddListener(ClientConnected);
            Client?.Authenticated.AddListener(ClientAuthenticated);
            Server?.Started.AddListener(ServerStarted);
        }

        private void ServerStarted()
        {
            Server?.MessageHandler.RegisterHandler<RequestSessionMessage>(HandleRequestSession);
        }

        private void ClientConnected(NetworkPlayer player)
        {
            if (Authenticator.ClientIdStore.TryGetSession(out var session) && DateTime.Now < session.Timeout)
            {
                if (logger.LogEnabled()) logger.Log("Client connected, Sending Session Authentication automatically");
                SendAuthentication(session);
            }
        }

        private void SendAuthentication(ClientSession session)
        {
            if (Client == null)
            {
                if (logger.LogEnabled()) logger.Log("Client is null, can't send authentication");
                return;
            }
            var msg = new SessionKeyMessage
            {
                SessionKey = new ArraySegment<byte>(session.Key)
            };
            Authenticator.SendAuthentication(Client, msg);
        }

        private void ClientAuthenticated(NetworkPlayer player)
        {
            if (!Authenticator.ClientIdStore.TryGetSession(out _))
            {
                if (logger.LogEnabled()) logger.Log("Client authenticated but didn't have session, Requesting Session now");
                RequestSession();
            }
        }

        private void RequestSession()
        {
            if (Client == null)
            {
                if (logger.LogEnabled()) logger.Log("Client is null, can't request session");
                return;
            }
            var waiter = new MessageWaiter<SessionKeyMessage>(Client, allowUnauthenticated: false);

            Client?.Send(new RequestSessionMessage());

            _sentRefresh = true;
            waiter.Callback((_, msg) =>
            {
                var key = msg.SessionKey.ToArray(); // Copy to new array
                var session = new ClientSession
                {
                    Key = key,
                    Timeout = DateTime.Now.AddMinutes(Authenticator.TimeoutMinutes),
                };

                Authenticator.ClientIdStore.StoreSession(session);
                _sentRefresh = false;
            });
        }

        private void HandleRequestSession(NetworkPlayer player, RequestSessionMessage message)
        {
            if (logger.LogEnabled()) logger.Log($"{player} requested new session token");
            var sessionKey = Authenticator.CreateOrRefreshSession(player);
            player.Send(new SessionKeyMessage { SessionKey = sessionKey });
        }

        public override void _Process(double delta)
        {
            if (AutoRefreshSession)
            {
                CheckRefresh();
            }
        }

        private void CheckRefresh()
        {
            if (_sentRefresh || Client?.Active != true || !Authenticator.ClientIdStore.TryGetSession(out var session))
            {
                return;
            }

            if (ShouldRefresh(Authenticator.TimeoutMinutes, session.Timeout))
            {
                if (logger.LogEnabled()) logger.Log("Refreshing token before timeout, Requesting Session now");
                RequestSession();
            }
        }

        private static bool ShouldRefresh(int timeoutMinutes, DateTime sessionTimeout)
        {
            var halfTotalTimeout = timeoutMinutes / 2.0;
            var timeRemaining = sessionTimeout - DateTime.Now;

            return timeRemaining.TotalMinutes <= halfTotalTimeout;
        }
    }
}
