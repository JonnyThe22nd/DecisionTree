using UnityEngine;
using UnityEngine.UI;

public class SourceViewer : MonoBehaviour
{
    public TMPro.TextMeshProUGUI titleText;

    [Header("Text")]
    public GameObject textRoot;
    public TMPro.TextMeshProUGUI bodyText;

    [Header("Bild")]
    public RectTransform imageArea;
    public RawImage image;

    public Button closeButton;

    Texture2D loadedTexture;

    void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    public void Show(SourceEntry source)
    {
        gameObject.SetActive(true);
        ReleaseTexture();

        if (titleText != null) titleText.text = source.Title;

        bool isImage = source.IsImage;
        if (textRoot != null) textRoot.SetActive(!isImage);
        if (imageArea != null) imageArea.gameObject.SetActive(isImage);

        if (isImage) ShowImage(source);
        else ShowText(source);
    }

    public void Hide()
    {
        ReleaseTexture();
        gameObject.SetActive(false);
    }

    void ShowText(SourceEntry source)
    {
        if (bodyText == null) return;
        string text = SourceLibrary.LoadText(source.FilePath);
        bodyText.text = text ?? "Datei konnte nicht gelesen werden:\n" + source.FilePath;
    }

    void ShowImage(SourceEntry source)
    {
        if (image == null || imageArea == null) return;

        loadedTexture = SourceLibrary.LoadImage(source.FilePath);
        image.texture = loadedTexture;
        image.enabled = loadedTexture != null;
        if (loadedTexture == null) return;

        Vector2 area = imageArea.rect.size;
        float scale = Mathf.Min(area.x / loadedTexture.width, area.y / loadedTexture.height);
        image.rectTransform.sizeDelta = new Vector2(loadedTexture.width, loadedTexture.height) * scale;
    }

    void ReleaseTexture()
    {
        if (image != null) image.texture = null;
        if (loadedTexture != null) Destroy(loadedTexture);
        loadedTexture = null;
    }

    void OnDestroy()
    {
        ReleaseTexture();
    }
}
