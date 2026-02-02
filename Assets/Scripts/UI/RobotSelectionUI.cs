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

    public event System.Action<RobotType> OnSelectedRobotChanged;

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
            var entry = new ButtonEntry
            {
                button = button,
                image = image,
                type = DefaultOrder[i],
                root = button.transform,
                lockOverlay = button.transform.parent != null
                    ? button.transform.parent.Find("CloseIcon")?.gameObject
                    : null
            };

            int capturedIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnButtonClicked(DefaultOrder[capturedIndex]));

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

    private void OnButtonClicked(RobotType type)
    {
        if (unlockManager != null && !unlockManager.IsRobotUnlocked(type))
            return;

        _selectedType = type;
        UpdateVisuals();

        if (spawner != null)
        {
            spawner.SetSelectedRobotType(type, spawnOnSelection);
        }

        OnSelectedRobotChanged?.Invoke(type);
    }

    private void SelectInitial()
    {
        _selectedType = defaultSelected;
        EnsureValidSelection();

        if (spawner != null)
        {
            spawner.SetSelectedRobotType(_selectedType, spawnOnSelection);
        }

        OnSelectedRobotChanged?.Invoke(_selectedType);
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
