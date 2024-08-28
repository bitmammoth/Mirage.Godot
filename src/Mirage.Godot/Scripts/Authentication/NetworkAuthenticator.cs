using System;
using System.Threading.Tasks;
using Godot;
using Mirage.Serialization;

namespace Mirage.Authentication
{
    public interface INetworkAuthenticator
    {
        string AuthenticatorName { get; }
    }

    public partial class NetworkAuthenticator : Node, INetworkAuthenticator
    {
        public virtual string AuthenticatorName => GetType().Name;

        internal virtual void Setup(MessageHandler messageHandler, Action<NetworkPlayer, AuthenticationResult> afterAuth)
        {
            // Default implementation can be empty or throw a NotImplementedException.
        }
    }

    
}

