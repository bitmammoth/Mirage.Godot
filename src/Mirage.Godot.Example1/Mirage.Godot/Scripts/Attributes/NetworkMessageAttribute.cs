using System;

namespace Mirage.Godot.Scripts.Attributes;

/// <summary>
/// Tell the weaver to generate  reader and writer for a class
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class NetworkMessageAttribute : Attribute
{
}
