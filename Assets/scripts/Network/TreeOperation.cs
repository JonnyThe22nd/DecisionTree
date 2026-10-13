using Unity.Netcode;
using UnityEngine;

public enum TreeOperationType : byte
{
    CreateRoot,
    AddChild,
    AddSource,
    AttachModel,
    MoveTree
}

public struct TreeOperation : INetworkSerializable
{
    public TreeOperationType Type;
    public string TreeId;
    public string NodeId;
    public string ParentId;
    public string Name;
    public string Reasoning;
    public string Author;
    public string FileName;
    public ArgumentationKind Kind;
    public BeliefValue Belief;
    public long Ticks;
    public Vector3 Offset;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsWriter)
        {
            TreeId ??= "";
            NodeId ??= "";
            ParentId ??= "";
            Name ??= "";
            Reasoning ??= "";
            Author ??= "";
            FileName ??= "";
        }

        serializer.SerializeValue(ref Type);
        serializer.SerializeValue(ref TreeId);
        serializer.SerializeValue(ref NodeId);
        serializer.SerializeValue(ref ParentId);
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref Reasoning);
        serializer.SerializeValue(ref Author);
        serializer.SerializeValue(ref FileName);
        serializer.SerializeValue(ref Kind);
        serializer.SerializeValue(ref Belief);
        serializer.SerializeValue(ref Ticks);
        serializer.SerializeValue(ref Offset);
    }
}
