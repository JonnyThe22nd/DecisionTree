using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class DecisionTreeSetup
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

        var namingPopup = BuildNamingPopup();
        var sourcePicker = BuildSourcePicker(listItem);
        var sourcesPanel = BuildSourcesPanel(listItem);
        BuildTreeCanvas(namingPopup, sourcePicker, sourcesPanel);

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

    static SourcePickerPopup BuildSourcePicker(Button listItem)
    {
        var panel = CreatePanel(null, "SourcePickerPopup", new Vector2(700f, 620f));

        var title = CreateText(panel, "Title", "Quelle auswählen", 34f, TextAlignmentOptions.Center);
        Stretch(title.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(20f, -70f), new Vector2(-20f, -10f));

        var list = CreateScrollList(panel, "List", 8f);
        Stretch((RectTransform)list.transform, Vector2.zero, Vector2.one, new Vector2(20f, 100f), new Vector2(-20f, -80f));

        var hint = CreateText(panel, "EmptyHint", "", 24f, TextAlignmentOptions.Center);
        hint.color = hintColor;
        Stretch(hint.rectTransform, Vector2.zero, Vector2.one, new Vector2(40f, 120f), new Vector2(-40f, -100f));

        var cancel = CreateButton(panel, "CancelButton", "Abbrechen", new Vector2(260f, 70f));
        Place(cancel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 15f), new Vector2(260f, 70f), new Vector2(0.5f, 0f));

        var popup = panel.gameObject.AddComponent<SourcePickerPopup>();
        popup.listContainer = list.content;
        popup.listItemPrefab = listItem;
        popup.emptyHint = hint;
        popup.cancelButton = cancel;

        return SavePrefab<SourcePickerPopup>(panel.gameObject);
    }

    static SourcesPanel BuildSourcesPanel(Button listItem)
    {
        var panel = CreatePanel(null, "SourcesPanel", new Vector2(760f, 660f));

        var title = CreateText(panel, "Title", "Quellen", 34f, TextAlignmentOptions.Center);
        Stretch(title.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(20f, -70f), new Vector2(-20f, -10f));

        var list = CreateScrollList(panel, "List", 8f);
        Stretch((RectTransform)list.transform, Vector2.zero, Vector2.one, new Vector2(20f, 100f), new Vector2(-20f, -80f));

        var hint = CreateText(panel, "EmptyHint", "Noch keine Quellen.\nÜber den Button an der Entscheidung hinzufügen.", 24f, TextAlignmentOptions.Center);
        hint.color = hintColor;
        Stretch(hint.rectTransform, Vector2.zero, Vector2.one, new Vector2(40f, 120f), new Vector2(-40f, -100f));

        var close = CreateButton(panel, "CloseButton", "Schließen", new Vector2(260f, 70f));
        Place(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 15f), new Vector2(260f, 70f), new Vector2(0.5f, 0f));

        var viewer = BuildViewer(panel);

        var sources = panel.gameObject.AddComponent<SourcesPanel>();
        sources.titleText = title;
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

    static void BuildTreeCanvas(NamingPopup namingPopup, SourcePickerPopup sourcePicker, SourcesPanel sourcesPanel)
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
        builder.canvasRoot = canvasRT;
        builder.content = content;
        builder.overlay = overlay;
        builder.nodePrefab = LoadPrefabComponent<Button>("Button");
        builder.linePrefab = LoadPrefabComponent<Image>("ConnectingLine");
        builder.plusButtonPrefab = LoadPrefabComponent<Button>("NewChildButton");
        builder.addSourceButtonPrefab = LoadPrefabComponent<Button>("SoucreButton");
        builder.namingPopupPrefab = namingPopup;
        builder.sourcePickerPrefab = sourcePicker;
        builder.sourcesPanelPrefab = sourcesPanel;
        builder.createPromptButton = createButton.gameObject;
        builder.layout.horizontalSpacing = 380f;

        var follow = go.AddComponent<WorldSpaceCanvasFollow>();
        follow.faceCamera = true;

        SavePrefab<TreeMenuBuilder>(go);
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

        var hover = go.AddComponent<UIHoverEffect>();
        hover.hoverScale = 1.08f;
        hover.hoverColor = accentColor;
        return go.GetComponent<Button>();
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
