using System.Collections.Generic;
using Unity.Netcode;

public class TreeSync : NetworkBehaviour
{
    public static TreeSync Instance { get; private set; }

    public static bool IsOnline => Instance != null && Instance.IsSpawned;

    readonly List<TreeOperation> log = new List<TreeOperation>();

    public override void OnNetworkSpawn()
    {
        Instance = this;

        if (IsServer)
            NetworkManager.SceneManager.OnSynchronizeComplete += OnClientSynchronized;
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this) Instance = null;

        if (IsServer && NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnSynchronizeComplete -= OnClientSynchronized;
    }

    public void Submit(TreeOperation op)
    {
        SubmitRpc(op);
    }

    [Rpc(SendTo.Server)]
    void SubmitRpc(TreeOperation op)
    {
        if (op.Type == TreeOperationType.CreateRoot && log.Exists(o => o.Type == TreeOperationType.CreateRoot && o.TreeId == op.TreeId))
            return;

        if (op.Type == TreeOperationType.MoveTree)
            log.RemoveAll(o => o.Type == TreeOperationType.MoveTree && o.TreeId == op.TreeId);

        log.Add(op);
        BroadcastRpc(op);
    }

    [Rpc(SendTo.ClientsAndHost)]
    void BroadcastRpc(TreeOperation op)
    {
        TreeRegistry.Apply(op);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    void ReplayRpc(TreeOperation op, RpcParams rpcParams = default)
    {
        TreeRegistry.Apply(op);
    }

    void OnClientSynchronized(ulong clientId)
    {
        if (clientId == NetworkManager.ServerClientId) return;

        var fileTransfer = FileTransfer.Instance;
        foreach (var op in log)
        {
            if (fileTransfer != null && !string.IsNullOrEmpty(op.FileName))
            {
                if (op.Type == TreeOperationType.AddSource) fileTransfer.SendTo(clientId, SharedFileKind.Source, op.FileName);
                else if (op.Type == TreeOperationType.AttachModel) fileTransfer.SendTo(clientId, SharedFileKind.Model, op.FileName);
            }
        }

        foreach (var op in log)
            ReplayRpc(op, RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }
}
