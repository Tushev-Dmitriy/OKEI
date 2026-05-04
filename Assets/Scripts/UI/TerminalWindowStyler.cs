using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public static class TerminalWindowStyler
{
    private static readonly Color PanelColor = new Color(0.04f, 0.055f, 0.075f, 0.94f);
    private static readonly Color HeaderColor = new Color(0.02f, 0.15f, 0.18f, 0.96f);
    private static readonly Color AccentColor = new Color(0.0f, 0.88f, 0.78f, 1f);
    private static readonly Color FillColor = new Color(0.13f, 0.75f, 0.94f, 1f);
    private static readonly Color TextColor = new Color(0.86f, 0.97f, 1f, 1f);
    private static readonly Color MutedTextColor = new Color(0.42f, 0.63f, 0.68f, 1f);
    private static readonly Vector2 FixedAnchoredPosition = new Vector2(-96f, 0f);
    private static readonly Vector2 FixedSize = new Vector2(520f, 260f);

    public static void Apply(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(1f, 0.5f);
        rectTransform.anchorMax = new Vector2(1f, 0.5f);
        rectTransform.pivot = new Vector2(1f, 0.5f);
        rectTransform.anchoredPosition = FixedAnchoredPosition;
        rectTransform.sizeDelta = FixedSize;

        Image background = EnsureImage(panel);
        background.color = PanelColor;
        background.raycastTarget = true;

        EnsureFrame(panel.transform);
        StyleTexts(panel);
        StyleSliders(panel);
    }

    private static void EnsureFrame(Transform panel)
    {
        RectTransform header = EnsureChildRect(panel, "TerminalHeader");
        if (header == null)
        {
            return;
        }

        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.anchoredPosition = Vector2.zero;
        header.sizeDelta = new Vector2(0f, 46f);
        header.SetAsFirstSibling();
        EnsureImage(header.gameObject).color = HeaderColor;

        RectTransform accent = EnsureChildRect(panel, "TerminalAccent");
        if (accent == null)
        {
            return;
        }

        accent.anchorMin = new Vector2(0f, 0f);
        accent.anchorMax = new Vector2(0f, 1f);
        accent.pivot = new Vector2(0f, 0.5f);
        accent.anchoredPosition = Vector2.zero;
        accent.sizeDelta = new Vector2(5f, 0f);
        accent.SetAsFirstSibling();
        EnsureImage(accent.gameObject).color = AccentColor;

        RectTransform scanline = EnsureChildRect(panel, "TerminalScanline");
        if (scanline == null)
        {
            return;
        }

        scanline.anchorMin = new Vector2(0f, 0f);
        scanline.anchorMax = new Vector2(1f, 0f);
        scanline.pivot = new Vector2(0.5f, 0f);
        scanline.anchoredPosition = new Vector2(0f, 18f);
        scanline.sizeDelta = new Vector2(0f, 2f);
        EnsureImage(scanline.gameObject).color = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.36f);

        RectTransform label = EnsureChildRect(header, "TerminalHeaderLabel");
        if (label == null)
        {
            return;
        }

        label.anchorMin = new Vector2(0f, 0f);
        label.anchorMax = new Vector2(1f, 1f);
        label.offsetMin = new Vector2(20f, 0f);
        label.offsetMax = new Vector2(-20f, 0f);

        RemoveImage(label.gameObject);
        Text labelText = label.GetComponent<Text>();
        if (labelText == null)
        {
            labelText = label.gameObject.AddComponent<Text>();
        }

        if (labelText == null)
        {
            return;
        }

        labelText.text = "PARAMETER TERMINAL";
        labelText.font = GetBuiltinFont();
        labelText.fontSize = 15;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.color = MutedTextColor;
        labelText.raycastTarget = false;
    }

    private static void StyleTexts(GameObject panel)
    {
        foreach (Text text in panel.GetComponentsInChildren<Text>(true))
        {
            if (text.name == "TerminalHeaderLabel")
            {
                continue;
            }

            text.font = GetBuiltinFont();
            text.color = TextColor;
            text.resizeTextForBestFit = false;

            if (text.name == "Title")
            {
                text.fontSize = 24;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleLeft;

                RectTransform rectTransform = text.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                rectTransform.anchoredPosition = new Vector2(20f, -68f);
                rectTransform.sizeDelta = new Vector2(-40f, 36f);
                continue;
            }

            if (text.name == "Value")
            {
                text.fontSize = 22;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleRight;
            }
        }
    }

    private static void StyleSliders(GameObject panel)
    {
        foreach (Slider slider in panel.GetComponentsInChildren<Slider>(true))
        {
            RectTransform rectTransform = slider.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rectTransform.anchorMax = new Vector2(1f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, -38f);
            rectTransform.sizeDelta = new Vector2(-72f, 44f);

            if (slider.targetGraphic != null)
            {
                slider.targetGraphic.color = AccentColor;
            }

            StyleSliderImage(slider.transform, "Background", new Color(0.12f, 0.18f, 0.21f, 1f));
            StyleSliderImage(slider.transform, "Fill", FillColor);
            StyleSliderImage(slider.transform, "Handle", AccentColor);
        }
    }

    private static void StyleSliderImage(Transform root, string namePart, Color color)
    {
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.name.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                image.color = color;
            }
        }
    }

    private static RectTransform EnsureChildRect(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null && existing.TryGetComponent(out RectTransform existingRect))
        {
            return existingRect;
        }

        GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private static Image EnsureImage(GameObject target)
    {
        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            image = target.AddComponent<Image>();
        }

        return image;
    }

    private static void RemoveImage(GameObject target)
    {
        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(image);
        }
        else
        {
            Object.DestroyImmediate(image);
        }
    }

    private static Font GetBuiltinFont()
    {
        try
        {
            Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (legacyFont != null)
            {
                return legacyFont;
            }
        }
        catch (System.ArgumentException)
        {
        }

        return Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault();
    }
}
