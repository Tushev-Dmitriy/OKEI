using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class Level4FlowController : MonoBehaviour
{
    private const string ProgressStagePrefsKey = "Level4RoomStage";

    private enum SectionId
    {
        Base,
        Attacker,
        Healer,
        Defender,
        Final
    }

    private sealed class EnemyWaveDefinition
    {
        public string[] EnemyNames = Array.Empty<string>();
        public float Health;
        public float Damage;
    }

    private sealed class SectionDefinition
    {
        public SectionId Id;
        public RobotType RequiredRobotType;
        public RobotType UnlockOnSuccess;
        public RobotType PreferredSelectionType;
        public string SpawnPointName;
        public string CameraAnchorName;
        public string ExitGateName;
        public string GoalMarkerName;
        public string[] MarkerNames = Array.Empty<string>();
        public EnemyWaveDefinition[] Waves = Array.Empty<EnemyWaveDefinition>();
        public int MaxSpawns = 1;
        public string EscortSpawnPointName;
        public float EscortStartHealthRatio;
        public float EscortReadyHealthRatio;
        public float EscortFinishHealthRatio;
        public float CameraMinX;
        public float CameraMaxX;
        public float CameraMinZ;
        public float CameraMaxZ;
        public float PlayerPulseInterval;
        public float PlayerPulseDamage;
        public float EscortPulseInterval;
        public float EscortPulseDamage;
        public float SquadPulseInterval;
        public float SquadPulseDamage;
        public string Header;
        public string TheoryText;
        public string ReadyText;
        public string FailureText;
        public string SuccessText;
        public string[] ObjectiveTexts = Array.Empty<string>();
    }

    [Header("References")]
    [SerializeField] private RobotSpawner spawner;
    [SerializeField] private RobotUnlockManager unlockManager;
    [SerializeField] private RobotSelectionUI selectionUI;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private VCamController cameraController;

    [Header("UI")]
    [SerializeField] private string statusTextObjectName = "HintText";

    [Header("Progress")]
    [SerializeField] private int completedLevelIndex = 4;

    [Header("Legacy Scene Cleanup")]
    [SerializeField] private bool disableLegacyConveyors = true;
    [SerializeField] private bool disableLegacyExitTrigger = true;

    [Header("Flow")]
    [SerializeField] private float stageMessageDelay = 0.85f;
    [SerializeField, Min(0f)] private float unlockHintReplayCooldown = 1.1f;

    [Header("Level4 Combat Tuning")]
    [SerializeField, Min(1f)] private float playerRobotMoveSpeedMultiplier = 1.35f;
    [SerializeField, Min(10f)] private float corridorEnemyBaseHealth = 70f;
    [SerializeField, Min(0f)] private float corridorEnemyHealthStep = 18f;
    [SerializeField, Min(1f)] private float corridorEnemyBaseDamage = 7f;
    [SerializeField, Min(0f)] private float corridorEnemyDamageStep = 1.75f;

    [Header("Camera")]
    [SerializeField] private Vector3 cameraRoomOffset = new(0f, 14f, -6f);

    private static readonly Vector3[] FinalSquadOffsets =
    {
        new(-3f, 0f, 0f),
        new(-1f, 0f, -1.2f),
        new(1f, 0f, -0.2f),
        new(3f, 0f, -1.4f),
        new(-2f, 0f, -2.6f),
        new(2f, 0f, -2.6f)
    };

    private readonly List<SectionDefinition> _sections = new();
    private readonly Dictionary<string, Transform> _sceneTransforms = new(StringComparer.Ordinal);
    private readonly Dictionary<string, EnemyUnit> _sceneEnemies = new(StringComparer.Ordinal);
    private readonly List<EnemyUnit> _sceneEnemiesOrdered = new();
    private readonly List<EnemyUnit> _activeEnemies = new();
    private readonly List<Robot> _finalSquad = new();
    private readonly List<GameObject> _runtimeFinalObjects = new();

    private SectionDefinition _currentSection;
    private Robot _playerRobot;
    private Robot _escortRobot;
    private bool _attemptActive;
    private bool _finalSectionUnlocked;
    private bool _finalRunStarted;
    private bool _levelCompleted;
    private bool _suppressSpawnedRobotHandling;
    private bool _stageTransitionLocked;
    private bool _suppressProgressRefresh;
    private int _progressStage;
    private int _stageIndex;
    private int _activeWaveIndex;
    private int _finalCommittedAttackers;
    private int _finalCommittedHealers;
    private int _finalCommittedDefenders;
    private int _finalCommittedBases;
    private int _finalCommittedTotal;
    private float _playerPulseTimer;
    private float _escortPulseTimer;
    private float _squadPulseTimer;
    private float _lastUnlockHintTime;
    private RobotType _lastHintRobotType;
    private string _statusOverride;

    private void Awake()
    {
        ResolveReferences();
        DisableLegacySceneHelpers();
        BuildSections();
        CacheSceneObjects();
        EnsureRuntimeFinalSectionLayout();
        CacheSceneObjects();
        PrepareStaticScene();
    }

    private void OnEnable()
    {
        if (spawner != null)
        {
            spawner.OnSpawnRequested += ValidateSpawnRequest;
            spawner.OnRobotSpawned += HandleRobotSpawned;
            spawner.OnSpawnDenied += HandleSpawnDenied;
        }

        if (unlockManager != null)
        {
            unlockManager.OnProgressApplied += HandleProgressApplied;
        }

        EnemyUnit.OnEnemyDied += HandleEnemyDied;
    }

    private void Start()
    {
        LoadProgressStage();
        RefreshSectionFromProgress();
    }

    private void Update()
    {
        if (!_attemptActive || _currentSection == null || _levelCompleted)
            return;

        switch (_currentSection.Id)
        {
            case SectionId.Base:
                UpdateBaseSection();
                break;
            case SectionId.Attacker:
                UpdateAttackerSection();
                break;
            case SectionId.Healer:
                UpdateHealerSection();
                break;
            case SectionId.Defender:
                UpdateDefenderSection();
                break;
            case SectionId.Final:
                UpdateFinalSection();
                break;
        }
    }

    private void OnDisable()
    {
        EnemyUnit.OnEnemyDied -= HandleEnemyDied;
        if (spawner != null)
        {
            spawner.OnSpawnRequested -= ValidateSpawnRequest;
            spawner.OnRobotSpawned -= HandleRobotSpawned;
            spawner.OnSpawnDenied -= HandleSpawnDenied;
        }

        if (unlockManager != null)
        {
            unlockManager.OnProgressApplied -= HandleProgressApplied;
        }

        CleanupAttempt(destroyPlayerRobot: true);
        PrepareStaticScene();
    }

    private void ResolveReferences()
    {
        if (spawner == null)
            spawner = FindFirstObjectByType<RobotSpawner>();

        if (unlockManager == null)
            unlockManager = FindFirstObjectByType<RobotUnlockManager>();

        if (selectionUI == null)
            selectionUI = FindFirstObjectByType<RobotSelectionUI>();

        if (cameraController == null)
            cameraController = FindFirstObjectByType<VCamController>();

        if (statusText == null)
        {
            statusText = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)
                .FirstOrDefault(text => text != null && text.name == statusTextObjectName);
        }
    }

    private void DisableLegacySceneHelpers()
    {
        if (disableLegacyConveyors)
        {
            foreach (RobotMoverPlatform mover in FindObjectsByType<RobotMoverPlatform>(FindObjectsSortMode.None))
            {
                if (mover != null && mover.name.StartsWith("conveyor", StringComparison.OrdinalIgnoreCase))
                {
                    mover.enabled = false;
                }
            }
        }

        if (disableLegacyExitTrigger)
        {
            foreach (PlatformExitTrigger trigger in FindObjectsByType<PlatformExitTrigger>(FindObjectsSortMode.None))
            {
                if (trigger != null)
                {
                    trigger.gameObject.SetActive(false);
                }
            }
        }
    }

    private void CacheSceneObjects()
    {
        _sceneTransforms.Clear();
        _sceneEnemies.Clear();
        _sceneEnemiesOrdered.Clear();

        foreach (Transform candidate in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (candidate == null || !candidate.gameObject.scene.IsValid() || candidate.hideFlags != HideFlags.None)
                continue;

            if (candidate.name.StartsWith("L4", StringComparison.Ordinal) ||
                candidate.name.StartsWith("SectionGate_", StringComparison.Ordinal) ||
                candidate.name.EndsWith("Spawn", StringComparison.Ordinal) ||
                candidate.name.Equals("RobotSpawnPos", StringComparison.Ordinal) ||
                candidate.name.Equals("Camera", StringComparison.Ordinal) ||
                candidate.name.Equals("CinemachineCamera", StringComparison.Ordinal))
            {
                _sceneTransforms[candidate.name] = candidate;
            }
        }

        foreach (EnemyUnit enemy in Resources.FindObjectsOfTypeAll<EnemyUnit>())
        {
            if (enemy == null || !enemy.gameObject.scene.IsValid() || enemy.hideFlags != HideFlags.None)
                continue;

            string key = enemy.gameObject.name;
            if (string.IsNullOrWhiteSpace(key))
            {
                key = $"Enemy_{_sceneEnemies.Count}";
            }

            if (_sceneEnemies.ContainsKey(key))
            {
                key = $"{key}_{_sceneEnemies.Count}";
            }

            enemy.SetDestroyOnDeath(false);
            _sceneEnemies[key] = enemy;
            _sceneEnemiesOrdered.Add(enemy);
        }

        _sceneEnemiesOrdered.Sort((left, right) => left.transform.position.z.CompareTo(right.transform.position.z));
    }

    private void PrepareStaticScene()
    {
        CloseAllSectionGates();
        RestorePlacedSceneEnemies();
    }

    private void EnsureRuntimeFinalSectionLayout()
    {
        if (_sceneTransforms.ContainsKey("L4Room_Final") &&
            _sceneTransforms.ContainsKey("L4Spawn_Final") &&
            _sceneTransforms.ContainsKey("L4Goal_Final"))
        {
            return;
        }

        if (!TryGetSceneTransform("L4Room_Defender", out Transform defenderRoom) ||
            !TryGetSceneTransform("L4Spawn_Defender", out Transform defenderSpawn) ||
            !TryGetSceneTransform("L4Camera_Defender", out Transform defenderCamera) ||
            !TryGetSceneTransform("L4Goal_Defender", out Transform defenderGoal) ||
            !TryGetSceneTransform("L4Marker_Defender_MidA", out Transform defenderMidA) ||
            !TryGetSceneTransform("L4Marker_Defender_MidB", out Transform defenderMidB) ||
            !TryGetSceneTransform("SectionGate_Defender_1", out Transform defenderGate))
        {
            return;
        }

        Transform runtimeRoot = new GameObject("L4RuntimeFinalSection").transform;
        Vector3 firstOffset = new(0f, 0f, 36f);
        Vector3 secondOffset = new(0f, 0f, 72f);

        CloneRuntimeObject(defenderRoom.gameObject, "L4Room_Final", defenderRoom.position + firstOffset, defenderRoom.rotation, runtimeRoot);
        CloneRuntimeObject(defenderRoom.gameObject, "L4Room_Final_Ext", defenderRoom.position + secondOffset, defenderRoom.rotation, runtimeRoot);
        CloneRuntimeObject(defenderGate.gameObject, "SectionGate_Final_1", defenderGate.position + firstOffset, defenderGate.rotation, runtimeRoot);

        CreateRuntimeSectionTransform("L4Spawn_Final", defenderSpawn.position + firstOffset, defenderSpawn.rotation, runtimeRoot);
        CreateRuntimeSectionTransform("L4Camera_Final", defenderCamera.position + new Vector3(0f, 0f, 54f), defenderCamera.rotation, runtimeRoot);
        CreateRuntimeSectionTransform("L4Marker_Final_MidA", defenderMidB.position + firstOffset, defenderMidB.rotation, runtimeRoot);
        CreateRuntimeSectionTransform("L4Marker_Final_MidB", defenderMidA.position + secondOffset, defenderMidA.rotation, runtimeRoot);
        CreateRuntimeSectionTransform("L4Goal_Final", defenderGoal.position + secondOffset, defenderGoal.rotation, runtimeRoot);

        float enemyY = 2.54f;
        if (_sceneEnemies.TryGetValue("L4Enemy_Defender_A_1", out EnemyUnit defenderTemplate) && defenderTemplate != null)
        {
            enemyY = defenderTemplate.transform.position.y;
        }

        CreateRuntimeFinalEnemy("L4Enemy_Attacker_A_1", "L4Enemy_Final_A_1", new Vector3(2.5f, enemyY, 168f), runtimeRoot);
        CreateRuntimeFinalEnemy("L4Enemy_Attacker_A_2", "L4Enemy_Final_A_2", new Vector3(7.5f, enemyY, 170f), runtimeRoot);
        CreateRuntimeFinalEnemy("L4Enemy_Healer_A_1", "L4Enemy_Final_B_1", new Vector3(3f, enemyY, 185f), runtimeRoot);
        CreateRuntimeFinalEnemy("L4Enemy_Attacker_B_1", "L4Enemy_Final_B_2", new Vector3(7f, enemyY, 188f), runtimeRoot);
        CreateRuntimeFinalEnemy("L4Enemy_Defender_A_1", "L4Enemy_Final_C_1", new Vector3(5f, enemyY, 201f), runtimeRoot);
        CreateRuntimeFinalEnemy("L4Enemy_Defender_B_1", "L4Enemy_Final_C_2", new Vector3(5f, enemyY, 210f), runtimeRoot);
    }

    private Transform CloneRuntimeObject(
        GameObject source,
        string newName,
        Vector3 position,
        Quaternion rotation,
        Transform parent)
    {
        if (source == null)
            return null;

        GameObject clone = Instantiate(source, position, rotation, parent);
        clone.name = newName;
        clone.SetActive(true);
        _runtimeFinalObjects.Add(clone);
        return clone.transform;
    }

    private Transform CreateRuntimeSectionTransform(string objectName, Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject go = new(objectName);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(position, rotation);
        _runtimeFinalObjects.Add(go);
        return go.transform;
    }

    private EnemyUnit CreateRuntimeFinalEnemy(string templateName, string enemyName, Vector3 position, Transform parent)
    {
        if (!_sceneEnemies.TryGetValue(templateName, out EnemyUnit template) || template == null)
            return null;

        GameObject clone = Instantiate(template.gameObject, position, template.transform.rotation, parent);
        clone.name = enemyName;
        clone.SetActive(false);
        _runtimeFinalObjects.Add(clone);

        return clone.GetComponent<EnemyUnit>();
    }

    private void BuildSections()
    {
        _sections.Clear();

        _sections.Add(new SectionDefinition
        {
            Id = SectionId.Base,
            RequiredRobotType = RobotType.Base,
            UnlockOnSuccess = RobotType.Attacker,
            PreferredSelectionType = RobotType.Base,
            SpawnPointName = "RobotSpawnPos",
            CameraAnchorName = "RobotSpawnPos",
            ExitGateName = "SectionGate_Base_1",
            GoalMarkerName = "L4Goal_Base",
            MarkerNames = new[] { "L4Marker_Base_Move", "L4Marker_Base_Damage" },
            Waves = new[]
            {
                new EnemyWaveDefinition
                {
                    EnemyNames = new[] { "L4Enemy_Base_A_1" },
                    Health = 120f,
                    Damage = 24f
                }
            },
            CameraMinX = -4f,
            CameraMaxX = 14f,
            CameraMinZ = 8f,
            CameraMaxZ = 38f,
            Header = "Section 1. BaseRobot",
            TheoryText = "BaseRobot\\n- Move()\\n- TakeDamage()\\nThe base class owns the shared behavior for every robot below it.",
            ReadyText = "Launch the base robot. It can move and survive some damage, but this room is designed so the base class eventually loses.",
            FailureText = "The base robot failed before finishing the introduction. Launch it again and watch the shared behavior step by step.",
            SuccessText = "Robot section complete. The base class reached the fight, took damage, and then lost. Now the first child class becomes available.",
            ObjectiveTexts = new[]
            {
                "Step 1 of 3. Ride the conveyor lane and reach the first marker.",
                "Step 2 of 3. Cross the pressure zone and show that Robot survives damage while moving forward.",
                "Step 3 of 3. Reach the guard and watch the base class run out of tools. This death unlocks the attacker room."
            }
        });

        _sections.Add(new SectionDefinition
        {
            Id = SectionId.Attacker,
            RequiredRobotType = RobotType.Attacker,
            UnlockOnSuccess = RobotType.Healer,
            PreferredSelectionType = RobotType.Attacker,
            SpawnPointName = "RobotSpawnPos",
            CameraAnchorName = "RobotSpawnPos",
            ExitGateName = "SectionGate_Attacker_1",
            GoalMarkerName = "L4Goal_Attacker",
            MarkerNames = new[] { "L4Marker_Attacker_MidA", "L4Marker_Attacker_MidB" },
            Waves = new[]
            {
                new EnemyWaveDefinition
                {
                    EnemyNames = new[] { "L4Enemy_Attacker_A_1", "L4Enemy_Attacker_A_2" },
                    Health = 100f,
                    Damage = 11f
                },
                new EnemyWaveDefinition
                {
                    EnemyNames = new[] { "L4Enemy_Attacker_B_1" },
                    Health = 130f,
                    Damage = 14f
                }
            },
            CameraMinX = -4f,
            CameraMaxX = 14f,
            CameraMinZ = 44f,
            CameraMaxZ = 74f,
            Header = "Section 2. AttackRobot : BaseRobot",
            TheoryText = "AttackRobot : BaseRobot\\n- inherited: Move(), TakeDamage()\\n- added: Attack()\\nInheritance keeps the base behavior and adds a new combat role on top of it.",
            ReadyText = "Launch the attacker. This room is blocked by enemy checkpoints, so the new attack behavior is now required.",
            FailureText = "The attacker fell before the room was cleared. This room should make Attack() feel necessary, not optional.",
            SuccessText = "Attacker section complete. The child class keeps the base movement and adds a brand new way to solve the room.",
            ObjectiveTexts = new[]
            {
                "Step 1 of 3. Break the first blockers in the corridor.",
                "Step 2 of 3. Push forward through the mid checkpoint.",
                "Step 3 of 3. Reach the terminal area. Attack() is the key skill in this run."
            }
        });

        _sections.Add(new SectionDefinition
        {
            Id = SectionId.Healer,
            RequiredRobotType = RobotType.Healer,
            UnlockOnSuccess = RobotType.Defender,
            PreferredSelectionType = RobotType.Healer,
            SpawnPointName = "RobotSpawnPos",
            CameraAnchorName = "RobotSpawnPos",
            ExitGateName = "SectionGate_Healer_1",
            GoalMarkerName = "L4Goal_Healer",
            MarkerNames = new[] { "L4Marker_Healer_Mid" },
            Waves = new[]
            {
                new EnemyWaveDefinition
                {
                    EnemyNames = new[] { "L4Enemy_Healer_A_1" },
                    Health = 0f,
                    Damage = 0f
                }
            },
            CameraMinX = -4f,
            CameraMaxX = 14f,
            CameraMinZ = 80f,
            CameraMaxZ = 110f,
            Header = "Section 3. HealRobot : BaseRobot",
            TheoryText = "HealRobot : BaseRobot\\n- inherited: Move(), TakeDamage()\\n- added: Heal()\\nNot every child class is a stronger fighter. Some inherit the same base and solve a different problem.",
            ReadyText = "Launch the healer. This room now tests survival support on a single robot: Heal() must offset constant pressure long enough to cross the corridor.",
            FailureText = "The healer section failed. Launch it again and keep the robot alive through the support corridor.",
            SuccessText = "Healer section complete. This child class solves a support problem instead of replacing the attacker.",
            ObjectiveTexts = new[]
            {
                "Phase 1. Cross the first support zone and show that Heal() can answer room damage.",
                "Phase 2. Reach the middle marker while pressure keeps ticking on the same robot.",
                "Phase 3. Keep moving and survive to the exit marker."
            }
        });

        _sections.Add(new SectionDefinition
        {
            Id = SectionId.Defender,
            RequiredRobotType = RobotType.Defender,
            UnlockOnSuccess = RobotType.None,
            PreferredSelectionType = RobotType.Defender,
            SpawnPointName = "RobotSpawnPos",
            CameraAnchorName = "RobotSpawnPos",
            ExitGateName = "SectionGate_Defender_1",
            GoalMarkerName = "L4Goal_Defender",
            MarkerNames = new[] { "L4Marker_Defender_MidA", "L4Marker_Defender_MidB" },
            Waves = new[]
            {
                new EnemyWaveDefinition
                {
                    EnemyNames = new[] { "L4Enemy_Defender_A_1" },
                    Health = 150f,
                    Damage = 18f
                },
                new EnemyWaveDefinition
                {
                    EnemyNames = new[] { "L4Enemy_Defender_B_1" },
                    Health = 180f,
                    Damage = 20f
                }
            },
            CameraMinX = -4f,
            CameraMaxX = 14f,
            CameraMinZ = 116f,
            CameraMaxZ = 146f,
            Header = "Section 4. DefenseRobot : BaseRobot",
            TheoryText = "DefenseRobot : BaseRobot\\n- inherited core behavior\\n- added: Defend() through damage reduction and tanking\\nThis is where the same base action starts feeling different because the child class changes how it survives.",
            ReadyText = "Launch the defender. The reactor room keeps applying pressure, so the specialized tank behavior has to carry the run.",
            FailureText = "The defender could not hold the reactor chamber. This room exists to make specialization feel real.",
            SuccessText = "Defender section complete. The last child class is tested, so the final squad room is now ready.",
            ObjectiveTexts = new[]
            {
                "Step 1 of 3. Enter the reactor segment and hold pressure.",
                "Step 2 of 3. Push through the center holdout.",
                "Step 3 of 3. Reach the exit marker while tanking damage."
            }
        });

        _sections.Add(new SectionDefinition
        {
            Id = SectionId.Final,
            RequiredRobotType = RobotType.None,
            UnlockOnSuccess = RobotType.None,
            PreferredSelectionType = RobotType.Attacker,
            SpawnPointName = "RobotSpawnPos",
            CameraAnchorName = "RobotSpawnPos",
            ExitGateName = "SectionGate_Final_1",
            GoalMarkerName = "L4Goal_Final",
            MarkerNames = new[] { "L4Marker_Final_MidA", "L4Marker_Final_MidB" },
            Waves = new[]
            {
                new EnemyWaveDefinition
                {
                    EnemyNames = Array.Empty<string>(),
                    Health = 0f,
                    Damage = 0f
                }
            },
            MaxSpawns = 5,
            CameraMinX = -4f,
            CameraMaxX = 14f,
            CameraMinZ = 80f,
            CameraMaxZ = 220f,
            Header = "Section 5. Inheritance Squad",
            TheoryText = "BaseRobot\\n- Move()\\n- TakeDamage()\\n\\nAttackRobot : BaseRobot\\n- Attack()\\nHealRobot : BaseRobot\\n- Heal()\\nDefenseRobot : BaseRobot\\n- Defend()\\n\\nThe base class is shared. The child classes stay related, but each one solves a different part of the final room.",
            ReadyText = "Assemble a squad of 5 robots from the terminal. The room only opens for balanced inherited roles, not for random spam.",
            FailureText = "The squad failed the final chamber. Rebuild the team and try a different class composition.",
            SuccessText = "Level complete. The final room proves inheritance through one shared base class and three specialized child roles working together.",
            ObjectiveTexts = new[]
            {
                "Phase 1 of 2. Build a squad of exactly 5 robots before deployment.",
                "Phase 2 of 2. Clear the entire corridor and keep at least one robot alive."
            }
        });
    }

    private SpawnPermissionResult ValidateSpawnRequest(RobotType requestedType)
    {
        if (_levelCompleted)
        {
            return SpawnPermissionResult.Deny("This version of the level is already complete.");
        }

        if (_currentSection == null)
        {
            return SpawnPermissionResult.Deny("The current section is not initialized yet.");
        }

        if (unlockManager != null && !unlockManager.IsRobotUnlocked(requestedType))
        {
            return SpawnPermissionResult.Deny("That robot class is still locked.");
        }

        if (_currentSection.Id == SectionId.Final)
        {
            if (_finalRunStarted)
            {
                return SpawnPermissionResult.Deny("The final squad is already deployed. Wait for the result, then try again.");
            }

            int limit = Mathf.Max(1, _currentSection.MaxSpawns);
            if (_finalSquad.Count >= limit)
            {
                return SpawnPermissionResult.Deny($"The final room only allows {limit} robots per attempt.");
            }

            return SpawnPermissionResult.Allow();
        }

        if (_attemptActive)
        {
            return SpawnPermissionResult.Deny("Finish the current run before launching another robot.");
        }

        if (requestedType != _currentSection.RequiredRobotType)
        {
            return SpawnPermissionResult.Deny(
                $"Используйте этого робота: {GetRobotName(_currentSection.RequiredRobotType)}");
        }

        return SpawnPermissionResult.Allow();
    }

    private void HandleRobotSpawned(Robot robot)
    {
        if (_suppressSpawnedRobotHandling || robot == null || _currentSection == null || _levelCompleted)
            return;

        if (_currentSection.Id == SectionId.Final)
        {
            HandleFinalRobotSpawned(robot);
            return;
        }

        BeginAttempt(robot);
    }

    private void HandleSpawnDenied(RobotType robotType, string reason)
    {
        _statusOverride = string.IsNullOrWhiteSpace(reason)
            ? $"Cannot launch {GetRobotName(robotType)} right now."
            : reason;

        TryReplayRequiredRobotUnlockHint(robotType);
        UpdateStatus();
    }

    private void TryReplayRequiredRobotUnlockHint(RobotType requestedType)
    {
        if (_currentSection == null || _currentSection.RequiredRobotType == RobotType.None)
            return;

        RobotType requiredType = _currentSection.RequiredRobotType;
        if (requestedType == requiredType)
            return;

        if (unlockManager == null || !unlockManager.IsRobotUnlocked(requiredType))
            return;

        if (Time.unscaledTime - _lastUnlockHintTime < unlockHintReplayCooldown && _lastHintRobotType == requiredType)
            return;

        RobotUnlockHintUI hintUi = FindFirstObjectByType<RobotUnlockHintUI>();
        if (hintUi == null)
            return;

        hintUi.ShowHintForRobot(requiredType);
        _lastUnlockHintTime = Time.unscaledTime;
        _lastHintRobotType = requiredType;
    }

    private void HandleProgressApplied()
    {
        if (_suppressProgressRefresh)
            return;

        _levelCompleted = false;
        RefreshSectionFromProgress();
    }

    private void BeginAttempt(Robot robot)
    {
        CleanupAttempt(destroyPlayerRobot: false);
        CloseAllSectionGates();

        _attemptActive = true;
        _playerRobot = robot;
        _stageIndex = 0;
        _activeWaveIndex = -1;
        _playerPulseTimer = 0f;
        _escortPulseTimer = 0f;
        _statusOverride = null;
        _stageTransitionLocked = false;

        if (_playerRobot != null)
        {
            _playerRobot.Died += HandlePlayerRobotDied;
            ApplyPlayerRobotTuning(_playerRobot);
            _playerRobot.SetAutonomousMode(true);
        }

        InitializeSectionAttempt();
        UpdateStatus();
    }

    private void HandleFinalRobotSpawned(Robot robot)
    {
        if (robot == null || _currentSection == null || _currentSection.Id != SectionId.Final)
            return;

        if (!_attemptActive)
        {
            BeginFinalAssembly();
        }

        int formationIndex = Mathf.Clamp(_finalSquad.Count, 0, FinalSquadOffsets.Length - 1);
        if (spawner != null && spawner.SpawnPoint != null)
        {
            Vector3 spawnPosition = spawner.SpawnPoint.position + FinalSquadOffsets[formationIndex];
            robot.transform.SetPositionAndRotation(spawnPosition, spawner.SpawnPoint.rotation);
        }

        robot.Died += HandleFinalRobotDied;
        ApplyPlayerRobotTuning(robot);
        robot.SetAutonomousMode(false);
        _finalSquad.Add(robot);
        _statusOverride = null;

        if (_finalSquad.Count >= Mathf.Max(1, _currentSection.MaxSpawns))
        {
            StartFinalRun();
        }
        else
        {
            UpdateStatus();
        }
    }

    private void BeginFinalAssembly()
    {
        CleanupAttempt(destroyPlayerRobot: false);
        CloseAllSectionGates();
        DeactivateAllSceneEnemies();

        _attemptActive = true;
        _finalRunStarted = false;
        _stageIndex = 0;
        _activeWaveIndex = -1;
        _playerPulseTimer = 0f;
        _escortPulseTimer = 0f;
        _squadPulseTimer = 0f;
        _statusOverride = null;
        _stageTransitionLocked = false;

        UpdateStatus();
    }

    private void StartFinalRun()
    {
        if (_currentSection == null || _currentSection.Id != SectionId.Final)
            return;

        _finalRunStarted = true;
        _squadPulseTimer = 0f;
        _stageIndex = 1;
        GetFinalCompositionCounts(
            out _finalCommittedAttackers,
            out _finalCommittedHealers,
            out _finalCommittedDefenders,
            out _finalCommittedBases,
            out _finalCommittedTotal);
        _statusOverride = IsAllowedFinalComposition()
            ? "Final squad deployed. The chamber is now checking whether your inherited roles really work together."
            : "Final squad deployed. This composition is not one of the validated balanced lineups, so the terminal may reject it even if some enemies fall.";

        foreach (Robot robot in _finalSquad)
        {
            if (robot != null && robot.IsAlive)
            {
                robot.SetAutonomousMode(true);
            }
        }

        ActivateCorridorEnemiesForRun();
        UpdateStatus();
    }

    private void InitializeSectionAttempt()
    {
        if (_currentSection == null)
            return;

        switch (_currentSection.Id)
        {
            case SectionId.Base:
            case SectionId.Attacker:
            case SectionId.Healer:
            case SectionId.Defender:
                ActivateCorridorEnemiesForRun();
                break;
        }
    }

    private void UpdateFinalSection()
    {
        if (!_finalRunStarted)
            return;

        ApplyFinalSquadPulseIfNeeded(
            _currentSection.SquadPulseInterval,
            _currentSection.SquadPulseDamage,
            enabled: false);

        if (!HasLivingFinalRobot())
        {
            FailCurrentSection(_currentSection.FailureText);
            return;
        }

        if (AreAllActiveEnemiesDefeated())
        {
            if (IsAllowedFinalComposition())
            {
                CompleteCurrentSection();
            }
            else
            {
                FailCurrentSection("The squad reached the end, but the terminal only accepts balanced inheritance lineups: 2/2/1, 2/1/2, or 3/1/1 for Attack / Heal / Defense.");
            }
        }
    }

    private void UpdateBaseSection()
    {
        if (_playerRobot == null || !_playerRobot.IsAlive)
            return;
    }

    private void UpdateAttackerSection()
    {
        if (_playerRobot == null || !_playerRobot.IsAlive)
            return;
    }

    private void UpdateHealerSection()
    {
        if (_playerRobot == null || !_playerRobot.IsAlive)
            return;
    }

    private void UpdateDefenderSection()
    {
        if (_playerRobot == null || !_playerRobot.IsAlive)
            return;
    }

    private void HandleEnemyDied(EnemyUnit enemy)
    {
        if (!_attemptActive || enemy == null)
            return;

        if (_activeEnemies.Remove(enemy))
        {
            UpdateStatus();
        }
    }

    private void HandlePlayerRobotDied(Robot robot)
    {
        if (!_attemptActive || robot != _playerRobot || _currentSection == null)
            return;

        if (_currentSection.Id == SectionId.Base && robot.RobotType == RobotType.Base)
        {
            AdvanceAfterRequiredRobotTest(
                "BaseRobot completed the first lesson: it could Move() and TakeDamage(), but it lost the fight. AttackRobot is now unlocked.",
                openGate: false);
            return;
        }

        if (_currentSection.Id == SectionId.Attacker && robot.RobotType == RobotType.Attacker)
        {
            AdvanceAfterRequiredRobotTest(
                "AttackRobot has been tested. It inherited the base methods and added Attack(), so HealerRobot is now unlocked.",
                openGate: false);
            return;
        }

        if (_currentSection.Id == SectionId.Healer && robot.RobotType == RobotType.Healer)
        {
            AdvanceAfterRequiredRobotTest(
                "HealerRobot has been tested. It kept the base behavior and added Heal(), so DefenseRobot is now unlocked.",
                openGate: false);
            return;
        }

        if (_currentSection.Id == SectionId.Defender && robot.RobotType == RobotType.Defender)
        {
            AdvanceAfterRequiredRobotTest(
                "All robot classes have now been spawned and tested once. Army mode is unlocked: build a squad of 5 robots and push to the end.",
                openGate: false);
            return;
        }

        FailCurrentSection(_currentSection.FailureText);
    }

    private void HandleEscortRobotDied(Robot robot)
    {
        if (!_attemptActive || robot != _escortRobot || _currentSection == null)
            return;

        FailCurrentSection(_currentSection.FailureText);
    }

    private void HandleFinalRobotDied(Robot robot)
    {
        if (!_attemptActive || robot == null || _currentSection == null || _currentSection.Id != SectionId.Final)
            return;

        robot.Died -= HandleFinalRobotDied;

        if (_finalRunStarted && !HasLivingFinalRobot())
        {
            FailCurrentSection(_currentSection.FailureText);
            return;
        }

        UpdateStatus();
    }

    private void ActivateWave(int waveIndex)
    {
        ActivateCorridorEnemiesForRun();
    }

    private void ActivateSectionEnemies(SectionDefinition section)
    {
        ActivateCorridorEnemiesForRun();
    }

    private void SpawnEscortRobot()
    {
        if (spawner == null || string.IsNullOrWhiteSpace(_currentSection?.EscortSpawnPointName))
            return;

        if (!TryGetSceneTransform(_currentSection.EscortSpawnPointName, out Transform escortSpawn))
            return;

        try
        {
            _suppressSpawnedRobotHandling = true;
            _escortRobot = spawner.SpawnRobotOfType(
                RobotType.Attacker,
                escortSpawn.position,
                escortSpawn.rotation,
                registerAsCurrent: false,
                bypassValidation: true);
        }
        finally
        {
            _suppressSpawnedRobotHandling = false;
        }

        if (_escortRobot == null)
            return;

        _escortRobot.Died += HandleEscortRobotDied;
        ApplyPlayerRobotTuning(_escortRobot);
        _escortRobot.SetAutonomousMode(false);

        if (_escortRobot.Health != null)
        {
            float desiredHealth = _escortRobot.Health.MaxHealth * Mathf.Clamp01(_currentSection.EscortStartHealthRatio);
            float damageAmount = Mathf.Max(0f, _escortRobot.Health.CurrentHealth - desiredHealth);
            if (damageAmount > 0f)
            {
                _escortRobot.Health.TakeDamage(damageAmount, escortSpawn.position);
            }
        }
    }

    private void AdvanceAfterRequiredRobotTest(string message, bool openGate)
    {
        if (_currentSection == null)
            return;

        SectionId completedSection = _currentSection.Id;
        RobotType unlockType = _currentSection.UnlockOnSuccess;

        if (openGate)
        {
            SetGateClosed(_currentSection.ExitGateName, closed: false);
        }

        CleanupAttempt(destroyPlayerRobot: true);

        if (completedSection == SectionId.Defender)
        {
            SetProgressStage(4);
            _finalSectionUnlocked = true;
            EnterSection(GetSection(SectionId.Final), message);
            return;
        }

        if (unlockManager != null && unlockType != RobotType.None)
        {
            _suppressProgressRefresh = true;
            unlockManager.UnlockRobot(unlockType);
            _suppressProgressRefresh = false;
            TrySaveProgress();
        }

        SetProgressStage(Mathf.Min(4, _progressStage + 1));
        EnterSection(DetermineCurrentSection(), message);
    }

    private void CompleteCurrentSection()
    {
        if (_currentSection == null || _levelCompleted)
            return;

        if (_currentSection.Id == SectionId.Final)
        {
            SetGateClosed(_currentSection.ExitGateName, closed: false);
            CleanupAttempt(destroyPlayerRobot: true);
            _levelCompleted = true;
            LevelProgressManager.CompleteLevel(completedLevelIndex);
            TrySaveProgress();
            _statusOverride = _currentSection.SuccessText;
            UpdateStatus();
            return;
        }

        AdvanceAfterRequiredRobotTest(_currentSection.SuccessText, openGate: true);
    }

    private void FailCurrentSection(string message)
    {
        CleanupAttempt(destroyPlayerRobot: true);
        EnterSection(_currentSection, message);
    }

    private void RefreshSectionFromProgress()
    {
        CleanupAttempt(destroyPlayerRobot: true);
        ClampProgressStageToUnlocks();
        _finalSectionUnlocked = _progressStage >= 4;
        EnterSection(DetermineCurrentSection(), null);
    }

    private SectionDefinition DetermineCurrentSection()
    {
        return _progressStage switch
        {
            0 => GetSection(SectionId.Base),
            1 => GetSection(SectionId.Attacker),
            2 => GetSection(SectionId.Healer),
            3 => GetSection(SectionId.Defender),
            _ => GetSection(SectionId.Final)
        };
    }

    private SectionDefinition GetSection(SectionId id)
    {
        return _sections.FirstOrDefault(section => section.Id == id);
    }

    private void ApplySectionLayout(SectionDefinition section)
    {
        if (section == null)
            return;

        Transform spawnTransform = null;
        bool hasSectionSpawn = TryGetSceneTransform(section.SpawnPointName, out spawnTransform);
        if (!hasSectionSpawn)
        {
            TryGetSceneTransform("RobotSpawnPos", out spawnTransform);
        }

        if (spawner != null &&
            spawner.SpawnPoint != null &&
            spawnTransform != null)
        {
            spawner.SpawnPoint.position = spawnTransform.position;
            spawner.SpawnPoint.rotation = spawnTransform.rotation;
        }

        // Camera is intentionally not controlled from Level4FlowController.
        // Scene camera behavior is owned by the dedicated camera script on the camera object.
    }

    private void EnterSection(SectionDefinition section, string statusOverride)
    {
        _currentSection = section;
        ApplySectionLayout(_currentSection);
        ResetSectionState(_currentSection);
        _statusOverride = statusOverride;
        SetSelectionForSection(_currentSection);
        UpdateStatus();
    }

    private void SetSelectionForSection(SectionDefinition section)
    {
        if (selectionUI == null || section == null)
            return;

        RobotType selectionType = section.PreferredSelectionType != RobotType.None
            ? section.PreferredSelectionType
            : section.RequiredRobotType;

        if (selectionType != RobotType.None)
        {
            selectionUI.SetSelectedRobot(selectionType, false);
        }
    }

    private void ResetSectionState(SectionDefinition section)
    {
        CloseAllSectionGates();
        RestorePlacedSceneEnemies();

        if (_escortRobot != null)
        {
            _escortRobot.Died -= HandleEscortRobotDied;
            Destroy(_escortRobot.gameObject);
            _escortRobot = null;
        }

        _activeWaveIndex = -1;
        _stageIndex = 0;
        _finalRunStarted = false;
        _squadPulseTimer = 0f;
    }

    private void CleanupAttempt(bool destroyPlayerRobot)
    {
        CancelInvoke(nameof(UnlockStageTransition));
        _attemptActive = false;
        _stageTransitionLocked = false;
        _stageIndex = 0;
        _activeWaveIndex = -1;
        _playerPulseTimer = 0f;
        _escortPulseTimer = 0f;
        _finalCommittedAttackers = 0;
        _finalCommittedHealers = 0;
        _finalCommittedDefenders = 0;
        _finalCommittedBases = 0;
        _finalCommittedTotal = 0;
        _squadPulseTimer = 0f;
        _finalRunStarted = false;

        if (_playerRobot != null)
        {
            _playerRobot.Died -= HandlePlayerRobotDied;

            if (destroyPlayerRobot)
            {
                if (spawner != null)
                {
                    spawner.ClearCurrentRobot(_playerRobot);
                }

                if (_playerRobot.IsAlive)
                {
                    Destroy(_playerRobot.gameObject);
                }
            }

            _playerRobot = null;
        }

        if (_escortRobot != null)
        {
            _escortRobot.Died -= HandleEscortRobotDied;
            if (_escortRobot.IsAlive)
            {
                Destroy(_escortRobot.gameObject);
            }
            _escortRobot = null;
        }

        foreach (Robot squadRobot in _finalSquad)
        {
            if (squadRobot == null)
                continue;

            squadRobot.Died -= HandleFinalRobotDied;

            if (destroyPlayerRobot)
            {
                if (spawner != null)
                {
                    spawner.ClearCurrentRobot(squadRobot);
                }

                if (squadRobot.IsAlive)
                {
                    Destroy(squadRobot.gameObject);
                }
            }
        }

        _finalSquad.Clear();

        RestorePlacedSceneEnemies();
        _activeEnemies.Clear();
    }

    private void DeactivateAllSceneEnemies()
    {
        foreach (EnemyUnit enemy in _sceneEnemies.Values)
        {
            if (enemy != null)
            {
                enemy.gameObject.SetActive(false);
            }
        }
    }

    private void RestorePlacedSceneEnemies()
    {
        for (int i = 0; i < _sceneEnemiesOrdered.Count; i++)
        {
            EnemyUnit enemy = _sceneEnemiesOrdered[i];
            if (enemy == null)
                continue;

            float health = corridorEnemyBaseHealth + (corridorEnemyHealthStep * i);
            float damage = corridorEnemyBaseDamage + (corridorEnemyDamageStep * i);
            enemy.gameObject.SetActive(true);
            enemy.Configure(health, damage);
            enemy.SetDestroyOnDeath(false);
        }
    }

    private void ActivateCorridorEnemiesForRun()
    {
        _activeEnemies.Clear();
        _activeWaveIndex = -1;
        RestorePlacedSceneEnemies();

        foreach (EnemyUnit enemy in _sceneEnemiesOrdered)
        {
            if (enemy == null || enemy.Health == null)
                continue;

            _activeEnemies.Add(enemy);
        }

        UpdateStatus();
    }

    private void CloseAllSectionGates()
    {
        foreach ((string key, Transform value) in _sceneTransforms)
        {
            if (value != null && key.StartsWith("SectionGate_", StringComparison.Ordinal))
            {
                value.gameObject.SetActive(true);
            }
        }
    }

    private void SetGateClosed(string gateName, bool closed)
    {
        if (string.IsNullOrWhiteSpace(gateName))
            return;

        if (_sceneTransforms.TryGetValue(gateName, out Transform gateTransform) && gateTransform != null)
        {
            gateTransform.gameObject.SetActive(closed);
        }
    }

    private void AdvanceStage(string message)
    {
        if (_stageTransitionLocked)
            return;

        _stageTransitionLocked = true;
        _stageIndex++;
        _statusOverride = message;
        UpdateStatus();
        Invoke(nameof(UnlockStageTransition), stageMessageDelay);
    }

    private void UnlockStageTransition()
    {
        _stageTransitionLocked = false;
        _statusOverride = null;
        UpdateStatus();
    }

    private bool TryGetSceneTransform(string transformName, out Transform sceneTransform)
    {
        if (_sceneTransforms.TryGetValue(transformName, out sceneTransform) && sceneTransform != null)
            return true;

        sceneTransform = FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate != null && candidate.name == transformName);

        if (sceneTransform != null)
        {
            _sceneTransforms[transformName] = sceneTransform;
            return true;
        }

        return false;
    }

    private bool AreAllActiveEnemiesDefeated()
    {
        for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            EnemyUnit enemy = _activeEnemies[i];
            if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.Health == null || !enemy.Health.IsAlive)
            {
                _activeEnemies.RemoveAt(i);
            }
        }

        return _activeEnemies.Count == 0;
    }

    private float GetHealthRatio(Robot robot)
    {
        if (robot == null || robot.Health == null || robot.Health.MaxHealth <= 0f)
            return 0f;

        return robot.Health.CurrentHealth / robot.Health.MaxHealth;
    }

    private bool HasLivingFinalRobot()
    {
        foreach (Robot robot in _finalSquad)
        {
            if (robot != null && robot.IsAlive)
            {
                return true;
            }
        }

        return false;
    }

    private int GetLivingFinalRobotCount()
    {
        int count = 0;

        foreach (Robot robot in _finalSquad)
        {
            if (robot != null && robot.IsAlive)
            {
                count++;
            }
        }

        return count;
    }

    private void ApplyPlayerPulseIfNeeded(float interval, float damage, bool enabled)
    {
        if (!enabled || interval <= 0f || damage <= 0f || _playerRobot == null || _playerRobot.Health == null || !_playerRobot.IsAlive)
            return;

        _playerPulseTimer -= Time.deltaTime;
        if (_playerPulseTimer > 0f)
            return;

        float adjustedDamage = _playerRobot.ModifyIncomingDamage(damage);
        _playerRobot.Health.TakeDamage(adjustedDamage, _playerRobot.transform.position);
        _playerPulseTimer = interval;
        UpdateStatus();
    }

    private void ApplyEscortPulseIfNeeded(float interval, float damage, bool enabled)
    {
        if (!enabled || interval <= 0f || damage <= 0f || _escortRobot == null || _escortRobot.Health == null || !_escortRobot.IsAlive)
            return;

        _escortPulseTimer -= Time.deltaTime;
        if (_escortPulseTimer > 0f)
            return;

        float adjustedDamage = _escortRobot.ModifyIncomingDamage(damage);
        _escortRobot.Health.TakeDamage(adjustedDamage, _escortRobot.transform.position);
        _escortPulseTimer = interval;
        UpdateStatus();
    }

    private void ApplyPlayerRobotTuning(Robot robot)
    {
        if (robot == null)
            return;

        robot.SetMoveSpeedMultiplier(playerRobotMoveSpeedMultiplier);
    }

    private void ApplyFinalSquadPulseIfNeeded(float interval, float damage, bool enabled)
    {
        if (!enabled || interval <= 0f || damage <= 0f || !_finalRunStarted)
            return;

        _squadPulseTimer -= Time.deltaTime;
        if (_squadPulseTimer > 0f)
            return;

        foreach (Robot robot in _finalSquad)
        {
            if (robot == null || !robot.IsAlive || robot.Health == null)
                continue;

            float adjustedDamage = robot.ModifyIncomingDamage(damage);
            robot.Health.TakeDamage(adjustedDamage, robot.transform.position);
        }

        _squadPulseTimer = interval;
        UpdateStatus();
    }

    private bool IsAllowedFinalComposition()
    {
        GetCommittedFinalCompositionCounts(out int attackers, out int healers, out int defenders, out int bases, out int total);

        if (bases > 0 || total != 5)
            return false;

        return (attackers == 2 && healers == 2 && defenders == 1) ||
               (attackers == 2 && healers == 1 && defenders == 2) ||
               (attackers == 3 && healers == 1 && defenders == 1);
    }

    private void GetFinalCompositionCounts(out int attackers, out int healers, out int defenders, out int bases, out int total)
    {
        attackers = 0;
        healers = 0;
        defenders = 0;
        bases = 0;
        total = 0;

        foreach (Robot robot in _finalSquad)
        {
            if (robot == null)
                continue;

            total++;
            switch (robot.RobotType)
            {
                case RobotType.Base:
                    bases++;
                    break;
                case RobotType.Attacker:
                    attackers++;
                    break;
                case RobotType.Healer:
                    healers++;
                    break;
                case RobotType.Defender:
                    defenders++;
                    break;
            }
        }
    }

    private void GetCommittedFinalCompositionCounts(out int attackers, out int healers, out int defenders, out int bases, out int total)
    {
        if (_finalCommittedTotal > 0)
        {
            attackers = _finalCommittedAttackers;
            healers = _finalCommittedHealers;
            defenders = _finalCommittedDefenders;
            bases = _finalCommittedBases;
            total = _finalCommittedTotal;
            return;
        }

        GetFinalCompositionCounts(out attackers, out healers, out defenders, out bases, out total);
    }

    private void UpdateStatus()
    {
        if (statusText == null)
            return;

        if (_currentSection == null)
        {
            statusText.text = "Level4 1 is not initialized.";
            return;
        }

        string objective = _attemptActive
            ? GetActiveObjectiveText()
            : _currentSection.ReadyText;

        string runtime = GetRuntimeStateText();
        string theory = _currentSection.TheoryText;

        string message = $"{_currentSection.Header}\n{theory}\n\n{objective}";
        if (!string.IsNullOrWhiteSpace(runtime))
        {
            message += $"\n\n{runtime}";
        }

        if (!string.IsNullOrWhiteSpace(_statusOverride))
        {
            message = $"{_statusOverride}\n\n{message}";
        }

        if (_levelCompleted)
        {
            message += "\n\nThe level is complete. This scene now teaches a base class, inherited expansion, support specialization, and defensive specialization through separated rooms.";
        }

        statusText.text = message;
    }

    private string GetActiveObjectiveText()
    {
        if (_currentSection == null || _currentSection.ObjectiveTexts == null || _currentSection.ObjectiveTexts.Length == 0)
            return string.Empty;

        int objectiveIndex = Mathf.Clamp(_stageIndex, 0, _currentSection.ObjectiveTexts.Length - 1);
        return _currentSection.ObjectiveTexts[objectiveIndex];
    }

    private string GetRuntimeStateText()
    {
        if (_currentSection == null)
            return string.Empty;

        if (!_attemptActive)
        {
            if (_currentSection.Id == SectionId.Final)
            {
                return "Build a squad of 5 robots. Valid terminal patterns: 2 Attack / 2 Heal / 1 Defense, 2 / 1 / 2, or 3 / 1 / 1. BaseRobot stays available, but it is not part of a winning final lineup.";
            }

            return $"Launch {GetRobotName(_currentSection.RequiredRobotType)}. This room only progresses after that class finishes its test and dies.";
        }

        List<string> lines = new();

        if (_currentSection.Id == SectionId.Final)
        {
            int limit = Mathf.Max(1, _currentSection.MaxSpawns);

            if (_finalRunStarted)
            {
                GetCommittedFinalCompositionCounts(out int attackers, out int healers, out int defenders, out int bases, out int total);
                lines.Add($"Squad size: {total}/{limit}");
                lines.Add($"Composition: Base {bases}, Attack {attackers}, Heal {healers}, Defense {defenders}");
                lines.Add($"Living robots: {GetLivingFinalRobotCount()}");
                lines.Add(IsAllowedFinalComposition()
                    ? "Composition check: valid balanced lineup."
                    : "Composition check: invalid lineup for the final terminal.");
            }
            else
            {
                GetFinalCompositionCounts(out int attackers, out int healers, out int defenders, out int bases, out int total);
                lines.Add($"Squad size: {total}/{limit}");
                lines.Add($"Composition: Base {bases}, Attack {attackers}, Heal {healers}, Defense {defenders}");
                lines.Add(total >= limit
                    ? "The squad is full and will deploy immediately."
                    : $"Deploy {limit - total} more robot(s) to start the final chamber.");
            }
        }

        if (_playerRobot != null && _playerRobot.Health != null)
        {
            lines.Add($"Current robot: {Mathf.CeilToInt(_playerRobot.Health.CurrentHealth)}/{Mathf.CeilToInt(_playerRobot.Health.MaxHealth)} HP");
        }

        if (_escortRobot != null && _escortRobot.Health != null)
        {
            lines.Add($"Escort ally: {Mathf.CeilToInt(_escortRobot.Health.CurrentHealth)}/{Mathf.CeilToInt(_escortRobot.Health.MaxHealth)} HP");
        }

        if (_activeEnemies.Count > 0)
        {
            lines.Add($"Active enemies in room: {_activeEnemies.Count}");
        }

        if (_currentSection.Id == SectionId.Defender)
        {
            lines.Add("Reactor pressure is active in this room.");
        }
        else if (_currentSection.Id == SectionId.Healer && _stageIndex >= 1)
        {
            lines.Add("The escort is taking periodic room damage and needs steady support.");
        }
        else if (_currentSection.Id == SectionId.Final && _finalRunStarted)
        {
            lines.Add("Factory pressure is ticking on the whole squad, so Heal() and Defend() both matter here.");
        }

        return string.Join("\n", lines);
    }

    private string GetRobotName(RobotType robotType)
    {
        if (unlockManager == null)
            return robotType.ToString();

        RobotConfigSO config = unlockManager.GetRobotConfig(robotType);
        return config != null && !string.IsNullOrWhiteSpace(config.robotName)
            ? config.robotName
            : robotType.ToString();
    }

    private void TrySaveProgress()
    {
        PlayerSaver saver = FindFirstObjectByType<PlayerSaver>();
        if (saver != null)
        {
            saver.SavePlayerData();
        }
    }

    private void LoadProgressStage()
    {
        _progressStage = Mathf.Clamp(PlayerPrefs.GetInt(ProgressStagePrefsKey, 0), 0, 4);
        ClampProgressStageToUnlocks();
    }

    private void SetProgressStage(int stage)
    {
        _progressStage = Mathf.Clamp(stage, 0, 4);
        PlayerPrefs.SetInt(ProgressStagePrefsKey, _progressStage);
        PlayerPrefs.Save();
    }

    private void ClampProgressStageToUnlocks()
    {
        int highestUnlockedStage = 0;

        if (unlockManager != null)
        {
            if (unlockManager.IsRobotUnlocked(RobotType.Attacker))
                highestUnlockedStage = Mathf.Max(highestUnlockedStage, 1);
            if (unlockManager.IsRobotUnlocked(RobotType.Healer))
                highestUnlockedStage = Mathf.Max(highestUnlockedStage, 2);
            if (unlockManager.IsRobotUnlocked(RobotType.Defender))
                highestUnlockedStage = Mathf.Max(highestUnlockedStage, 3);
        }

        int maxAllowedStage = highestUnlockedStage;
        if (highestUnlockedStage >= 3 && _progressStage >= 4)
        {
            maxAllowedStage = 4;
        }

        _progressStage = Mathf.Clamp(_progressStage, 0, maxAllowedStage);
    }
}
