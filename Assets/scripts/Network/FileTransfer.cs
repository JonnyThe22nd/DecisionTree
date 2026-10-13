using System;
using System.IO;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public enum SharedFileKind : byte
{
    Source,
    Model
}

public class FileTransfer : NetworkBehaviour
{
    public const int MaxFileBytes = 4 * 1024 * 1024;
    const string MessageName = "dt.file";

    public static FileTransfer Instance { get; private set; }

    public event Action<string> Notice;

    public override void OnNetworkSpawn()
    {
        Instance = this;
        NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(MessageName, OnFileMessage);
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this) Instance = null;

        if (NetworkManager != null && NetworkManager.CustomMessagingManager != null)
            NetworkManager.CustomMessagingManager.UnregisterNamedMessageHandler(MessageName);
    }

    public static string FolderFor(SharedFileKind kind)
    {
        return kind == SharedFileKind.Source ? SourceLibrary.FolderPath : ModelLibrary.FolderPath;
    }

    public void Share(SharedFileKind kind, string fileName)
    {
        if (!IsSpawned) return;

        if (IsServer)
        {
            foreach (var clientId in NetworkManager.ConnectedClientsIds)
            {
                if (clientId != NetworkManager.ServerClientId) SendTo(clientId, kind, fileName);
            }
        }
        else
        {
            SendTo(NetworkManager.ServerClientId, kind, fileName);
        }
    }

    public void SendTo(ulong clientId, SharedFileKind kind, string fileName)
    {
        string path = Path.Combine(FolderFor(kind), Path.GetFileName(fileName));
        if (!File.Exists(path)) return;

        byte[] bytes = File.ReadAllBytes(path);
        if (bytes.Length > MaxFileBytes)
        {
            Notice?.Invoke($"'{fileName}' ist größer als {MaxFileBytes / (1024 * 1024)} MB und wird nicht übertragen.");
            return;
        }

        using (var writer = new FastBufferWriter(bytes.Length + fileName.Length * 4 + 64, Allocator.Temp))
        {
            writer.WriteValueSafe((byte)kind);
            writer.WriteValueSafe(Path.GetFileName(fileName));
            writer.WriteValueSafe(bytes);
            NetworkManager.CustomMessagingManager.SendNamedMessage(MessageName, clientId, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }
    }

    void OnFileMessage(ulong senderId, FastBufferReader reader)
    {
        reader.ReadValueSafe(out byte kindByte);
        reader.ReadValueSafe(out string fileName);
        reader.ReadValueSafe(out byte[] bytes);

        var kind = (SharedFileKind)kindByte;
        fileName = Path.GetFileName(fileName);
        if (string.IsNullOrEmpty(fileName) || bytes.Length > MaxFileBytes) return;

        string folder = FolderFor(kind);
        string path = Path.Combine(folder, fileName);

        try
        {
            Directory.CreateDirectory(folder);
            if (!File.Exists(path) || new FileInfo(path).Length != bytes.Length)
                File.WriteAllBytes(path, bytes);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"FileTransfer: '{fileName}' konnte nicht gespeichert werden: {e.Message}");
            return;
        }

        Notice?.Invoke($"Datei '{fileName}' empfangen.");

        if (IsServer)
        {
            foreach (var clientId in NetworkManager.ConnectedClientsIds)
            {
                if (clientId != NetworkManager.ServerClientId && clientId != senderId)
                    SendTo(clientId, kind, fileName);
            }
        }
    }
}
