using System;

namespace Mirage.Godot.Scripts.Components.Authenticators.SessionId
{
    public interface ISessionIdStore
    {
        bool TryGetSession(out ClientSession? session);
        void StoreSession(ClientSession session);
    }

    public class ClientSession
    {
        public DateTime Timeout { get; set; }
        public byte[] Key { get; set; } = [];

        public bool NeedsRefreshing(TimeSpan tillRefresh)
        {
            var timeRemaining = Timeout - DateTime.Now;

            return timeRemaining < tillRefresh;
        }
    }

    internal class DefaultSessionIdStore : ISessionIdStore
    {
        private ClientSession? _session;

        public void StoreSession(ClientSession session)
        {
            _session = session;
        }

        public bool TryGetSession(out ClientSession? session)
        {
            session = _session;
            return _session != null;
        }
    }
}
