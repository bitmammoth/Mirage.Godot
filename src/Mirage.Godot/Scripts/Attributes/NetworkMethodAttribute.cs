using System;

namespace Mirage
{
    /// <summary>
    /// Prevents this method from running unless the NetworkFlags match the current state
    /// <para>Can only be used inside a NetworkBehaviour</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class NetworkMethodAttribute : Attribute
    {
        /// <summary>
        /// If true, if called incorrectly method will throw.<br/>
        /// If false, no error is thrown, but the method won't execute.<br/>
        /// <para>
        /// useful for unity built in methods such as Await, Update, Start, etc.
        /// </para>
        /// </summary>
        public bool error = true;

        public NetworkMethodAttribute(NetworkFlags flags) { }
    }

    [Flags]
    public enum NetworkFlags
    {
        // note: NotActive can't be 0 as it needs its own flag
        //       This is so that people can check for (Server | NotActive)
        /// <summary>
        /// If both server and client are not active. Can be used to check for singleplayer or unspawned object
        /// </summary>
        NotActive = 1,
        Server = 2,
        Client = 4,
        /// <summary>
        /// If either Server or Client is active.
        /// <para>
        /// Note this will not check host mode. For host mode you need to use <see cref="ServerAttribute"/> and <see cref="ClientAttribute"/>
        /// </para>
        /// </summary>
        Active = Server | Client,
        HasAuthority = 8,
        LocalOwner = 16,
    }
    /// <summary>
    /// Converts a string property into a Scene property in the inspector
    /// </summary>
    public sealed class SceneAttribute : PropertyAttribute { }

    /// <summary>
    /// Used to show private SyncList in the inspector,
    /// <para> Use instead of SerializeField for non Serializable types </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ShowInInspectorAttribute : Attribute { }

    /// <summary>
    /// Draws UnityEvent as a foldout
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class FoldoutEventAttribute : PropertyAttribute { }

    /// <summary>
    /// Makes field readonly in inspector.
    /// <para>This is useful for fields that are set by code, but are shown iin inpector for debuggiing</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ReadOnlyInspectorAttribute : PropertyAttribute { }

    /// <summary>
    /// Forces the user to provide a prefab that has a NetworkIdentity component and is registered.
    /// Also provides a fix button to fix the prefab if it hasn't been networked.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NetworkedPrefabAttribute : PropertyAttribute { }

    /// <summary>
    /// Add to NetworkBehaviour to force SyncSettings to be drawn, even if there are no syncvars
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ShowSyncSettingsAttribute : Attribute { }
}
