#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BootstrapMenuSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Bootstrap.unity";

    private const string BackgroundPath = "Assets/UI/ChatGPT Image 23 мар. 2026 г., 23_40_31.png";
    private const string AtlasPath = "Assets/UI/ChatGPT Image 23 мар. 2026 г., 23_47_32.png";
    private const string LockIconPath = "Assets/OtherAssets/Devion Games/Flat GUI/Icons/Locked.png";
    private const string CompletedIconPath = "Assets/OtherAssets/Devion Games/Flat GUI/Icons/Ok.png";

    private const string LargePanelSpriteName = "ChatGPT Image 23 мар. 2026 г., 23_47_32_0";
    private const string LevelOneCardSpriteName = "ChatGPT Image 23 мар. 2026 г., 23_47_32_15";
    private const string LevelTwoCardSpriteName = "ChatGPT Image 23 мар. 2026 г., 23_47_32_3";
    private const string GenericCardSpriteName = "ChatGPT Image 23 мар. 2026 г., 23_47_32_20";
    private const string LockedCardSpriteName = "ChatGPT Image 23 мар. 2026 г., 23_47_32_32";
    private const string RowPanelSpriteName = "ChatGPT Image 23 мар. 2026 г., 23_47_32_17";
    private const string RowPanelAltSpriteName = "ChatGPT Image 23 мар. 2026 г., 23_47_32_21";
    private const string WideDarkButtonSpriteName = "ChatGPT Image 23 мар. 2026 г., 23_47_32_57";
    private const string WideAccentButtonSpriteName = "ChatGPT Image 23 мар. 2026 г., 23_47_32_71";
    private const string BackButtonSpriteName = "ChatGPT Image 23 мар. 2026 г., 23_47_32_44";
    private const string CardButtonSpriteName = "ChatGPT Image 23 мар. 2026 г., 23_47_32_42";
    private const string DropdownWideSpriteName = "ChatGPT Image 23 мар. 2026 г., 23_47_32_79";
    private const string DropdownSpriteName = "ChatGPT Image 23 мар. 2026 г., 23_47_32_82";
    private const string SliderTrackSpriteName = "ChatGPT Image 23 мар. 2026 г., 23_47_32_61";
    private const string SliderFillSpriteName = "ChatGPT Image 23 мар. 2026 г., 23_47_32_70";
    private const string SliderHandleSpriteName = "ChatGPT Image 23 мар. 2026 г., 23_47_32_80";
    private const string ToggleOnSpriteName = "ChatGPT Image 23 мар. 2026 г., 23_47_32_56";
    private const string ToggleOffSpriteName = "ChatGPT Image 23 мар. 2026 г., 23_47_32_62";
    private const string GearIconSpriteName = "ChatGPT Image 23 мар. 2026 г., 23_47_32_7";
    private const string SlidersIconSpriteName = "ChatGPT Image 23 мар. 2026 г., 23_47_32_10";

    private static readonly Color PanelTint = new Color(1f, 1f, 1f, 0.95f);
    private static readonly Color SubtlePanelTint = new Color(1f, 1f, 1f, 0.22f);
    private static readonly Color DimOverlayColor = new Color(0f, 0f, 0f, 0.38f);
    private static readonly Color PrimaryTextColor = new Color(0.96f, 0.94f, 0.90f, 1f);
    private static readonly Color SecondaryTextColor = new Color(0.84f, 0.81f, 0.76f, 1f);

    private sealed class AtlasSprites
    {
        public Sprite background;
        public Sprite largePanel;
        public Sprite levelOneCard;
        public Sprite levelTwoCard;
        public Sprite genericCard;
        public Sprite lockedCard;
        public Sprite rowPanel;
        public Sprite rowPanelAlt;
        public Sprite wideDarkButton;
        public Sprite wideAccentButton;
        public Sprite backButton;
        public Sprite cardButton;
        public Sprite dropdownWide;
        public Sprite dropdown;
        public Sprite sliderTrack;
        public Sprite sliderFill;
        public Sprite sliderHandle;
        public Sprite toggleOn;
        public Sprite toggleOff;
        public Sprite gearIcon;
        public Sprite slidersIcon;
        public Sprite lockIcon;
        public Sprite completedIcon;
    }

    [MenuItem("Tools/OKEI/Rebuild Bootstrap Menu")]
    public static void RebuildBootstrapMenu()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        BuildSceneInternal(scene, null, true);
    }

    public static bool HasExpectedHierarchy(Scene scene)
    {
        if (!scene.IsValid())
        {
            return false;
        }

        GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == "BootstrapUI");
        if (root == null)
        {
            return false;
        }

        Transform canvas = root.transform.Find("Canvas");
        if (canvas == null)
        {
            return false;
        }

        if (root.transform.Find("MenuController") == null)
        {
            return false;
        }

        return canvas.Find("Background") != null
            && canvas.Find("DimOverlay") != null
            && canvas.Find("MainMenuPanel") != null
            && canvas.Find("LevelSelectPanel") != null
            && canvas.Find("SettingsPanel") != null
            && canvas.Find("EventSystem") != null;
    }

    public static void BuildCurrentScene(GameObject bootstrapSource, bool saveScene)
    {
        if (bootstrapSource == null)
        {
            return;
        }

        BuildSceneInternal(bootstrapSource.scene, bootstrapSource, saveScene);
    }

    private static void BuildSceneInternal(Scene scene, GameObject bootstrapSource, bool saveScene)
    {
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            return;
        }

        AtlasSprites sprites = LoadSprites();
        TMP_FontAsset fontAsset = ResolveFontAsset();
        EventSystem eventSystem = ResolveEventSystem(scene, bootstrapSource);
        BootstrapMenuSceneBootstrapper bootstrapper = EnsureBootstrapper(eventSystem.gameObject);

        if (eventSystem.transform.parent != null)
        {
            eventSystem.transform.SetParent(null, false);
        }

        ClearExistingUi(scene, eventSystem.gameObject);

        GameObject root = new GameObject("BootstrapUI");
        SceneManager.MoveGameObjectToScene(root, scene);

        GameObject controllerRoot = new GameObject("MenuController");
        controllerRoot.transform.SetParent(root.transform, false);

        LevelProgressManager progressManager = controllerRoot.AddComponent<LevelProgressManager>();
        MainMenuController mainMenuController = controllerRoot.AddComponent<MainMenuController>();
        LevelSelectPanelController levelSelectController = controllerRoot.AddComponent<LevelSelectPanelController>();
        SettingsPanelController settingsPanelController = controllerRoot.AddComponent<SettingsPanelController>();

        Canvas canvas = CreateCanvas(root.transform);
        RectTransform canvasTransform = (RectTransform)canvas.transform;

        Image background = CreateStretchImage("Background", canvasTransform, sprites.background, Color.white);
        background.raycastTarget = false;

        Image dimOverlay = CreateStretchImage("DimOverlay", canvasTransform, null, DimOverlayColor);
        dimOverlay.raycastTarget = false;

        CanvasGroup mainMenuPanel;
        RectTransform mainMenuPanelTransform = CreateFullScreenPanel("MainMenuPanel", canvasTransform, true, out mainMenuPanel);
        CanvasGroup levelSelectPanel;
        RectTransform levelSelectPanelTransform = CreateFullScreenPanel("LevelSelectPanel", canvasTransform, false, out levelSelectPanel);
        CanvasGroup settingsPanel;
        RectTransform settingsPanelTransform = CreateFullScreenPanel("SettingsPanel", canvasTransform, false, out settingsPanel);

        Button continueButton;
        Button levelSelectButton;
        Button settingsButton;
        Button exitButton;
        TMP_Text titleText;
        TMP_Text subtitleText;
        BuildMainMenu(mainMenuPanelTransform, sprites, fontAsset, out titleText, out subtitleText, out continueButton, out levelSelectButton, out settingsButton, out exitButton);

        LevelCardUI[] levelCards;
        BuildLevelSelect(levelSelectPanelTransform, sprites, fontAsset, out levelCards, out Button levelBackButton);

        BuildSettings(settingsPanelTransform, sprites, fontAsset, out Slider soundSlider, out TMP_Dropdown graphicsDropdown, out Toggle fullscreenToggle, out Image fullscreenToggleGraphic, out TMP_Dropdown resolutionDropdown, out Button settingsBackButton, out TMP_Text fullscreenStateText);

        ConfigureLevelData(progressManager, sprites);

        mainMenuController.Configure(
            progressManager,
            mainMenuPanel,
            mainMenuPanelTransform,
            levelSelectPanel,
            levelSelectPanelTransform,
            settingsPanel,
            settingsPanelTransform,
            levelSelectController,
            settingsPanelController,
            continueButton,
            levelSelectButton,
            settingsButton,
            exitButton,
            titleText,
            subtitleText);

        levelSelectController.Configure(
            mainMenuController,
            progressManager,
            FindText(levelSelectPanelTransform, "HeaderText"),
            levelBackButton,
            levelCards);

        settingsPanelController.Configure(
            mainMenuController,
            FindText(settingsPanelTransform, "HeaderText"),
            settingsBackButton,
            soundSlider,
            graphicsDropdown,
            fullscreenToggle,
            fullscreenToggleGraphic,
            sprites.toggleOff,
            sprites.toggleOn,
            fullscreenStateText,
            resolutionDropdown);

        eventSystem.transform.SetParent(canvasTransform, false);
        eventSystem.transform.SetAsLastSibling();

        if (bootstrapper != null)
        {
            EditorUtility.SetDirty(bootstrapper);
        }

        mainMenuController.ShowMainMenuImmediate();
        ApplyInitialCardPreview(progressManager, levelCards, mainMenuController);

        EditorSceneManager.MarkSceneDirty(scene);
        if (saveScene)
        {
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static AtlasSprites LoadSprites()
    {
        Dictionary<string, Sprite> atlas = AssetDatabase.LoadAllAssetsAtPath(AtlasPath)
            .OfType<Sprite>()
            .ToDictionary(sprite => sprite.name, sprite => sprite);

        return new AtlasSprites
        {
            background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath),
            largePanel = atlas[LargePanelSpriteName],
            levelOneCard = atlas[LevelOneCardSpriteName],
            levelTwoCard = atlas[LevelTwoCardSpriteName],
            genericCard = atlas[GenericCardSpriteName],
            lockedCard = atlas[LockedCardSpriteName],
            rowPanel = atlas[RowPanelSpriteName],
            rowPanelAlt = atlas[RowPanelAltSpriteName],
            wideDarkButton = atlas[WideDarkButtonSpriteName],
            wideAccentButton = atlas[WideAccentButtonSpriteName],
            backButton = atlas[BackButtonSpriteName],
            cardButton = atlas[CardButtonSpriteName],
            dropdownWide = atlas[DropdownWideSpriteName],
            dropdown = atlas[DropdownSpriteName],
            sliderTrack = atlas[SliderTrackSpriteName],
            sliderFill = atlas[SliderFillSpriteName],
            sliderHandle = atlas[SliderHandleSpriteName],
            toggleOn = atlas[ToggleOnSpriteName],
            toggleOff = atlas[ToggleOffSpriteName],
            gearIcon = atlas[GearIconSpriteName],
            slidersIcon = atlas[SlidersIconSpriteName],
            lockIcon = AssetDatabase.LoadAssetAtPath<Sprite>(LockIconPath),
            completedIcon = AssetDatabase.LoadAssetAtPath<Sprite>(CompletedIconPath)
        };
    }

    private static TMP_FontAsset ResolveFontAsset()
    {
        if (TMP_Settings.defaultFontAsset != null)
        {
            return TMP_Settings.defaultFontAsset;
        }

        string fontGuid = AssetDatabase.FindAssets("t:TMP_FontAsset").FirstOrDefault();
        return string.IsNullOrWhiteSpace(fontGuid)
            ? null
            : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fontGuid));
    }

    private static EventSystem ResolveEventSystem(Scene scene, GameObject bootstrapSource)
    {
        EventSystem existingEventSystem = bootstrapSource != null
            ? bootstrapSource.GetComponent<EventSystem>()
            : null;

        if (existingEventSystem == null)
        {
            existingEventSystem = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EventSystem>(true))
                .FirstOrDefault();
        }

        if (existingEventSystem != null)
        {
            if (existingEventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                existingEventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            return existingEventSystem;
        }

        GameObject eventSystemObject = new GameObject(
            "EventSystem",
            typeof(Transform),
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));

        SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
        return eventSystemObject.GetComponent<EventSystem>();
    }

    private static BootstrapMenuSceneBootstrapper EnsureBootstrapper(GameObject eventSystemObject)
    {
        BootstrapMenuSceneBootstrapper bootstrapper = eventSystemObject.GetComponent<BootstrapMenuSceneBootstrapper>();
        if (bootstrapper == null)
        {
            bootstrapper = eventSystemObject.AddComponent<BootstrapMenuSceneBootstrapper>();
        }

        return bootstrapper;
    }

    private static void ClearExistingUi(Scene scene, GameObject protectedObject)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == protectedObject)
            {
                continue;
            }

            if (root.name == "BootstrapUI" || root.name == "Canvas")
            {
                Object.DestroyImmediate(root);
            }
        }
    }

    private static Canvas CreateCanvas(Transform parent)
    {
        GameObject canvasObject = new GameObject(
            "Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        canvasObject.transform.SetParent(parent, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560f, 1440f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        StretchToParent((RectTransform)canvasObject.transform);
        return canvas;
    }

    private static RectTransform CreateFullScreenPanel(string name, Transform parent, bool isActive, out CanvasGroup canvasGroup)
    {
        GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        panelObject.transform.SetParent(parent, false);
        panelObject.SetActive(isActive);

        RectTransform rectTransform = (RectTransform)panelObject.transform;
        StretchToParent(rectTransform);

        canvasGroup = panelObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = isActive ? 1f : 0f;
        canvasGroup.interactable = isActive;
        canvasGroup.blocksRaycasts = isActive;

        return rectTransform;
    }

    private static void BuildMainMenu(
        RectTransform parent,
        AtlasSprites sprites,
        TMP_FontAsset fontAsset,
        out TMP_Text titleText,
        out TMP_Text subtitleText,
        out Button continueButton,
        out Button levelSelectButton,
        out Button settingsButton,
        out Button exitButton)
    {
        Image panelBackground = CreateImage("PanelBackground", parent, sprites.largePanel, SubtlePanelTint);
        SetCentered(panelBackground.rectTransform, new Vector2(920f, 980f), new Vector2(0f, -24f));
        panelBackground.raycastTarget = false;

        titleText = CreateText("TitleText", parent, "C# QUEST", fontAsset, 128, FontStyles.Bold, PrimaryTextColor, TextAlignmentOptions.Center);
        SetCentered(titleText.rectTransform, new Vector2(1000f, 140f), new Vector2(0f, 320f));

        subtitleText = CreateText("SubtitleText", parent, "обучающая игра по программированию", fontAsset, 44, FontStyles.Normal, SecondaryTextColor, TextAlignmentOptions.Center);
        SetCentered(subtitleText.rectTransform, new Vector2(980f, 70f), new Vector2(0f, 188f));

        continueButton = CreateButton("ContinueButton", parent, sprites.wideAccentButton, new Vector2(620f, 92f), new Vector2(0f, 24f), "ПРОДОЛЖИТЬ", fontAsset, true);
        levelSelectButton = CreateButton("LevelSelectButton", parent, sprites.wideDarkButton, new Vector2(620f, 92f), new Vector2(0f, -92f), "ВЫБОР УРОВНЯ", fontAsset, false);
        settingsButton = CreateButton("SettingsButton", parent, sprites.wideDarkButton, new Vector2(620f, 92f), new Vector2(0f, -208f), "НАСТРОЙКИ", fontAsset, false);
        exitButton = CreateButton("ExitButton", parent, sprites.wideDarkButton, new Vector2(620f, 92f), new Vector2(0f, -324f), "ВЫХОД", fontAsset, false);
    }

    private static void BuildLevelSelect(
        RectTransform parent,
        AtlasSprites sprites,
        TMP_FontAsset fontAsset,
        out LevelCardUI[] levelCards,
        out Button backButton)
    {
        Image panelBackground = CreateImage("PanelBackground", parent, sprites.largePanel, PanelTint);
        SetCentered(panelBackground.rectTransform, new Vector2(1520f, 930f), new Vector2(0f, -8f));
        panelBackground.raycastTarget = false;

        TMP_Text headerText = CreateText("HeaderText", parent, "ВЫБОР УРОВНЯ", fontAsset, 82, FontStyles.Bold, PrimaryTextColor, TextAlignmentOptions.Center);
        SetCentered(headerText.rectTransform, new Vector2(1100f, 96f), new Vector2(0f, 320f));

        levelCards = new LevelCardUI[4];
        levelCards[0] = CreateLevelCard(parent, "LevelCard1", sprites.levelOneCard, sprites.lockedCard, null, sprites.lockIcon, sprites.completedIcon, sprites.cardButton, fontAsset, new Vector2(-282f, 84f));
        levelCards[1] = CreateLevelCard(parent, "LevelCard2", sprites.levelTwoCard, sprites.lockedCard, null, sprites.lockIcon, sprites.completedIcon, sprites.cardButton, fontAsset, new Vector2(282f, 84f));
        levelCards[2] = CreateLevelCard(parent, "LevelCard3", sprites.genericCard, sprites.lockedCard, sprites.gearIcon, sprites.lockIcon, sprites.completedIcon, sprites.cardButton, fontAsset, new Vector2(-282f, -176f));
        levelCards[3] = CreateLevelCard(parent, "LevelCard4", sprites.genericCard, sprites.lockedCard, sprites.slidersIcon, sprites.lockIcon, sprites.completedIcon, sprites.cardButton, fontAsset, new Vector2(282f, -176f));

        backButton = CreateButton("BackButton", parent, sprites.backButton, new Vector2(360f, 86f), new Vector2(0f, -392f), "НАЗАД", fontAsset, false);
    }

    private static void BuildSettings(
        RectTransform parent,
        AtlasSprites sprites,
        TMP_FontAsset fontAsset,
        out Slider soundSlider,
        out TMP_Dropdown graphicsDropdown,
        out Toggle fullscreenToggle,
        out Image fullscreenToggleGraphic,
        out TMP_Dropdown resolutionDropdown,
        out Button backButton,
        out TMP_Text fullscreenStateText)
    {
        Image panelBackground = CreateImage("PanelBackground", parent, sprites.largePanel, PanelTint);
        SetCentered(panelBackground.rectTransform, new Vector2(1160f, 900f), new Vector2(0f, -10f));
        panelBackground.raycastTarget = false;

        TMP_Text headerText = CreateText("HeaderText", parent, "НАСТРОЙКИ", fontAsset, 78, FontStyles.Bold, PrimaryTextColor, TextAlignmentOptions.Center);
        SetCentered(headerText.rectTransform, new Vector2(900f, 96f), new Vector2(0f, 308f));

        RectTransform soundRow = CreateSettingsRow(parent, "SoundRow", sprites.rowPanel, fontAsset, "ЗВУК", new Vector2(0f, 154f), 330f);
        soundSlider = CreateSlider("SoundSlider", soundRow, new Vector2(560f, 54f), new Vector2(232f, 0f), sprites.sliderTrack, sprites.sliderFill, sprites.sliderHandle);

        RectTransform graphicsRow = CreateSettingsRow(parent, "GraphicsRow", sprites.rowPanelAlt, fontAsset, "ГРАФИКА", new Vector2(0f, 28f), 330f);
        graphicsDropdown = CreateDropdown("GraphicsDropdown", graphicsRow, new Vector2(372f, 62f), new Vector2(238f, 0f), sprites.dropdown, sprites.rowPanelAlt, sprites.rowPanel, fontAsset);

        RectTransform fullscreenRow = CreateSettingsRow(parent, "FullscreenRow", sprites.rowPanel, fontAsset, "ПОЛНЫЙ ЭКРАН", new Vector2(0f, -98f), 470f);
        fullscreenStateText = CreateText("ValueText", fullscreenRow, "On", fontAsset, 34, FontStyles.Normal, SecondaryTextColor, TextAlignmentOptions.MidlineRight);
        SetCentered(fullscreenStateText.rectTransform, new Vector2(110f, 40f), new Vector2(152f, 0f));
        fullscreenToggle = CreateToggle("FullscreenToggle", fullscreenRow, new Vector2(112f, 44f), new Vector2(308f, 0f), sprites.toggleOff, out fullscreenToggleGraphic);

        RectTransform resolutionRow = CreateSettingsRow(parent, "ResolutionRow", sprites.rowPanelAlt, fontAsset, "РАЗРЕШЕНИЕ", new Vector2(0f, -224f), 330f);
        resolutionDropdown = CreateDropdown("ResolutionDropdown", resolutionRow, new Vector2(404f, 62f), new Vector2(236f, 0f), sprites.dropdownWide, sprites.rowPanelAlt, sprites.rowPanel, fontAsset);

        backButton = CreateButton("BackButton", parent, sprites.backButton, new Vector2(340f, 84f), new Vector2(0f, -368f), "НАЗАД", fontAsset, false);
    }

    private static RectTransform CreateSettingsRow(Transform parent, string name, Sprite backgroundSprite, TMP_FontAsset fontAsset, string labelText, Vector2 anchoredPosition, float labelWidth)
    {
        Image row = CreateImage(name, parent, backgroundSprite, new Color(1f, 1f, 1f, 0.92f));
        SetCentered(row.rectTransform, new Vector2(980f, 106f), anchoredPosition);
        row.raycastTarget = false;

        TMP_Text label = CreateText("LabelText", row.rectTransform, labelText, fontAsset, 38, FontStyles.Bold, PrimaryTextColor, TextAlignmentOptions.MidlineLeft);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        SetAnchored(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(210f, 0f), new Vector2(labelWidth, 48f));

        return row.rectTransform;
    }

    private static LevelCardUI CreateLevelCard(
        Transform parent,
        string name,
        Sprite unlockedSprite,
        Sprite lockedSprite,
        Sprite previewIconSprite,
        Sprite lockIconSprite,
        Sprite completedIconSprite,
        Sprite buttonSprite,
        TMP_FontAsset fontAsset,
        Vector2 anchoredPosition)
    {
        GameObject cardObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LevelCardUI));
        cardObject.transform.SetParent(parent, false);

        RectTransform cardTransform = (RectTransform)cardObject.transform;
        SetCentered(cardTransform, new Vector2(552f, 242f), anchoredPosition);

        Image cardBackground = cardObject.GetComponent<Image>();
        cardBackground.sprite = unlockedSprite;
        cardBackground.type = Image.Type.Simple;
        cardBackground.color = Color.white;
        cardBackground.raycastTarget = false;

        TMP_Text titleText = CreateText("LevelTitleText", cardTransform, name.ToUpperInvariant(), fontAsset, 42, FontStyles.Bold, PrimaryTextColor, TextAlignmentOptions.TopLeft);
        SetAnchored(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(164f, -34f), new Vector2(270f, 56f));

        TMP_Text descriptionText = CreateText("LevelDescriptionText", cardTransform, string.Empty, fontAsset, 30, FontStyles.Normal, SecondaryTextColor, TextAlignmentOptions.TopLeft);
        SetAnchored(descriptionText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(172f, -92f), new Vector2(284f, 78f));

        Image optionalIcon = CreateImage("OptionalIcon", cardTransform, previewIconSprite, Color.white);
        SetAnchored(optionalIcon.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-92f, -80f), new Vector2(148f, 148f));
        optionalIcon.raycastTarget = false;
        optionalIcon.gameObject.SetActive(previewIconSprite != null);

        Image completedIcon = CreateImage("CompletedIcon", cardTransform, completedIconSprite, Color.white);
        SetAnchored(completedIcon.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -34f), new Vector2(54f, 54f));
        completedIcon.raycastTarget = false;
        completedIcon.gameObject.SetActive(false);

        Image lockIcon = CreateImage("LockIcon", cardTransform, lockIconSprite, Color.white);
        SetCentered(lockIcon.rectTransform, new Vector2(76f, 76f), new Vector2(0f, 10f));
        lockIcon.raycastTarget = false;
        lockIcon.gameObject.SetActive(false);

        GameObject lockedOverlay = new GameObject("LockedOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lockedOverlay.transform.SetParent(cardTransform, false);
        RectTransform lockedOverlayTransform = lockedOverlay.GetComponent<RectTransform>();
        StretchToParent(lockedOverlayTransform);
        Image lockedOverlayImage = lockedOverlay.GetComponent<Image>();
        lockedOverlayImage.color = new Color(0f, 0f, 0f, 0.42f);
        lockedOverlayImage.raycastTarget = false;
        lockedOverlay.SetActive(false);

        TMP_Text lockedOverlayText = CreateText("LockedText", lockedOverlayTransform, "ЗАБЛОКИРОВАНО", fontAsset, 28, FontStyles.Bold, SecondaryTextColor, TextAlignmentOptions.Center);
        SetCentered(lockedOverlayText.rectTransform, new Vector2(360f, 40f), new Vector2(0f, -70f));

        Button startButton = CreateButton("StartButton", cardTransform, buttonSprite, new Vector2(220f, 64f), new Vector2(0f, -78f), "НАЧАТЬ", fontAsset, true);
        TMP_Text startButtonText = FindText(startButton.transform as RectTransform, "Label");

        LevelCardUI card = cardObject.GetComponent<LevelCardUI>();
        card.ConfigureReferences(
            cardBackground,
            titleText,
            descriptionText,
            optionalIcon,
            lockIcon,
            completedIcon,
            startButton,
            startButtonText,
            lockedOverlay,
            lockedOverlayText,
            unlockedSprite,
            lockedSprite);

        return card;
    }

    private static void ConfigureLevelData(LevelProgressManager progressManager, AtlasSprites sprites)
    {
        progressManager.SetLevels(new List<LevelProgressManager.LevelMenuEntry>
        {
            new LevelProgressManager.LevelMenuEntry
            {
                sceneName = "Level1",
                additionalScenes = new List<string> { "UI" },
                displayName = "LEVEL 1",
                description = "ТИПЫ ДАННЫХ"
            },
            new LevelProgressManager.LevelMenuEntry
            {
                sceneName = "Level2",
                displayName = "LEVEL 2",
                description = "УСЛОВНЫЕ ОПЕРАТОРЫ"
            },
            new LevelProgressManager.LevelMenuEntry
            {
                sceneName = "Level3",
                additionalScenes = new List<string> { "UI" },
                displayName = "LEVEL 3",
                description = "ЦИКЛЫ",
                previewIcon = sprites.gearIcon
            },
            new LevelProgressManager.LevelMenuEntry
            {
                sceneName = "Level4",
                displayName = "LEVEL 4",
                description = "МЕТОДЫ",
                previewIcon = sprites.slidersIcon
            }
        });
    }

    private static void ApplyInitialCardPreview(LevelProgressManager progressManager, LevelCardUI[] levelCards, MainMenuController mainMenuController)
    {
        for (int i = 0; i < levelCards.Length; i++)
        {
            LevelProgressManager.LevelMenuEntry level = progressManager.GetLevel(i + 1);
            if (level == null || levelCards[i] == null)
            {
                continue;
            }

            bool isUnlocked = i == 0;
            levelCards[i].ConfigureLevel(level, i + 1, isUnlocked, false, mainMenuController);
        }
    }

    private static Image CreateStretchImage(string name, Transform parent, Sprite sprite, Color color)
    {
        Image image = CreateImage(name, parent, sprite, color);
        StretchToParent(image.rectTransform);
        return image;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        image.color = color;
        image.type = Image.Type.Sliced;
        return image;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string text,
        TMP_FontAsset fontAsset,
        float fontSize,
        FontStyles fontStyle,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TMP_Text textComponent = textObject.GetComponent<TMP_Text>();
        textComponent.font = fontAsset;
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.color = color;
        textComponent.alignment = alignment;
        textComponent.textWrappingMode = TextWrappingModes.Normal;
        textComponent.raycastTarget = false;

        return textComponent;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        Sprite sprite,
        Vector2 size,
        Vector2 anchoredPosition,
        string labelText,
        TMP_FontAsset fontAsset,
        bool isAccent)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(SimpleMenuAnimator));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = (RectTransform)buttonObject.transform;
        SetCentered(rectTransform, size, anchoredPosition);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;

        TMP_Text label = CreateText("Label", rectTransform, labelText, fontAsset, 38f, FontStyles.Bold, isAccent ? new Color(0.21f, 0.19f, 0.16f, 1f) : PrimaryTextColor, TextAlignmentOptions.Center);
        StretchToParent(label.rectTransform);

        SimpleMenuAnimator animator = buttonObject.GetComponent<SimpleMenuAnimator>();
        animator.Configure(rectTransform, image, label, isAccent);

        return button;
    }

    private static Slider CreateSlider(
        string name,
        Transform parent,
        Vector2 size,
        Vector2 anchoredPosition,
        Sprite trackSprite,
        Sprite fillSprite,
        Sprite handleSprite)
    {
        GameObject sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);

        RectTransform sliderTransform = (RectTransform)sliderObject.transform;
        SetCentered(sliderTransform, size, anchoredPosition);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        Image background = CreateImage("Background", sliderTransform, trackSprite, Color.white);
        SetCentered(background.rectTransform, new Vector2(size.x, 18f), Vector2.zero);
        background.raycastTarget = false;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderTransform, false);
        RectTransform fillAreaTransform = fillArea.GetComponent<RectTransform>();
        StretchToParent(fillAreaTransform);
        fillAreaTransform.offsetMin = new Vector2(12f, 9f);
        fillAreaTransform.offsetMax = new Vector2(-12f, -9f);

        Image fill = CreateImage("Fill", fillAreaTransform, fillSprite, Color.white);
        StretchToParent(fill.rectTransform);
        fill.raycastTarget = false;

        GameObject handleSlideArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleSlideArea.transform.SetParent(sliderTransform, false);
        RectTransform handleSlideAreaTransform = handleSlideArea.GetComponent<RectTransform>();
        StretchToParent(handleSlideAreaTransform);
        handleSlideAreaTransform.offsetMin = new Vector2(12f, -12f);
        handleSlideAreaTransform.offsetMax = new Vector2(-12f, 12f);

        GameObject handleRoot = new GameObject("Handle Root", typeof(RectTransform));
        handleRoot.transform.SetParent(handleSlideAreaTransform, false);
        RectTransform handleRootTransform = handleRoot.GetComponent<RectTransform>();
        handleRootTransform.anchorMin = new Vector2(0f, 0.5f);
        handleRootTransform.anchorMax = new Vector2(0f, 0.5f);
        handleRootTransform.pivot = new Vector2(0.5f, 0.5f);
        handleRootTransform.sizeDelta = new Vector2(72f, 48f);
        handleRootTransform.localScale = Vector3.one;

        Image handleArea = CreateImage("HandleArea", handleRootTransform, fillSprite, new Color(0.92f, 0.87f, 0.79f, 0.35f));
        SetCentered(handleArea.rectTransform, new Vector2(68f, 18f), Vector2.zero);
        handleArea.raycastTarget = false;

        Image handle = CreateImage("Handle", handleRootTransform, handleSprite, Color.white);
        handle.type = Image.Type.Simple;
        SetCentered(handle.rectTransform, new Vector2(36f, 36f), Vector2.zero);

        slider.targetGraphic = handle;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handleRootTransform;

        return slider;
    }

    private static Toggle CreateToggle(
        string name,
        Transform parent,
        Vector2 size,
        Vector2 anchoredPosition,
        Sprite offSprite,
        out Image graphic)
    {
        GameObject toggleObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
        toggleObject.transform.SetParent(parent, false);

        RectTransform rectTransform = (RectTransform)toggleObject.transform;
        SetCentered(rectTransform, size, anchoredPosition);

        graphic = toggleObject.GetComponent<Image>();
        graphic.sprite = offSprite;
        graphic.type = Image.Type.Sliced;
        graphic.color = Color.white;

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = graphic;
        toggle.graphic = null;

        return toggle;
    }

    private static TMP_Dropdown CreateDropdown(
        string name,
        Transform parent,
        Vector2 size,
        Vector2 anchoredPosition,
        Sprite controlSprite,
        Sprite templateSprite,
        Sprite itemSprite,
        TMP_FontAsset fontAsset)
    {
        GameObject dropdownObject = TMP_DefaultControls.CreateDropdown(GetTmpResources());
        dropdownObject.name = name;
        dropdownObject.transform.SetParent(parent, false);

        RectTransform rectTransform = (RectTransform)dropdownObject.transform;
        SetCentered(rectTransform, size, anchoredPosition);

        Image background = dropdownObject.GetComponent<Image>();
        background.sprite = controlSprite;
        background.type = Image.Type.Sliced;
        background.color = Color.white;

        TMP_Dropdown dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
        dropdown.template = dropdown.transform.Find("Template") as RectTransform;
        dropdown.captionText = dropdown.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        dropdown.itemText = dropdown.transform.Find("Template/Viewport/Content/Item/Item Label").GetComponent<TextMeshProUGUI>();

        TMP_Text label = dropdown.captionText;
        ConfigureDropdownText(label, fontAsset);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.rectTransform.anchorMin = new Vector2(0f, 0f);
        label.rectTransform.anchorMax = new Vector2(1f, 1f);
        label.rectTransform.offsetMin = new Vector2(28f, 10f);
        label.rectTransform.offsetMax = new Vector2(-56f, -10f);

        Transform arrowTransform = dropdown.transform.Find("Arrow");
        if (arrowTransform != null)
        {
            Image arrow = arrowTransform.GetComponent<Image>();
            if (arrow != null)
            {
                arrow.color = SecondaryTextColor;
            }

            RectTransform arrowRect = arrowTransform as RectTransform;
            if (arrowRect != null)
            {
                SetAnchored(arrowRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(22f, 14f));
            }
        }

        RectTransform template = dropdown.template;
        template.sizeDelta = new Vector2(size.x, 230f);
        Image templateImage = template.GetComponent<Image>();
        templateImage.sprite = templateSprite;
        templateImage.type = Image.Type.Sliced;
        templateImage.color = new Color(1f, 1f, 1f, 0.95f);

        Toggle itemToggle = template.Find("Viewport/Content/Item").GetComponent<Toggle>();
        Image itemBackground = itemToggle.targetGraphic as Image;
        if (itemBackground != null)
        {
            itemBackground.sprite = itemSprite;
            itemBackground.type = Image.Type.Sliced;
            itemBackground.color = Color.white;
        }

        itemToggle.transition = Selectable.Transition.None;
        itemToggle.graphic = null;

        TMP_Text itemLabel = dropdown.itemText;
        ConfigureDropdownText(itemLabel, fontAsset);
        itemLabel.alignment = TextAlignmentOptions.MidlineLeft;

        Transform itemCheckmark = template.Find("Viewport/Content/Item/Item Checkmark");
        if (itemCheckmark != null)
        {
            itemCheckmark.gameObject.SetActive(false);
        }

        Scrollbar scrollbar = template.GetComponentInChildren<Scrollbar>(true);
        if (scrollbar != null)
        {
            scrollbar.gameObject.SetActive(false);
        }

        dropdown.value = 0;
        dropdown.RefreshShownValue();
        return dropdown;
    }

    private static void ConfigureDropdownText(TMP_Text text, TMP_FontAsset fontAsset)
    {
        text.font = fontAsset;
        text.fontSize = 30f;
        text.fontStyle = FontStyles.Bold;
        text.color = PrimaryTextColor;
        text.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private static TMP_DefaultControls.Resources GetTmpResources()
    {
        return new TMP_DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
        };
    }

    private static TMP_Text FindText(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private static void SetCentered(RectTransform rectTransform, Vector2 size, Vector2 anchoredPosition)
    {
        SetAnchored(rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, size);
    }

    private static void SetAnchored(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.localScale = Vector3.one;
    }
}
#endif
