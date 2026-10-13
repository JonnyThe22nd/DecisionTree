using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static partial class DecisionTreeSetup
{
    const string TestPlayerName = "Test";

    [MenuItem("DecisionTree/Netzwerk in Szene einrichten (überschreibt)")]
    public static void BuildNetwork()
    {
        LoadResources();

        var avatar = BuildAvatarPrefab();
        var panelPrefab = BuildSessionPanelPrefab();

        RemoveFromScene("NetworkManager");
        RemoveFromScene("NetworkSync");
        RemoveFromScene("SessionPanel");

        var managerObject = new GameObject("NetworkManager");
        var networkManager = managerObject.AddComponent<NetworkManager>();
        var transport = managerObject.AddComponent<UnityTransport>();
        var voice = managerObject.AddComponent<VoiceChat>();
        var session = managerObject.AddComponent<SessionManager>();
        session.voice = voice;

        networkManager.NetworkConfig.NetworkTransport = transport;
        networkManager.NetworkConfig.PlayerPrefab = avatar;
        RegisterPrefab(networkManager, avatar);

        var syncObject = new GameObject("NetworkSync");
        syncObject.AddComponent<NetworkObject>();
        syncObject.AddComponent<TreeSync>();
        syncObject.AddComponent<FileTransfer>();

        var panel = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab);
        panel.name = "SessionPanel";
        PlacePanel(panel.transform);

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("DecisionTreeSetup: Netzwerk in Szene eingerichtet.");
    }

    [MenuItem("DecisionTree/Test: lokalen Host starten")]
    static void StartLocalHost()
    {
        SessionManager.Instance.StartLocalHost(TestPlayerName);
    }

    [MenuItem("DecisionTree/Test: lokalen Host starten", true)]
    static bool StartLocalHostValidate()
    {
        return Application.isPlaying && SessionManager.Instance != null;
    }

    static void RemoveFromScene(string objectName)
    {
        var existing = GameObject.Find(objectName);
        if (existing != null) Object.DestroyImmediate(existing);
    }

    static void RegisterPrefab(NetworkManager networkManager, GameObject prefab)
    {
        var lists = networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists;
        NetworkPrefabsList list;

        if (lists.Count > 0)
        {
            list = lists[0];
        }
        else
        {
            list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            AssetDatabase.CreateAsset(list, PrefabFolder + "/NetworkPrefabs.asset");
            lists.Add(list);
        }

        if (!list.Contains(prefab)) list.Add(new NetworkPrefab { Prefab = prefab });
        EditorUtility.SetDirty(list);
    }

    static void PlacePanel(Transform panel)
    {
        var origin = GameObject.Find("XR Origin (XR Rig)");
        Vector3 head = origin != null ? origin.transform.position + Vector3.up * 1.3f : new Vector3(0f, 1.3f, 0f);
        Vector3 forward = origin != null ? origin.transform.forward : Vector3.forward;
        forward.y = 0f;

        panel.position = head + forward.normalized * 1.1f;
        panel.rotation = Quaternion.LookRotation(panel.position - head);
    }

    static GameObject BuildAvatarPrefab()
    {
        var root = new GameObject("NetworkAvatar");
        root.AddComponent<NetworkObject>();
        var avatar = root.AddComponent<NetworkAvatar>();

        avatar.head = BuildAvatarPart(root.transform, "Head", PrimitiveType.Sphere, 0.22f);
        avatar.leftHand = BuildAvatarPart(root.transform, "LeftHand", PrimitiveType.Cube, 0.08f);
        avatar.rightHand = BuildAvatarPart(root.transform, "RightHand", PrimitiveType.Cube, 0.08f);

        var label = new GameObject("Name").AddComponent<TextMeshPro>();
        label.transform.SetParent(root.transform, false);
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 1.5f;
        label.color = Color.white;
        label.rectTransform.sizeDelta = new Vector2(1.5f, 0.3f);
        avatar.nameLabel = label;

        var asset = PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/NetworkAvatar.prefab");
        Object.DestroyImmediate(root);
        return asset;
    }

    static Transform BuildAvatarPart(Transform parent, string partName, PrimitiveType shape, float size)
    {
        var part = GameObject.CreatePrimitive(shape);
        part.name = partName;
        part.transform.SetParent(parent, false);
        part.transform.localScale = Vector3.one * size;
        Object.DestroyImmediate(part.GetComponent<Collider>());

        var networkTransform = part.AddComponent<NetworkTransform>();
        networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
        networkTransform.SyncScaleX = false;
        networkTransform.SyncScaleY = false;
        networkTransform.SyncScaleZ = false;
        return part.transform;
    }

    static GameObject BuildSessionPanelPrefab()
    {
        var go = new GameObject("SessionPanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
        go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        go.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 4f;

        var canvasRT = go.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(700f, 640f);
        canvasRT.localScale = Vector3.one * 0.001f;

        var background = go.AddComponent<Image>();
        background.color = panelColor;

        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 20, 20);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var title = CreateText(canvasRT, "Title", "Sitzung", 40f, TextAlignmentOptions.Center);
        SetHeight(title, 50f);

        var status = CreateText(canvasRT, "Status", "Nicht verbunden", 22f, TextAlignmentOptions.Center);
        status.textWrappingMode = TextWrappingModes.Normal;
        status.color = hintColor;
        SetHeight(status, 90f);

        var lobby = CreateGroup(canvasRT, "LobbyGroup", 310f);
        var nameField = CreateInputField(lobby, "NameField");
        ((TextMeshProUGUI)nameField.placeholder).text = "Dein Name...";
        SetHeight(nameField, 70f);
        var hostButton = CreateChoiceButton(lobby, "HostButton", "Sitzung erstellen");
        SetHeight(hostButton, 70f);
        var codeField = CreateInputField(lobby, "CodeField");
        ((TextMeshProUGUI)codeField.placeholder).text = "Sitzungs-Code...";
        SetHeight(codeField, 70f);
        var joinButton = CreateChoiceButton(lobby, "JoinButton", "Beitreten");
        SetHeight(joinButton, 70f);

        var session = CreateGroup(canvasRT, "SessionGroup", 420f);
        var code = CreateText(session, "Code", "", 48f, TextAlignmentOptions.Center);
        code.color = accentColor;
        SetHeight(code, 60f);
        var players = CreateText(session, "Players", "", 24f, TextAlignmentOptions.TopLeft);
        players.textWrappingMode = TextWrappingModes.Normal;
        SetHeight(players, 190f);
        var muteButton = CreateChoiceButton(session, "MuteButton", "Mikrofon: an");
        SetHeight(muteButton, 70f);
        var leaveButton = CreateChoiceButton(session, "LeaveButton", "Verlassen");
        SetHeight(leaveButton, 70f);
        session.gameObject.SetActive(false);

        var panel = go.AddComponent<SessionPanel>();
        panel.nameField = nameField;
        panel.codeField = codeField;
        panel.hostButton = hostButton;
        panel.joinButton = joinButton;
        panel.leaveButton = leaveButton;
        panel.muteButton = muteButton;
        panel.statusText = status;
        panel.codeText = code;
        panel.playersText = players;
        panel.muteLabel = muteButton.GetComponentInChildren<TextMeshProUGUI>();
        panel.lobbyGroup = lobby.gameObject;
        panel.sessionGroup = session.gameObject;

        var asset = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/SessionPanel.prefab");
        Object.DestroyImmediate(go);
        return asset;
    }

    static RectTransform CreateGroup(RectTransform parent, string groupName, float height)
    {
        var go = new GameObject(groupName, typeof(RectTransform), typeof(VerticalLayoutGroup));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);

        var group = go.GetComponent<VerticalLayoutGroup>();
        group.spacing = 10f;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;

        SetHeight(rt, height);
        return rt;
    }
}
