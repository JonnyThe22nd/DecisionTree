using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static partial class DecisionTreeSetup
{
    const string PrefabFolder = "Assets/prefabs";

    static readonly Color panelColor = new Color(0.12f, 0.13f, 0.16f, 0.97f);
    static readonly Color hintColor = new Color(1f, 1f, 1f, 0.6f);
    static readonly Color accentColor = new Color(1f, 0.8f, 0.25f, 1f);

    static TMP_DefaultControls.Resources tmpResources;
    static DefaultControls.Resources uiResources;

    [MenuItem("DecisionTree/Prefabs neu erzeugen (überschreibt)")]
    public static void BuildPrefabs()
    {
        LoadResources();

        EnsureHoverEffect("Button", 1.08f);
        EnsureHoverEffect("NewChildButton", 1.3f);
        EnsureHoverEffect("SoucreButton", 1.3f);
        EnsureHoverEffect("SourceSelectButton", 1.03f);

        var listItem = LoadPrefabComponent<Button>("SourceSelectButton");

        var modelButton = BuildModelButton();
        var resetViewButton = BuildResetViewButton();
        var anchorSprite = CreateAnchorSprite();
        var namingPopup = BuildNamingPopup();
        var decisionPopup = BuildDecisionPopup();
        var sourcePicker = BuildFilePicker("SourcePickerPopup", "Quelle auswählen", listItem);
        var modelPicker = BuildFilePicker("ModelPickerPopup", "3D-Modell auswählen", listItem);
        var sourcesPanel = BuildSourcesPanel(listItem);
        BuildTreeCanvas(modelButton, resetViewButton, anchorSprite, namingPopup, decisionPopup, sourcePicker, modelPicker, sourcesPanel);

        AssetDatabase.SaveAssets();
        Debug.Log("DecisionTreeSetup: Prefabs in " + PrefabFolder + " erzeugt.");
    }

    static void LoadResources()
    {
        var standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        var background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        var inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
        var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        var mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");

        tmpResources = new TMP_DefaultControls.Resources
        {
            standard = standard, background = background, inputField = inputField, knob = knob, mask = mask
        };
        uiResources = new DefaultControls.Resources
        {
            standard = standard, background = background, inputField = inputField, knob = knob, mask = mask
        };
    }

    static T LoadPrefabComponent<T>(string prefabName) where T : Component
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{prefabName}.prefab");
        if (prefab == null)
        {
            Debug.LogError($"DecisionTreeSetup: {PrefabFolder}/{prefabName}.prefab fehlt.");
            return null;
        }
        return prefab.GetComponent<T>();
    }

    static void EnsureHoverEffect(string prefabName, float hoverScale)
    {
        string path = $"{PrefabFolder}/{prefabName}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            Debug.LogError($"DecisionTreeSetup: {path} fehlt.");
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(path);
        var rootButton = root.GetComponent<Button>();
        if (rootButton != null) rootButton.transition = Selectable.Transition.None;
        var effect = root.GetComponent<UIHoverEffect>();
        if (effect == null) effect = root.AddComponent<UIHoverEffect>();
        effect.hoverScale = hoverScale;
        effect.hoverColor = accentColor;
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static T SavePrefab<T>(GameObject go) where T : Component
    {
        var asset = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabFolder}/{go.name}.prefab");
        Object.DestroyImmediate(go);
        return asset.GetComponent<T>();
    }

    static Button BuildModelButton()
    {
        var go = TMP_DefaultControls.CreateButton(tmpResources);
        go.name = "ModelButton";
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(40f, 40f);

        var text = go.GetComponentInChildren<TextMeshProUGUI>();
        text.text = "M";
        text.fontSize = 24f;

        go.GetComponent<Button>().transition = Selectable.Transition.None;
        var hover = go.AddComponent<UIHoverEffect>();
        hover.hoverScale = 1.3f;
        hover.hoverColor = accentColor;

        return SavePrefab<Button>(go);
    }

    static Button BuildResetViewButton()
    {
        var go = TMP_DefaultControls.CreateButton(tmpResources);
        go.name = "ResetViewButton";
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(140f, 40f);

        var text = go.GetComponentInChildren<TextMeshProUGUI>();
        text.text = "Standard";
        text.fontSize = 22f;

        go.GetComponent<Button>().transition = Selectable.Transition.None;
        var hover = go.AddComponent<UIHoverEffect>();
        hover.hoverScale = 1.15f;
        hover.hoverColor = accentColor;

        return SavePrefab<Button>(go);
    }

    static Sprite CreateAnchorSprite()
    {
        const int size = 128;
        string path = PrefabFolder + "/Anchor.png";

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = AnchorCoverage(x + 0.5f, y + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static float AnchorCoverage(float px, float py)
    {
        var p = new Vector2(px, py);
        float coverage = 0f;

        float ring = Mathf.Abs(Vector2.Distance(p, new Vector2(64f, 108f)) - 11.5f);
        coverage = Mathf.Max(coverage, Mathf.Clamp01(4.5f - ring));

        coverage = Mathf.Max(coverage, SegmentCoverage(p, new Vector2(64f, 97f), new Vector2(64f, 22f), 4f));
        coverage = Mathf.Max(coverage, SegmentCoverage(p, new Vector2(42f, 84f), new Vector2(86f, 84f), 3.5f));
        coverage = Mathf.Max(coverage, SegmentCoverage(p, new Vector2(26f, 56f), new Vector2(14f, 72f), 4f));
        coverage = Mathf.Max(coverage, SegmentCoverage(p, new Vector2(102f, 56f), new Vector2(114f, 72f), 4f));

        Vector2 fromCenter = p - new Vector2(64f, 56f);
        if (fromCenter.y <= 4f)
        {
            float arc = Mathf.Abs(fromCenter.magnitude - 38f);
            coverage = Mathf.Max(coverage, Mathf.Clamp01(4.5f - arc));
        }

        return coverage;
    }

    static float SegmentCoverage(Vector2 p, Vector2 a, Vector2 b, float halfWidth)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        float distance = Vector2.Distance(p, a + ab * t);
        return Mathf.Clamp01(halfWidth + 0.5f - distance);
    }

    static NamingPopup BuildNamingPopup()
    {
        var panel = CreatePanel(null, "NamingPopup", new Vector2(640f, 260f));

        var input = CreateInputField(panel, "InputField");
        Place(input.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(560f, 80f));

        var confirm = CreateButton(panel, "ConfirmButton", "OK", new Vector2(260f, 70f));
        Place(confirm.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(150f, 35f), new Vector2(260f, 70f), new Vector2(0.5f, 0f));

        var cancel = CreateButton(panel, "CancelButton", "Abbrechen", new Vector2(260f, 70f));
        Place(cancel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(-150f, 35f), new Vector2(260f, 70f), new Vector2(0.5f, 0f));

        var popup = panel.gameObject.AddComponent<NamingPopup>();
        popup.inputField = input;
        popup.confirmButton = confirm;
        popup.cancelButton = cancel;
        popup.placeholderText = input.placeholder as TextMeshProUGUI;

        return SavePrefab<NamingPopup>(panel.gameObject);
    }

    static DecisionPopup BuildDecisionPopup()
    {
        var panel = CreatePanel(null, "DecisionPopup", new Vector2(880f, 700f));

        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 20, 20);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var title = CreateText(panel, "Title", "Neue Entscheidung", 36f, TextAlignmentOptions.Center);
        SetHeight(title, 50f);

        var nameField = CreateInputField(panel, "NameField");
        ((TextMeshProUGUI)nameField.placeholder).text = "Name der Entscheidung...";
        SetHeight(nameField, 70f);

        var kindLabel = CreateText(panel, "KindLabel", "Grundlage dieser Entscheidung", 24f, TextAlignmentOptions.Left);
        kindLabel.color = hintColor;
        SetHeight(kindLabel, 30f);

        var kindRow = CreateRow(panel, "KindRow", 90f);
        var kindButtons = new Button[ArgumentationInfo.Kinds.Length];
        for (int i = 0; i < kindButtons.Length; i++)
        {
            kindButtons[i] = CreateChoiceButton(kindRow, "Kind" + i, ArgumentationInfo.Name(ArgumentationInfo.Kinds[i]));
        }

        var beliefLabel = CreateText(panel, "BeliefLabel", "Sicherheit", 24f, TextAlignmentOptions.Left);
        beliefLabel.color = hintColor;
        SetHeight(beliefLabel, 30f);

        var beliefRow = CreateRow(panel, "BeliefRow", 80f);
        var beliefButtons = new Button[ArgumentationInfo.Beliefs.Length];
        for (int i = 0; i < beliefButtons.Length; i++)
        {
            string name = ArgumentationInfo.Name(ArgumentationInfo.Beliefs[i]);
            beliefButtons[i] = CreateChoiceButton(beliefRow, "Belief" + i, char.ToUpper(name[0]) + name.Substring(1));
        }

        var reasoningLabel = CreateText(panel, "ReasoningLabel", "Begründung / Methode (optional)", 24f, TextAlignmentOptions.Left);
        reasoningLabel.color = hintColor;
        SetHeight(reasoningLabel, 30f);

        var reasoningField = CreateInputField(panel, "ReasoningField");
        ((TextMeshProUGUI)reasoningField.placeholder).text = "z. B. Stratigrafie, Vergleichsfund...";
        SetHeight(reasoningField, 70f);

        var buttonRow = CreateRow(panel, "ButtonRow", 70f);
        var cancel = CreateChoiceButton(buttonRow, "CancelButton", "Abbrechen");
        var confirm = CreateChoiceButton(buttonRow, "ConfirmButton", "OK");

        var popup = panel.gameObject.AddComponent<DecisionPopup>();
        popup.nameField = nameField;
        popup.reasoningField = reasoningField;
        popup.kindButtons = kindButtons;
        popup.beliefButtons = beliefButtons;
        popup.confirmButton = confirm;
        popup.cancelButton = cancel;

        return SavePrefab<DecisionPopup>(panel.gameObject);
    }

    static FilePickerPopup BuildFilePicker(string prefabName, string titleText, Button listItem)
    {
        var panel = CreatePanel(null, prefabName, new Vector2(700f, 620f));

        var title = CreateText(panel, "Title", titleText, 34f, TextAlignmentOptions.Center);
        Stretch(title.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(20f, -70f), new Vector2(-20f, -10f));

        var list = CreateScrollList(panel, "List", 8f);
        Stretch((RectTransform)list.transform, Vector2.zero, Vector2.one, new Vector2(20f, 100f), new Vector2(-20f, -80f));

        var hint = CreateText(panel, "EmptyHint", "", 24f, TextAlignmentOptions.Center);
        hint.color = hintColor;
        Stretch(hint.rectTransform, Vector2.zero, Vector2.one, new Vector2(40f, 120f), new Vector2(-40f, -100f));

        var cancel = CreateButton(panel, "CancelButton", "Abbrechen", new Vector2(260f, 70f));
        Place(cancel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 15f), new Vector2(260f, 70f), new Vector2(0.5f, 0f));

        var popup = panel.gameObject.AddComponent<FilePickerPopup>();
        popup.titleText = title;
        popup.listContainer = list.content;
        popup.listItemPrefab = listItem;
        popup.emptyHint = hint;
        popup.cancelButton = cancel;

        return SavePrefab<FilePickerPopup>(panel.gameObject);
    }

    static SourcesPanel BuildSourcesPanel(Button listItem)
    {
        var panel = CreatePanel(null, "SourcesPanel", new Vector2(760f, 720f));

        var title = CreateText(panel, "Title", "Quellen", 34f, TextAlignmentOptions.Center);
        Stretch(title.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(20f, -70f), new Vector2(-20f, -10f));

        var info = CreateText(panel, "Info", "", 22f, TextAlignmentOptions.TopLeft);
        info.textWrappingMode = TextWrappingModes.Normal;
        Stretch(info.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(30f, -270f), new Vector2(-30f, -80f));

        var list = CreateScrollList(panel, "List", 8f);
        Stretch((RectTransform)list.transform, Vector2.zero, Vector2.one, new Vector2(20f, 100f), new Vector2(-20f, -280f));

        var hint = CreateText(panel, "EmptyHint", "Noch keine Quellen.\nÜber den Button an der Entscheidung hinzufügen.", 24f, TextAlignmentOptions.Center);
        hint.color = hintColor;
        Stretch(hint.rectTransform, Vector2.zero, Vector2.one, new Vector2(40f, 120f), new Vector2(-40f, -300f));

        var close = CreateButton(panel, "CloseButton", "Schließen", new Vector2(260f, 70f));
        Place(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 15f), new Vector2(260f, 70f), new Vector2(0.5f, 0f));

        var viewer = BuildViewer(panel);

        var sources = panel.gameObject.AddComponent<SourcesPanel>();
        sources.titleText = title;
        sources.infoText = info;
        sources.listRoot = list.gameObject;
        sources.listContainer = list.content;
        sources.sourceListItemPrefab = listItem;
        sources.emptyHint = hint;
        sources.viewer = viewer;
        sources.closeButton = close;

        return SavePrefab<SourcesPanel>(panel.gameObject);
    }

    static SourceViewer BuildViewer(RectTransform parent)
    {
        var root = CreatePanel(parent, "Viewer", Vector2.zero);
        root.GetComponent<Image>().color = new Color(panelColor.r, panelColor.g, panelColor.b, 1f);
        Stretch(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var title = CreateText(root, "Title", "Quelle", 30f, TextAlignmentOptions.Center);
        Stretch(title.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(20f, -70f), new Vector2(-20f, -10f));

        var textScroll = CreateScrollList(root, "TextView", 0f);
        Stretch((RectTransform)textScroll.transform, Vector2.zero, Vector2.one, new Vector2(20f, 100f), new Vector2(-20f, -80f));
        var contentLayout = textScroll.content.GetComponent<VerticalLayoutGroup>();
        contentLayout.childControlHeight = true;
        var body = CreateText(textScroll.content, "Body", "", 26f, TextAlignmentOptions.TopLeft);
        body.textWrappingMode = TextWrappingModes.Normal;

        var imageArea = new GameObject("ImageArea", typeof(RectTransform)).GetComponent<RectTransform>();
        imageArea.SetParent(root, false);
        Stretch(imageArea, Vector2.zero, Vector2.one, new Vector2(20f, 100f), new Vector2(-20f, -80f));

        var image = new GameObject("Image", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        image.transform.SetParent(imageArea, false);
        image.raycastTarget = false;
        Place(image.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f));

        var back = CreateButton(root, "BackButton", "Zurück", new Vector2(260f, 70f));
        Place(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 15f), new Vector2(260f, 70f), new Vector2(0.5f, 0f));

        var viewer = root.gameObject.AddComponent<SourceViewer>();
        viewer.titleText = title;
        viewer.textRoot = textScroll.gameObject;
        viewer.bodyText = body;
        viewer.imageArea = imageArea;
        viewer.image = image;
        viewer.closeButton = back;

        root.gameObject.SetActive(false);
        return viewer;
    }

    static void BuildTreeCanvas(Button modelButton, Button resetViewButton, Sprite anchorSprite, NamingPopup namingPopup, DecisionPopup decisionPopup,
        FilePickerPopup sourcePicker, FilePickerPopup modelPicker, SourcesPanel sourcesPanel)
    {
        var go = new GameObject("TreeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
        go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        go.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 4f;

        var canvasRT = go.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(1200f, 800f);
        canvasRT.localScale = Vector3.one * 0.001f;

        var content = new GameObject("TreeContent", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(canvasRT, false);
        Place(content, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        var createButton = CreateButton(canvasRT, "CreatePromptButton", "+ Baum erstellen", new Vector2(380f, 100f));
        Place(createButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 100f));

        var anchor = BuildAnchor(canvasRT, anchorSprite);

        var overlay = new GameObject("Overlay", typeof(RectTransform)).GetComponent<RectTransform>();
        overlay.SetParent(canvasRT, false);
        Stretch(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var pin = new GameObject("PinView", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        pin.SetParent(canvasRT, false);
        Place(pin, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(90f, 90f), new Vector2(0.5f, 0f));
        var pinImage = pin.GetComponent<Image>();
        pinImage.sprite = uiResources.knob;
        pinImage.color = accentColor;
        pinImage.raycastTarget = false;
        pin.gameObject.SetActive(false);

        var nameView = CreatePanel(canvasRT, "NameView", Vector2.zero);
        nameView.GetComponent<Image>().raycastTarget = false;
        Place(nameView, new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0f));
        var nameLayout = nameView.gameObject.AddComponent<HorizontalLayoutGroup>();
        nameLayout.padding = new RectOffset(30, 30, 14, 14);
        var nameFitter = nameView.gameObject.AddComponent<ContentSizeFitter>();
        nameFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        nameFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var nameLabel = CreateText(nameView, "Label", "Baum", 72f, TextAlignmentOptions.Center);
        nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
        nameView.gameObject.SetActive(false);

        overlay.SetAsLastSibling();

        var builder = go.AddComponent<TreeMenuBuilder>();
        builder.pinView = pin;
        builder.nameView = nameView;
        builder.nameLabel = nameLabel;
        builder.anchorHandle = anchor;
        builder.canvasRoot = canvasRT;
        builder.content = content;
        builder.overlay = overlay;
        builder.nodePrefab = LoadPrefabComponent<Button>("Button");
        builder.linePrefab = LoadPrefabComponent<Image>("ConnectingLine");
        builder.plusButtonPrefab = LoadPrefabComponent<Button>("NewChildButton");
        builder.addSourceButtonPrefab = LoadPrefabComponent<Button>("SoucreButton");
        builder.modelButtonPrefab = modelButton;
        builder.resetViewButtonPrefab = resetViewButton;
        builder.namingPopupPrefab = namingPopup;
        builder.decisionPopupPrefab = decisionPopup;
        builder.sourcePickerPrefab = sourcePicker;
        builder.modelPickerPrefab = modelPicker;
        builder.sourcesPanelPrefab = sourcesPanel;
        builder.createPromptButton = createButton.gameObject;
        builder.layout.horizontalSpacing = 380f;

        var follow = go.AddComponent<WorldSpaceCanvasFollow>();
        follow.faceCamera = true;

        var presenter = go.AddComponent<PathModelPresenter>();
        presenter.builder = builder;

        SavePrefab<TreeMenuBuilder>(go);
    }

    static RectTransform BuildAnchor(RectTransform parent, Sprite anchorSprite)
    {
        var go = new GameObject("AnchorHandle", typeof(RectTransform), typeof(Image), typeof(Button), typeof(AnchorHandle));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        Place(rt, Vector2.zero, new Vector2(30f, 30f), new Vector2(100f, 100f), Vector2.zero);

        var background = go.GetComponent<Image>();
        background.sprite = uiResources.knob;
        background.color = Color.white;
        go.GetComponent<Button>().targetGraphic = background;

        var hover = go.AddComponent<UIHoverEffect>();
        hover.hoverScale = 1.15f;
        hover.hoverColor = accentColor;

        var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        icon.SetParent(rt, false);
        Stretch(icon, Vector2.zero, Vector2.one, new Vector2(16f, 16f), new Vector2(-16f, -16f));
        var iconImage = icon.GetComponent<Image>();
        iconImage.sprite = anchorSprite;
        iconImage.color = new Color(0.1f, 0.1f, 0.15f, 1f);
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        go.SetActive(false);
        return rt;
    }

    static RectTransform CreatePanel(RectTransform parent, string name, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        if (parent != null) rt.SetParent(parent, false);
        rt.sizeDelta = size;

        go.GetComponent<Image>().color = panelColor;
        return rt;
    }

    static TextMeshProUGUI CreateText(RectTransform parent, string name, string text, float fontSize, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    static Button CreateButton(RectTransform parent, string name, string label, Vector2 size)
    {
        var go = TMP_DefaultControls.CreateButton(tmpResources);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = size;

        var text = go.GetComponentInChildren<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 28f;

        go.GetComponent<Button>().transition = Selectable.Transition.None;
        var hover = go.AddComponent<UIHoverEffect>();
        hover.hoverScale = 1.08f;
        hover.hoverColor = accentColor;
        return go.GetComponent<Button>();
    }

    static Button CreateChoiceButton(RectTransform parent, string name, string label)
    {
        var button = CreateButton(parent, name, label, new Vector2(100f, 60f));
        var text = button.GetComponentInChildren<TextMeshProUGUI>();
        text.enableAutoSizing = true;
        text.fontSizeMin = 14f;
        text.fontSizeMax = 24f;
        text.textWrappingMode = TextWrappingModes.Normal;
        return button;
    }

    static RectTransform CreateRow(RectTransform parent, string name, float height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);

        var row = go.GetComponent<HorizontalLayoutGroup>();
        row.spacing = 10f;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = true;
        row.childForceExpandHeight = true;

        SetHeight(rt, height);
        return rt;
    }

    static void SetHeight(Component target, float height)
    {
        var element = target.GetComponent<LayoutElement>();
        if (element == null) element = target.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = height;
        element.minHeight = height;
    }

    static TMP_InputField CreateInputField(RectTransform parent, string name)
    {
        var go = TMP_DefaultControls.CreateInputField(tmpResources);
        go.name = name;
        go.transform.SetParent(parent, false);

        var input = go.GetComponent<TMP_InputField>();
        input.pointSize = 32f;
        foreach (var text in go.GetComponentsInChildren<TextMeshProUGUI>(true))
            text.fontSize = 32f;
        return input;
    }

    static ScrollRect CreateScrollList(RectTransform parent, string name, float spacing)
    {
        var go = DefaultControls.CreateScrollView(uiResources);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);

        var scroll = go.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        Object.DestroyImmediate(scroll.horizontalScrollbar.gameObject);
        scroll.horizontalScrollbar = null;
        scroll.scrollSensitivity = 30f;

        var layout = scroll.content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        var fitter = scroll.content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return scroll;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Place(rt, anchor, position, size, anchor);
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
    }

    static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }
}
