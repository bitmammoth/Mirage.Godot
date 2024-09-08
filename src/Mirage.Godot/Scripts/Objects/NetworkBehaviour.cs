
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using Mirage.Collections;
using Mirage.Logging;
using Mirage.RemoteCalls;
using Mirage.Serialization;
[assembly: InternalsVisibleTo("Mirage.CodeGen")]
namespace Mirage
{
    /// <summary>
    /// Default NetworkBehaviour that can be used to quickly add RPC and Sync var to nodes
    /// <para>
    /// If you dont need RPC or SyncVar use <see cref="INetworkNode"/> instead
    /// </para>
    /// </summary>
    public abstract partial class NetworkBehaviour : Node
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(NetworkBehaviour));
        // protected because it is ok for child classes to set this if they want
        protected internal double _nextSyncTime;
        public const int COMPONENT_INDEX_NOT_FOUND = -1;

        [ExportGroup("Sync Settings")]
        [Export(PropertyHint.Flags)] public SyncFrom From = SyncSettings.Default.From;
        [Export(PropertyHint.Flags)] public SyncTo To = SyncSettings.Default.To;
        [Export(PropertyHint.Flags)] public SyncTiming Timing = SyncSettings.Default.Timing;
        [Export] public float Interval = SyncSettings.Default.Interval;

        /// <summary>
        /// Returns true if this object is active on an active server.
        /// <para>This is only true if the object has been spawned. This is different from NetworkServer.active, which is true if the server itself is active rather than this object being active.</para>
        /// </summary>
        public bool IsServer => Identity.IsServer;

        /// <summary>
        /// Returns true if running as a client and this object was spawned by a server.
        /// </summary>
        public bool IsClient => Identity.IsClient;

        /// <summary>
        /// Returns true if we're on host mode.
        /// </summary>
        [System.Obsolete("use IsHost instead")]
        public bool IsLocalClient => Identity.IsHost;
        /// <summary>
        /// Returns true if we're on host mode.
        /// </summary>
        public bool IsHost => Identity.IsHost;

        /// <summary>
        /// This returns true if this object is the one that represents the player on the local machine.
        /// <para>In multiplayer games, there are multiple instances of the Player object. The client needs to know which one is for "themselves" so that only that player processes input and potentially has a camera attached. The IsLocalPlayer function will return true only for the player instance that belongs to the player on the local machine, so it can be used to filter out input for non-local players.</para>
        /// </summary>
        public bool IsLocalPlayer => Identity.IsLocalPlayer;

        /// <summary>
        /// True if this object only exists on the server
        /// </summary>
        public bool IsServerOnly => IsServer && !IsClient;

        /// <summary>
        /// True if this object exists on a client that is not also acting as a server
        /// </summary>
        public bool IsClientOnly => IsClient && !IsServer;

        /// <summary>
        /// This returns true if this object is the authoritative version of the object in the distributed network application.
        /// <para>The <see cref="NetworkIdentity.HasAuthority">NetworkIdentity.hasAuthority</see> value on the NetworkIdentity determines how authority is determined. For most objects, authority is held by the server. For objects with <see cref="NetworkIdentity.HasAuthority">NetworkIdentity.hasAuthority</see> set, authority is held by the client of that player.</para>
        /// </summary>
        public bool HasAuthority => Identity.HasAuthority;

        /// <summary>
        /// The unique network Id of this object.
        /// <para>This is assigned at runtime by the network server and will be unique for all objects for that network session.</para>
        /// </summary>
        public uint NetId => Identity.NetId;

        /// <summary>
        /// The <see cref="NetworkServer">NetworkClient</see> associated to this object.
        /// </summary>
        public NetworkServer Server => Identity.Server;

        /// <summary>
        /// Quick Reference to the NetworkIdentities ServerObjectManager. Present only for server/host instances.
        /// </summary>
        public ServerObjectManager ServerObjectManager => Identity.ServerObjectManager;

        /// <summary>
        /// The <see cref="NetworkClient">NetworkClient</see> associated to this object.
        /// </summary>
        public NetworkClient Client => Identity.Client;

        /// <summary>
        /// Quick Reference to the NetworkIdentities ClientObjectManager. Present only for instances instances.
        /// </summary>
        public ClientObjectManager ClientObjectManager => Identity.ClientObjectManager;

        /// <summary>
        /// The <see cref="NetworkPlayer"/> associated with this <see cref="NetworkIdentity" /> This is only valid for player objects on the server.
        /// </summary>
        public new INetworkPlayer Owner => Identity.Owner;

        public NetworkWorld World => Identity.World;

        /// <summary>
        /// Returns the appropriate NetworkTime instance based on if this NetworkBehaviour is running as a Server or Client.
        /// </summary>
        public NetworkTime NetworkTime => World.Time;

        /// <summary>
        /// Get Id of this NetworkBehaviour, Its NetId and ComponentIndex
        /// </summary>
        /// <returns></returns>
        public Id BehaviourId => new Id(this);
        public ulong SyncVarDirtyBits => _syncVarDirtyBits;
        private NetworkIdentity _identity;
        private int? _componentIndex;
        private readonly List<ISyncObject> syncObjects = new List<ISyncObject>();

        public NetworkIdentity Identity
        {
            get
            {
                if (_identity == null)
                {
                    _identity = this.TryGetNetworkIdentity();
                }
                return _identity;
            }
        }
        internal bool _anySyncObjectDirty;
        internal ulong _syncVarDirtyBits;
        private bool _syncObjectsInitialized;
        private ulong _syncVarHookGuard;

        protected internal bool GetSyncVarHookGuard(ulong dirtyBit)
        {
            return (_syncVarHookGuard & dirtyBit) != 0UL;
        }

        protected internal void SetSyncVarHookGuard(ulong dirtyBit, bool value)
        {
            if (value)
                _syncVarHookGuard |= dirtyBit;
            else
                _syncVarHookGuard &= ~dirtyBit;
        }
        /// <summary>
        /// Returns the index of the component on this object
        /// </summary>
        public int ComponentIndex
        {
            get
            {
                if (_componentIndex.HasValue)
                    return _componentIndex.Value;

                // note: FindIndex causes allocations, we search manually instead
                for (var i = 0; i < Identity.NetworkBehaviours.Length; i++)
                {
                    var component = Identity.NetworkBehaviours[i];
                    if (component == this)
                    {
                        _componentIndex = i;
                        return i;
                    }
                }

                // this should never happen
                logger.LogError("Could not find component in GameObject. You should not add/remove components in networked objects dynamically");

                return COMPONENT_INDEX_NOT_FOUND;
            }
        }

        public SyncSettings SyncSettings => new SyncSettings(From, To, Timing, Interval);

        public virtual int GetRpcCount()
        {
            // generated by weaver
            return 0;
        }

        public virtual void RegisterRpc(RemoteCallCollection remoteCallCollection)
        {
            // generated by weaver
        }

        public virtual bool SerializeSyncVars(NetworkWriter writer, bool initial)
        {
            // generated by weaver
            return false;
        }

        public virtual void DeserializeSyncVars(NetworkReader reader, bool initial)
        {
            // generated by weaver
        }

        /// <summary>
        /// calls SetNetworkBehaviour on each SyncObject, but only once
        /// </summary>
        internal void InitializeSyncObjects()
        {
            if (_syncObjectsInitialized)
                return;

            _syncObjectsInitialized = true;

            // find all the ISyncObjects in this behaviour
            foreach (var syncObject in syncObjects)
                syncObject.SetNetworkBehaviour(this);
        }

        /// <summary>
        /// this gets called in the constructor by the weaver
        /// for every SyncObject in the component (e.g. SyncLists).
        /// We collect all of them and we synchronize them with OnSerialize/OnDeserialize 
        /// </summary>
        /// <param name="syncObject"></param>
        protected internal void InitSyncObject(ISyncObject syncObject)
        {
            syncObjects.Add(syncObject);
        }

        private void SyncObject_OnChange()
        {
            if (SyncSettings.ShouldSyncFrom(Identity, true))
            {
                _anySyncObjectDirty = true;
                Identity.SyncVarSender.AddDirtyObject(Identity);
            }
        }

        /// <summary>
        /// Call this after updating SyncSettings to update all SyncObjects
        /// <para>
        /// This only needs to be called manually if updating syncSettings at runtime.
        /// Mirage will automatically call this after serializing or deserializing with initialState
        /// </para>
        /// </summary>
        public void UpdateSyncObjectShouldSync()
        {
            var shouldSync = SyncSettings.ShouldSyncFrom(Identity, true);

            if (logger.LogEnabled()) logger.Log($"Settings SyncObject sync on to {shouldSync} for {Name}");
            for (var i = 0; i < syncObjects.Count; i++)
            {
                syncObjects[i].SetShouldSyncFrom(shouldSync);
            }
        }
        public bool SyncVarEqual<T>(T value, T fieldValue)
        {
            // newly initialized or changed value?
            return EqualityComparer<T>.Default.Equals(value, fieldValue);
        }
        /// <summary>
        /// Used to set the behaviour as dirty, so that a network update will be sent for the object.
        /// these are masks, not bit numbers, ie. 0x004 not 2
        /// </summary>
        /// <param name="bitMask">Bit mask to set.</param>
        public void SetDirtyBit(ulong bitMask)
        {
            if (logger.LogEnabled()) logger.Log($"Dirty bit set {bitMask} on {Identity}");

            _syncVarDirtyBits |= bitMask;

            if (SyncSettings.ShouldSyncFrom(Identity, false))
                Identity.SyncVarSender.AddDirtyObject(Identity);
        }


        /// <summary>
        /// Used to clear dirty bit.
        /// <para>Object may still be in dirty list, so will be checked in next update. but values in this mask will no longer be set until they are changed again</para>
        /// </summary>
        /// <param name="bitMask">Bit mask to set.</param>
        public void ClearDirtyBit(ulong bitMask)
        {
            _syncVarDirtyBits &= ~bitMask;
        }

        public void ClearDirtyBits()
        {
            _syncVarDirtyBits = 0L;

            // flush all unsynchronized changes in syncobjects
            for (var i = 0; i < syncObjects.Count; i++)
            {
                syncObjects[i].Flush();
            }
            _anySyncObjectDirty = false;
        }

        /// <summary>
        /// Clears dirty bits and sets the next sync time
        /// </summary>
        /// <param name="now"></param>
        public void ClearShouldSync(double now)
        {
            SyncSettings.UpdateTime(ref _nextSyncTime, now);
            ClearDirtyBits();
        }

        /// <summary>
        /// True if this behaviour is dirty and it is time to sync
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ShouldSync(double time)
        {
            return AnyDirtyBits() && TimeToSync(time);
        }

        /// <summary>
        /// If it is time to sync based on last sync and <see cref="SyncSettings"/>
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TimeToSync(double time)
        {
            return time >= _nextSyncTime;
        }

        /// <summary>
        /// Are any SyncVar or SyncObjects dirty
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AnyDirtyBits()
        {
            return _syncVarDirtyBits != 0L || _anySyncObjectDirty;
        }

        /// <summary>
        /// Virtual function to override to send custom serialization data. The corresponding function to send serialization data is OnDeserialize().
        /// </summary>
        /// <remarks>
        /// <para>The initialState flag is useful to differentiate between the first time an object is serialized and when incremental updates can be sent. The first time an object is sent to a client, it must include a full state snapshot, but subsequent updates can save on bandwidth by including only incremental changes. Note that SyncVar hook functions are not called when initialState is true, only for incremental updates.</para>
        /// <para>If a class has SyncVars, then an implementation of this function and OnDeserialize() are added automatically to the class. So a class that has SyncVars cannot also have custom serialization functions.</para>
        /// <para>The OnSerialize function should return true to indicate that an update should be sent. If it returns true, then the dirty bits for that script are set to zero, if it returns false then the dirty bits are not changed. This allows multiple changes to a script to be accumulated over time and sent when the system is ready, instead of every frame.</para>
        /// </remarks>
        /// <param name="writer">Writer to use to write to the stream.</param>
        /// <param name="initialState">If this is being called to send initial state.</param>
        /// <returns>True if data was written.</returns>
        public virtual bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            var objectWritten = SerializeObjects(writer, initialState);

            var syncVarWritten = SerializeSyncVars(writer, initialState);

            return objectWritten || syncVarWritten;
        }

        /// <summary>
        /// Virtual function to override to receive custom serialization data. The corresponding function to send serialization data is OnSerialize().
        /// </summary>
        /// <param name="reader">Reader to read from the stream.</param>
        /// <param name="initialState">True if being sent initial state.</param>
        public virtual void OnDeserialize(NetworkReader reader, bool initialState)
        {
            DeserializeObjects(reader, initialState);

            _deserializeMask = 0;
            DeserializeSyncVars(reader, initialState);
        }

        /// <summary>
        /// mask from the most recent DeserializeSyncVars
        /// </summary>
        internal ulong _deserializeMask;
        public void SetDeserializeMask(ulong dirtyBit, int offset)
        {
            _deserializeMask |= dirtyBit << offset;
        }
        internal ulong DirtyObjectBits()
        {
            ulong dirtyObjects = 0;
            for (var i = 0; i < syncObjects.Count; i++)
            {
                var syncObject = syncObjects[i];
                if (syncObject.IsDirty)
                {
                    dirtyObjects |= 1UL << i;
                }
            }
            return dirtyObjects;
        }

        public bool SerializeObjects(NetworkWriter writer, bool initialState)
        {
            if (syncObjects.Count == 0)
                return false;

            if (initialState)
            {
                var written = SerializeObjectsAll(writer);
                // after initial we need to set up objects for syncDirection
                UpdateSyncObjectShouldSync();
                return written;
            }
            else
            {
                return SerializeObjectsDelta(writer);
            }
        }

        public bool SerializeObjectsAll(NetworkWriter writer)
        {
            var dirty = false;
            for (var i = 0; i < syncObjects.Count; i++)
            {
                var syncObject = syncObjects[i];
                syncObject.OnSerializeAll(writer);
                dirty = true;
            }
            return dirty;
        }

        public bool SerializeObjectsDelta(NetworkWriter writer)
        {
            var dirty = false;
            // write the mask
            writer.WritePackedUInt64(DirtyObjectBits());
            // serializable objects, such as synclists
            for (var i = 0; i < syncObjects.Count; i++)
            {
                var syncObject = syncObjects[i];
                if (syncObject.IsDirty)
                {
                    syncObject.OnSerializeDelta(writer);
                    dirty = true;
                }
            }
            return dirty;
        }

        internal void DeserializeObjects(NetworkReader reader, bool initialState)
        {
            if (syncObjects.Count == 0)
                return;

            if (initialState)
            {
                DeSerializeObjectsAll(reader);
                UpdateSyncObjectShouldSync();
            }
            else
            {
                DeSerializeObjectsDelta(reader);
            }
        }

        internal void DeSerializeObjectsAll(NetworkReader reader)
        {
            for (var i = 0; i < syncObjects.Count; i++)
            {
                var syncObject = syncObjects[i];
                syncObject.OnDeserializeAll(reader);
            }
        }

        internal void DeSerializeObjectsDelta(NetworkReader reader)
        {
            var dirty = reader.ReadPackedUInt64();
            for (var i = 0; i < syncObjects.Count; i++)
            {
                var syncObject = syncObjects[i];
                if ((dirty & (1UL << i)) != 0)
                {
                    syncObject.OnDeserializeDelta(reader);
                }
            }
        }

        internal void ResetSyncObjects()
        {
            foreach (var syncObject in syncObjects)
            {
                syncObject.Reset();
            }
        }

        public struct Id : IEquatable<Id>
        {
            public readonly uint NetId;
            public readonly int ComponentIndex;

            public Id(uint netId, int componentIndex)
            {
                NetId = netId;
                ComponentIndex = componentIndex;
            }

            public Id(NetworkBehaviour behaviour)
            {
                NetId = behaviour.Identity.NetId;
                ComponentIndex = behaviour.ComponentIndex;
            }

            public override int GetHashCode()
            {
                return ((int)NetId * 256) + ComponentIndex;
            }

            public bool Equals(Id other)
            {
                return (NetId == other.NetId) && (ComponentIndex == other.ComponentIndex);
            }

            public override bool Equals(object obj)
            {
                if (obj is Id id)
                    return Equals(id);
                else
                    return false;
            }
        }
    }
}
