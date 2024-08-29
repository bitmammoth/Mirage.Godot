using System;

namespace Mirage.Godot.Scripts.Objects;

/// <summary>
/// Exception thrown when spawning fails
/// </summary>
[Serializable]
public class SpawnObjectException(string message) : Exception(message)
{
}
