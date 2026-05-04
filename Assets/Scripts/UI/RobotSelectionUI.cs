using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class RobotSelectionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RobotSpawner spawner;
    [SerializeField] private RobotUnlockManager unlockManager;

    [Header("Selection")]
    [SerializeField] private bool spawnOnSelection = true;
    [SerializeField] private RobotType defaultSelected = RobotType.Base;

    [Header("Visuals")]
    [SerializeField] private Color unlockedTint = Color.white;
    [SerializeField] private Color lockedTint = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color selectedTint = Color.white;
    [SerializeField] private float selectedScale = 1.05f;

    private readonly List<ButtonEntry> _entries = new List<ButtonEntry>();
    private RobotType _selectedType = RobotType.Base;
    private bool _spawnOnSelectionRuntime = true;

    public event System.Action<RobotType> OnSelectedRobotChanged;
    public event System.Action<RobotType> OnRobotButtonClicked;
    public static event System.Action<RobotType> OnAnyRobotButtonClicked;

    private static readonly RobotType[] DefaultOrder =
    {
        RobotType.Base,
        RobotType.Attacker,
        RobotType.Healer,
        RobotType.Defender
    };

    private void Awake()
    {
        if (spawner == null)
            spawner = FindFirstObjectByType<RobotSpawner>();

        if (unlockManager == null)
            unlockManager = FindFirstObjectByType<RobotUnlockManager>();

        _spawnOnSelectionRuntime = true;
        BuildEntries();
    }

    private void Start()
    {
        if (unlockManager == null)
            unlockManager = FindFirstObjectByType<RobotUnlockManager>();

        if (unlockManager != null)
        {
            unlockManager.OnProgressApplied += OnProgressApplied;
            unlockManager.OnRobotUnlocked += OnRobotUnlocked;
        }

        if (unlockManager != null && unlockManager.GetAllRobotConfigs().Count > 0)
        {
            ApplyIconsFromConfigs();
        }

        RebindButtons();
        SelectInitial();
        UpdateVisuals();
    }

    private void OnDestroy()
    {
        if (unlockManager != null)
        {
            unlockManager.OnProgressApplied -= OnProgressApplied;
            unlockManager.OnRobotUnlocked -= OnRobotUnlocked;
        }
    }

    private void OnProgressApplied()
    {
        if (unlockManager == null)
            unlockManager = FindFirstObjectByType<RobotUnlockManager>();

        EnsureValidSelection();
        UpdateVisuals();
    }

    private void OnRobotUnlocked(RobotType type)
    {
        EnsureValidSelection();
        UpdateVisuals();
    }

    private void BuildEntries()
    {
        _entries.Clear();

        var typedRoots = GetComponentsInChildren<RobotSelectionButton>(true)
            .Where(r => r != null && r.robotType != RobotType.None)
            .ToList();

        typedRoots.Sort((a, b) =>
        {
            var ap = a.transform.parent != null ? a.transform.parent.GetSiblingIndex() : a.transform.GetSiblingIndex();
            var bp = b.transform.parent != null ? b.transform.parent.GetSiblingIndex() : b.transform.GetSiblingIndex();
            return ap.CompareTo(bp);
        });

        var usedTypes = new HashSet<RobotType>();
        foreach (var typedRoot in typedRoots)
        {
            if (typedRoot == null || usedTypes.Contains(typedRoot.robotType))
                continue;

            var button = typedRoot.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(b => b != null && b.gameObject.name == "RobotIcon");

            if (button == null)
                continue;

            var image = button.GetComponent<Image>();
            var entry = new ButtonEntry
            {
                button = button,
                image = image,
                type = typedRoot.robotType,
                root = button.transform,
                lockOverlay = FindLockOverlay(typedRoot.transform)
            };

            _entries.Add(entry);
            usedTypes.Add(typedRoot.robotType);
        }

        if (_entries.Count > 0)
            return;

        var buttons = GetComponentsInChildren<Button>(true)
            .Where(b => b.gameObject.name == "RobotIcon")
            .ToList();

        if (buttons.Count == 0)
            return;

        buttons.Sort((a, b) =>
        {
            var ap = a.transform.parent != null ? a.transform.parent.GetSiblingIndex() : a.transform.GetSiblingIndex();
            var bp = b.transform.parent != null ? b.transform.parent.GetSiblingIndex() : b.transform.GetSiblingIndex();
            return ap.CompareTo(bp);
        });

        for (int i = 0; i < buttons.Count && i < DefaultOrder.Length; i++)
        {
            var button = buttons[i];
            var image = button.GetComponent<Image>();
            var typeSource = button.GetComponentInParent<RobotSelectionButton>();
            var resolvedType = typeSource != null ? typeSource.robotType : DefaultOrder[i];
            var root = typeSource != null ? typeSource.transform : button.transform.parent;
            var entry = new ButtonEntry
            {
                button = button,
                image = image,
                type = resolvedType,
                root = button.transform,
                lockOverlay = FindLockOverlay(root)
            };

            _entries.Add(entry);
        }
    }

    private void ApplyIconsFromConfigs()
    {
        if (unlockManager == null)
            return;

        foreach (var entry in _entries)
        {
            var config = unlockManager.GetRobotConfig(entry.type);
            if (config != null && entry.image != null && config.robotIcon != null)
            {
                entry.image.sprite = config.robotIcon;
            }
        }
    }

    private void RebindButtons()
    {
        foreach (var entry in _entries)
        {
            if (entry.button == null)
                continue;

            entry.button.onClick.RemoveAllListeners();
            entry.button.onClick.AddListener(() => OnButtonClicked(entry.type));
        }
    }

    private void OnButtonClicked(RobotType type)
    {
        if (unlockManager != null && !unlockManager.IsRobotUnlocked(type))
            return;

        OnRobotButtonClicked?.Invoke(type);
        OnAnyRobotButtonClicked?.Invoke(type);

        bool shouldSpawnNow = spawnOnSelection && _spawnOnSelectionRuntime;
        if (spawner != null && shouldSpawnNow && !spawner.CanSpawnType(type, out string denyReason))
        {
            spawner.NotifySpawnDenied(type, denyReason);
            return;
        }

        SetSelectedRobot(type, shouldSpawnNow);
    }

    private void SelectInitial()
    {
        _selectedType = defaultSelected;
        EnsureValidSelection();
        SetSelectedRobot(_selectedType, false);
    }

    private void EnsureValidSelection()
    {
        if (unlockManager == null)
        {
            if (_selectedType == RobotType.None)
                _selectedType = RobotType.Base;
            return;
        }

        if (unlockManager.IsRobotUnlocked(_selectedType))
            return;

        foreach (var type in DefaultOrder)
        {
            if (unlockManager.IsRobotUnlocked(type))
            {
                _selectedType = type;
                return;
            }
        }

        _selectedType = RobotType.Base;
    }

    public RobotType GetSelectedType()
    {
        return _selectedType;
    }

    public void SetSelectedRobot(RobotType type, bool spawnNow = false)
    {
        if (type == RobotType.None)
            return;

        if (unlockManager != null && !unlockManager.IsRobotUnlocked(type))
            return;

        _selectedType = type;
        UpdateVisuals();

        if (spawner != null)
        {
            spawner.SetSelectedRobotType(type, spawnNow);
        }

        OnSelectedRobotChanged?.Invoke(type);
    }

    public void SetSpawnOnSelectionEnabled(bool enabled)
    {
        _spawnOnSelectionRuntime = enabled;
    }

    public void RefreshUnlockState()
    {
        if (spawner == null)
            spawner = FindFirstObjectByType<RobotSpawner>();

        if (unlockManager == null)
            unlockManager = FindFirstObjectByType<RobotUnlockManager>();

        if (unlockManager != null && unlockManager.GetAllRobotConfigs().Count > 0)
            ApplyIconsFromConfigs();

        RebindButtons();
        EnsureValidSelection();
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        foreach (var entry in _entries)
        {
            bool unlocked = unlockManager == null || unlockManager.IsRobotUnlocked(entry.type);
            bool selected = entry.type == _selectedType;

            if (entry.button != null)
                entry.button.interactable = unlocked;

            if (entry.image != null)
            {
                if (!unlocked)
                    entry.image.color = lockedTint;
                else
                    entry.image.color = selected ? selectedTint : unlockedTint;
            }

            if (entry.root != null)
            {
                entry.root.localScale = selected ? Vector3.one * selectedScale : Vector3.one;
            }

            if (entry.lockOverlay != null)
            {
                entry.lockOverlay.SetActive(!unlocked);
            }
        }

        ForceRefreshLocksByTypedRoots();
    }

    private GameObject FindLockOverlay(Transform rootTransform)
    {
        if (rootTransform == null)
            return null;

        var overlays = rootTransform.GetComponentsInChildren<Transform>(true);
        foreach (var overlay in overlays)
        {
            if (overlay.name == "CloseIcon")
                return overlay.gameObject;
        }

        return null;
    }

    // Safety net: keep lock overlays tied to RobotSelectionButton.robotType,
    // even if button-entry mapping was altered by hierarchy/order changes.
    private void ForceRefreshLocksByTypedRoots()
    {
        if (unlockManager == null)
            return;

        var typedRoots = GetComponentsInChildren<RobotSelectionButton>(true);
        foreach (var typedRoot in typedRoots)
        {
            if (typedRoot == null)
                continue;

            GameObject closeIcon = FindLockOverlay(typedRoot.transform);
            if (closeIcon == null)
                continue;

            bool unlocked = unlockManager.IsRobotUnlocked(typedRoot.robotType);
            closeIcon.SetActive(!unlocked);
        }
    }

    private class ButtonEntry
    {
        public Button button;
        public Image image;
        public RobotType type;
        public Transform root;
        public GameObject lockOverlay;
    }
}
