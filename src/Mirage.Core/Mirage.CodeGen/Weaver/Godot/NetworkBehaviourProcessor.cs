using System.Collections.Generic;
using Mirage.CodeGen.Mirage.CecilExtensions.Logging;
using Mirage.CodeGen.Weaver.Godot.NetworkBehaviour;
using Mirage.CodeGen.Weaver.Processors;
using Mirage.CodeGen.Weaver.Serialization;
using Obj = Mirage.Godot.Scripts.Objects;
using Mirage.Godot.Scripts.Attributes;
using Mirage.Weaver;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace Mirage.CodeGen.Weaver.Godot
{
    public enum RemoteCallType
    {
        ServerRpc,
        ClientRpc
    }

    public enum ReturnType
    {
        Void,
        Task,
    }

    /// <summary>
    /// processes SyncVars, Cmds, Rpcs, etc. of NetworkBehaviours
    /// </summary>
    internal class NetworkBehaviourProcessor
    {
        private readonly TypeDefinition _netBehaviourSubclass;
        private readonly IWeaverLogger _logger;
        private readonly ServerRpcProcessor _serverRpcProcessor;
        private readonly ClientRpcProcessor _clientRpcProcessor;
        private readonly SyncVarProcessor _syncVarProcessor;
        private readonly SyncObjectProcessor _syncObjectProcessor;
        private readonly ConstFieldTracker _rpcCounter;

        public NetworkBehaviourProcessor(TypeDefinition td, Readers readers, Writers writers, PropertySiteProcessor propertySiteProcessor, IWeaverLogger logger)
        {
            Weaver.DebugLog(td, "NetworkBehaviourProcessor");
            _netBehaviourSubclass = td;
            this._logger = logger;
            _serverRpcProcessor = new ServerRpcProcessor(_netBehaviourSubclass.Module, readers, writers, logger);
            _clientRpcProcessor = new ClientRpcProcessor(_netBehaviourSubclass.Module, readers, writers, logger);
            _syncVarProcessor = new SyncVarProcessor(_netBehaviourSubclass.Module, readers, writers, propertySiteProcessor);
            _syncObjectProcessor = new SyncObjectProcessor(readers, writers, logger);

            // no max for rpcs, index is sent as var int, so more rpc just means bigger header size (still smaller than 4 byte hash)
            _rpcCounter = new ConstFieldTracker("RPC_COUNT", td, int.MaxValue, "Rpc");
        }

        // return true if modified
        public bool Process()
        {
            // only process once
            if (WasProcessed(_netBehaviourSubclass))
                return false;
            Weaver.DebugLog(_netBehaviourSubclass, $"Found NetworkBehaviour {_netBehaviourSubclass.FullName}");

            Weaver.DebugLog(_netBehaviourSubclass, "Process Start");
            MarkAsProcessed(_netBehaviourSubclass);

            try
            {
                _syncVarProcessor.ProcessSyncVars(_netBehaviourSubclass, _logger);
            }
            catch (NetworkBehaviourException e)
            {
                _logger.Error(e);
            }

            _syncObjectProcessor.ProcessSyncObjects(_netBehaviourSubclass);

            ProcessRpcs();

            Weaver.DebugLog(_netBehaviourSubclass, "Process Done");
            return true;
        }

        #region mark / check type as processed
        public const string ProcessedFunctionName = "MirageProcessed";

        // by adding an empty MirageProcessed() function
        public static bool WasProcessed(TypeDefinition td)
        {
            return td.GetMethod(ProcessedFunctionName) != null;
        }

        public static void MarkAsProcessed(TypeDefinition td)
        {
            if (!WasProcessed(td))
            {
                var versionMethod = td.AddMethod(ProcessedFunctionName, MethodAttributes.Private);
                var worker = versionMethod.Body.GetILProcessor();
                worker.Append(worker.Create(OpCodes.Ret));
            }
        }
        #endregion

        private void RegisterRpcs(List<RpcMethod> rpcs)
        {
            Weaver.DebugLog(_netBehaviourSubclass, "Set const RPC Count");
            SetRpcCount(rpcs.Count);

            // if there are no rpcs then we dont need to override method
            if (rpcs.Count == 0)
                return;

            Weaver.DebugLog(_netBehaviourSubclass, "Override RegisterRPC");

            var helper = new RegisterRpcHelper(_netBehaviourSubclass.Module, _netBehaviourSubclass);
            if (helper.HasManualOverride())
                throw new RpcException($"{helper.MethodName} should not have a manual override", helper.GetManualOverride());

            helper.AddMethod();

            RegisterRpc.RegisterAll(helper.Worker, rpcs);

            helper.Worker.Emit(OpCodes.Ret);
        }

        private void SetRpcCount(int count)
        {
            // set const so that child classes know count of base classes
            _rpcCounter.Set(count);

            // override virtual method so returns total
            var method = _netBehaviourSubclass.AddMethod(nameof(Obj.NetworkBehaviour.GetRpcCount), MethodAttributes.Virtual | MethodAttributes.Public, typeof(int));
            var worker = method.Body.GetILProcessor();
            // write count of base+current so that `GetInBase` call will return total
            worker.Emit(OpCodes.Ldc_I4, _rpcCounter.GetInBase() + count);
            worker.Emit(OpCodes.Ret);
        }

        private void ProcessRpcs()
        {
            // copy the list of methods because we will be adding methods in the loop
            var methods = new List<MethodDefinition>(_netBehaviourSubclass.Methods);

            var rpcs = new List<RpcMethod>();

            var index = _rpcCounter.GetInBase();
            foreach (var md in methods)
            {
                try
                {
                    var rpc = CheckAndProcessRpc(md, index);
                    if (rpc != null)
                    {
                        // increment only if rpc was count
                        index++;
                        rpcs.Add(rpc);
                    }
                }
                catch (RpcException e)
                {
                    _logger.Error(e);
                }
            }

            RegisterRpcs(rpcs);
        }

        private RpcMethod CheckAndProcessRpc(MethodDefinition md, int index)
        {
            if (md.TryGetCustomAttribute<ServerRpcAttribute>(out var serverAttribute))
            {
                if (md.HasCustomAttribute<ClientRpcAttribute>()) throw new RpcException("Method should not have both ServerRpc and ClientRpc", md);

                return _serverRpcProcessor.ProcessRpc(md, serverAttribute, index);
            }
            else if (md.TryGetCustomAttribute<ClientRpcAttribute>(out var clientAttribute))
            {
                return _clientRpcProcessor.ProcessRpc(md, clientAttribute, index);
            }
            return null;
        }
    }
}
