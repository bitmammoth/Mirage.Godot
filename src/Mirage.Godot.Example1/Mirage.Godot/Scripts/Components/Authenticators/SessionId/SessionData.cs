using System;
using Mirage.Godot.Scripts.Authentication;

namespace Mirage.Godot.Scripts.Components.Authenticators.SessionId;

public class SessionData : IAuthenticationDataWrapper
{
    public DateTime Timeout;
    public PlayerAuthentication PlayerAuthentication;

    object IAuthenticationDataWrapper.Inner => PlayerAuthentication.Data;
}
