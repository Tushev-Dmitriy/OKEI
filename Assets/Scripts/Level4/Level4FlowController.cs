using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Level4FlowController : MonoBehaviour
{
    private const string ProgressStagePrefsKey = "Level4RoomStage";
    private const string SquadUnlockedHintDefault = "Теперь доступен режим отряда: собери до 5 роботов и зачисти коридор.";
    private const string SquadReminderHintDefault = "Режим отряда активен: собери состав из 5 роботов и запускай штурм.";
    internal enum SectionId
    {
        Base,
        Attacker,
        Healer,
        Defender,
        Final
    }

    [Header("References")]
    [SerializeField] private RobotSpawner spawner;
    [SerializeField] private RobotUnlockManager unlockManager;
    [SerializeField] private RobotSelectionUI selectionUI;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private VCamController cameraController;
    [SerializeField] private Level4FlowSetupModule setupModule;
    [SerializeField] private Level4FlowEventsModule eventsModule;
    [SerializeField] private Level4FlowUpdateModule updateModule;
    [SerializeField] private Level4ProgressModule progressModule;
    [SerializeField] private Level4EffectsModule effectsModule;
    [SerializeField] private Level4SquadMovementModule squadMovementModule;
    [SerializeField] private Level4SquadHudModule squadHudModule;
    [SerializeField] private Level4SquadDeploymentModule squadDeploymentModule;
    [SerializeField] private Level4LocalizationModule localizationModule;
    [SerializeField] private Level4SquadCompositionModule squadCompositionModule;
    [SerializeField] private Level4EnemyCorridorModule enemyCorridorModule;
    [SerializeField] private Level4CombatRuntimeModule combatRuntimeModule;
    [SerializeField] private Level4StatusModule statusModule;
    [SerializeField] private Level4SectionNavigationModule sectionNavigationModule;
    [SerializeField] private Level4StageFlowModule stageFlowModule;
    [SerializeField] private Level4AttemptFlowModule attemptFlowModule;
    [SerializeField] private Level4SpawnFlowModule spawnFlowModule;
    [SerializeField] private Level4SectionEventsModule sectionEventsModule;
    [SerializeField] private Level4SceneContentModule sceneContentModule;
    [SerializeField] private Level4SectionLifecycleModule sectionLifecycleModule;
    [SerializeField] private Level4BootstrapModule bootstrapModule;
    [SerializeField] private Level4FinalSquadCoreModule finalSquadCoreModule;

    [Header("UI")]
    [SerializeField] private string statusTextObjectName = "HintText";
    [SerializeField] private bool hideLevelUpWindowOnStart = true;
    [SerializeField] private string levelUpWindowObjectName = "LevelUpWindow";

    [Header("Progress")]
    [SerializeField] private int completedLevelIndex = 4;

    [Header("Legacy Scene Cleanup")]
    [SerializeField] private bool disableLegacyConveyors = true;
    [SerializeField] private bool disableLegacyExitTrigger = true;

    [Header("Flow")]
    [SerializeField] private float stageMessageDelay = 0.85f;
    [SerializeField, Min(0f)] private float unlockHintReplayCooldown = 1.1f;
    [SerializeField] private string squadUnlockedHintText = SquadUnlockedHintDefault;
    [SerializeField] private string squadReminderHintText = SquadReminderHintDefault;

    [Header("Level4 Combat Tuning")]
    [SerializeField, Min(1f)] private float playerRobotMoveSpeedMultiplier = 1.35f;
    [SerializeField, Min(10f)] private float corridorEnemyBaseHealth = 70f;
    [SerializeField, Min(0f)] private float corridorEnemyHealthStep = 18f;
    [SerializeField, Min(1f)] private float corridorEnemyBaseDamage = 7f;
    [SerializeField, Min(0f)] private float corridorEnemyDamageStep = 1.75f;

    [Header("Camera")]
    [SerializeField] private Vector3 cameraRoomOffset = new(0f, 14f, -6f);

    [Header("Squad HUD")]
    [SerializeField] private string squadHudTitle = "Режим отряда";
    [SerializeField] private string squadHudHintBuild = "Выбирай тип робота и собирай состав до 5.";
    [SerializeField] private string squadHudHintRun = "Отряд запущен. Очистка отключена до конца попытки.";
    [SerializeField, Min(0.1f)] private float squadSpawnDelay = 1.5f;
    [SerializeField, Min(0.05f)] private float squadHudFadeDuration = 0.2f;
    [Header("Enemy Respawn FX")]
    [SerializeField, Min(0.05f)] private float enemyRespawnScaleDuration = 0.22f;
    [SerializeField, Range(0.05f, 1f)] private float enemyRespawnStartScaleFactor = 0.25f;

    [Header("Squad Spawn Stabilization")]
    [SerializeField, Min(0.2f)] private float squadMinMoveSpacing = 1.25f;
    [SerializeField, Range(0.05f, 1f)] private float squadMinSpeedFactorWhenBlocked = 0.2f;
    [SerializeField, Min(0.5f)] private float squadGroundProbeHeight = 5f;
    [SerializeField, Min(0f)] private float squadGroundOffset = 0.03f;
    [SerializeField] private LayerMask squadGroundMask = ~0;

    [Header("Completion Transition")]
    [SerializeField] private int completionTargetSceneBuildIndex = 0;
    [SerializeField, Min(0.01f)] private float completionFadeInDuration = 0.8f;
    [SerializeField, Min(0.01f)] private float completionFadeOutDuration = 0.35f;
    [SerializeField] private string completionLoadingStatus = "Завершение уровня";
    [SerializeField] private string completionFinalStatus = "Возвращаемся в меню";
    [SerializeField] private string completionHintText = "Сохраняем прогресс и открываем следующий уровень";
    [SerializeField] private bool completionLockCursorAfterLoad = false;

    private readonly List<SectionDefinition> _sections = new();
    private readonly Dictionary<string, Transform> _sceneTransforms = new(StringComparer.Ordinal);
    private readonly Dictionary<string, EnemyUnit> _sceneEnemies = new(StringComparer.Ordinal);
    private readonly List<EnemyUnit> _sceneEnemiesOrdered = new();
    private readonly List<EnemyUnit> _activeEnemies = new();
    private readonly List<Robot> _finalSquad = new();
    private readonly List<RobotType> _plannedFinalSquad = new();
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
    private bool _squadHintShownThisSession;
    private bool _isFinalDeploying;
    private bool _completionTransitionStarted;
    private GameObject _levelUpWindowObject;
    internal RobotSpawner Spawner => spawner; internal RobotSpawner SpawnerMutable { get => spawner; set => spawner = value; }
    internal RobotUnlockManager UnlockManager => unlockManager; internal RobotUnlockManager UnlockManagerMutable { get => unlockManager; set => unlockManager = value; }
    internal RobotSelectionUI SelectionUIRef => selectionUI; internal RobotSelectionUI SelectionUIMutable { get => selectionUI; set => selectionUI = value; }
    internal VCamController CameraControllerMutable { get => cameraController; set => cameraController = value; }
    internal TMP_Text StatusTextMutable { get => statusText; set => statusText = value; } internal TMP_Text StatusTextRef => statusText;
    internal string StatusTextObjectName => statusTextObjectName; internal bool HideLevelUpWindowOnStart => hideLevelUpWindowOnStart; internal string LevelUpWindowObjectName => levelUpWindowObjectName;
    internal GameObject LevelUpWindowObjectMutable { get => _levelUpWindowObject; set => _levelUpWindowObject = value; }
    internal bool DisableLegacyConveyors => disableLegacyConveyors; internal bool DisableLegacyExitTrigger => disableLegacyExitTrigger;
    internal Button SquadHudClearButton => squadHudModule != null ? squadHudModule.ClearButton : null;
    internal bool LevelCompleted => _levelCompleted; internal bool LevelCompletedValue { get => _levelCompleted; set => _levelCompleted = value; }
    internal bool IsSquadModeUnlockedForHud() => IsSquadModeUnlocked();
    internal bool FinalRunStarted => _finalRunStarted; internal bool FinalRunStartedMutable { get => _finalRunStarted; set => _finalRunStarted = value; } internal bool FinalRunStartedValue => _finalRunStarted;
    internal bool IsFinalDeploying { get => _isFinalDeploying; set => _isFinalDeploying = value; } internal bool IsInFinalSection => _currentSection != null && _currentSection.Id == SectionId.Final;
    internal int FinalSectionSpawnLimit => Mathf.Max(1, _currentSection != null ? _currentSection.MaxSpawns : 1); internal List<RobotType> PlannedFinalSquad => _plannedFinalSquad; internal List<Robot> FinalSquad => _finalSquad;
    internal bool AttemptActive => _attemptActive; internal bool AttemptActiveValue { get => _attemptActive; set => _attemptActive = value; }
    internal bool HasCurrentSection => _currentSection != null; internal bool CurrentSectionIsFinal => _currentSection != null && _currentSection.Id == SectionId.Final; internal bool CurrentSectionIsDefender => _currentSection != null && _currentSection.Id == SectionId.Defender; internal bool CurrentSectionIsHealer => _currentSection != null && _currentSection.Id == SectionId.Healer;
    internal List<EnemyUnit> ActiveEnemies => _activeEnemies; internal List<EnemyUnit> SceneEnemiesOrdered => _sceneEnemiesOrdered; internal Dictionary<string, EnemyUnit> SceneEnemiesMap => _sceneEnemies; internal Dictionary<string, Transform> SceneTransformsMap => _sceneTransforms; internal List<GameObject> RuntimeFinalObjects => _runtimeFinalObjects;
    internal Robot PlayerRobotRef => _playerRobot; internal Robot EscortRobotRef => _escortRobot; internal Robot EscortRobotMutable { get => _escortRobot; set => _escortRobot = value; } internal Robot PlayerRobotMutable { get => _playerRobot; set => _playerRobot = value; }
    internal Level4CombatRuntimeModule CombatRuntime => combatRuntimeModule; internal string StatusOverride { get => _statusOverride; set => _statusOverride = value; }
    internal string CurrentSectionReadyText => _currentSection != null ? _currentSection.ReadyText : string.Empty; internal string CurrentSectionTheoryText => _currentSection != null ? _currentSection.TheoryText : string.Empty; internal string CurrentSectionHeader => _currentSection != null ? _currentSection.Header : string.Empty; internal string[] CurrentSectionObjectiveTexts => _currentSection != null ? _currentSection.ObjectiveTexts : Array.Empty<string>(); internal RobotType CurrentSectionRequiredRobotType => _currentSection != null ? _currentSection.RequiredRobotType : RobotType.None; internal string CurrentSectionFailureText => _currentSection != null ? _currentSection.FailureText : string.Empty;
    internal float CurrentSectionSquadPulseInterval => _currentSection != null ? _currentSection.SquadPulseInterval : 0f; internal float CurrentSectionSquadPulseDamage => _currentSection != null ? _currentSection.SquadPulseDamage : 0f;
    internal int StageIndexValue => _stageIndex; internal int StageIndexMutable { get => _stageIndex; set => _stageIndex = value; } internal int ActiveWaveIndexValue { get => _activeWaveIndex; set => _activeWaveIndex = value; } internal bool StageTransitionLocked { get => _stageTransitionLocked; set => _stageTransitionLocked = value; }
    internal List<SectionDefinition> Sections => _sections; internal int ProgressStageValue => _progressStage; internal int ProgressStage { get => _progressStage; set => _progressStage = value; } internal bool SuppressProgressRefresh { get => _suppressProgressRefresh; set => _suppressProgressRefresh = value; }
    internal SectionDefinition CurrentSectionDef { get => _currentSection; set => _currentSection = value; }
    internal bool SquadHintShownThisSession { get => _squadHintShownThisSession; set => _squadHintShownThisSession = value; } internal bool SuppressSpawnedRobotHandling { get => _suppressSpawnedRobotHandling; set => _suppressSpawnedRobotHandling = value; }
    internal bool FinalSectionUnlocked => _finalSectionUnlocked; internal bool FinalSectionUnlockedValue { get => _finalSectionUnlocked; set => _finalSectionUnlocked = value; }
    internal int CompletedLevelIndex => completedLevelIndex; internal string ProgressStagePrefsKeyName => ProgressStagePrefsKey; internal string SquadUnlockedHintDefaultText => SquadUnlockedHintDefault; internal string SquadReminderHintDefaultText => SquadReminderHintDefault;
    internal string SquadUnlockedHintText { get => squadUnlockedHintText; set => squadUnlockedHintText = value; } internal string SquadReminderHintText { get => squadReminderHintText; set => squadReminderHintText = value; }
    internal float EnemyRespawnScaleDuration => enemyRespawnScaleDuration; internal float EnemyRespawnStartScaleFactor => enemyRespawnStartScaleFactor; internal float PlayerRobotMoveSpeedMultiplierValue => playerRobotMoveSpeedMultiplier;
    internal float SquadMinMoveSpacing => squadMinMoveSpacing; internal float SquadMinSpeedFactorWhenBlocked => squadMinSpeedFactorWhenBlocked; internal float SquadGroundProbeHeight => squadGroundProbeHeight; internal float SquadGroundOffset => squadGroundOffset; internal LayerMask SquadGroundMask => squadGroundMask;
    internal float SquadHudFadeDuration => squadHudFadeDuration; internal float SquadSpawnDelay => squadSpawnDelay; internal float CorridorEnemyBaseHealthValue => corridorEnemyBaseHealth; internal float CorridorEnemyHealthStepValue => corridorEnemyHealthStep; internal float CorridorEnemyBaseDamageValue => corridorEnemyBaseDamage; internal float CorridorEnemyDamageStepValue => corridorEnemyDamageStep;
    internal int CompletionTargetSceneBuildIndex => completionTargetSceneBuildIndex; internal bool CompletionLockCursorAfterLoad => completionLockCursorAfterLoad;
    internal float CompletionFadeInDuration => completionFadeInDuration; internal float CompletionFadeOutDuration => completionFadeOutDuration;
    internal string CompletionLoadingStatus => completionLoadingStatus; internal string CompletionFinalStatus => completionFinalStatus; internal string CompletionHintText => completionHintText;
    internal bool CompletionTransitionStarted { get => _completionTransitionStarted; set => _completionTransitionStarted = value; }
    internal float PlayerPulseTimer { get => _playerPulseTimer; set => _playerPulseTimer = value; } internal float EscortPulseTimer { get => _escortPulseTimer; set => _escortPulseTimer = value; } internal float SquadPulseTimer { get => _squadPulseTimer; set => _squadPulseTimer = value; }
    internal int FinalCommittedAttackers { get => _finalCommittedAttackers; set => _finalCommittedAttackers = value; } internal int FinalCommittedHealers { get => _finalCommittedHealers; set => _finalCommittedHealers = value; } internal int FinalCommittedDefenders { get => _finalCommittedDefenders; set => _finalCommittedDefenders = value; } internal int FinalCommittedBases { get => _finalCommittedBases; set => _finalCommittedBases = value; } internal int FinalCommittedTotal { get => _finalCommittedTotal; set => _finalCommittedTotal = value; }
    internal float UnlockHintReplayCooldownValue => unlockHintReplayCooldown; internal float LastUnlockHintTime { get => _lastUnlockHintTime; set => _lastUnlockHintTime = value; } internal RobotType LastHintRobotType { get => _lastHintRobotType; set => _lastHintRobotType = value; }
    internal void RefreshSquadHud() => UpdateSquadHud(); internal void SetStatusTextValue(string value) { }
    internal void GetFinalCompositionCountsForModule(out int attackers, out int healers, out int defenders, out int bases, out int total) => GetFinalCompositionCounts(out attackers, out healers, out defenders, out bases, out total);
    internal void GetCommittedFinalCompositionCountsForModule(out int attackers, out int healers, out int defenders, out int bases, out int total) => GetCommittedFinalCompositionCounts(out attackers, out healers, out defenders, out bases, out total);
    internal bool IsAllowedFinalCompositionForModule() => IsAllowedFinalComposition(); internal void SetActiveWaveIndexValue(int value) => _activeWaveIndex = value;
    internal bool TryGetSceneTransformForModule(string transformName, out Transform sceneTransform) => TryGetSceneTransform(transformName, out sceneTransform);
    internal void ResetSectionStateForModule(SectionDefinition section) => ResetSectionState(section); internal void ShowSquadModeHintForModule(bool showReminderOnly) => ShowSquadModeHint(showReminderOnly); internal void CleanupAttemptForModule(bool destroyPlayerRobot) => CleanupAttempt(destroyPlayerRobot); internal void CloseAllSectionGatesForModule() => CloseAllSectionGates();
    internal void ApplyPlayerRobotTuningForModule(Robot robot) => ApplyPlayerRobotTuning(robot);
    internal void SubscribePlayerRobotDeathForModule(Robot robot) { if (robot != null) robot.Died += HandlePlayerRobotDied; }
    internal void SubscribeEscortRobotDeathForModule(Robot robot) { if (robot != null) robot.Died += HandleEscortRobotDied; }
    internal void SubscribeFinalRobotDeathForModule(Robot robot) { if (robot != null) robot.Died += HandleFinalRobotDied; }
    internal void SetProgressStageForModule(int stage) => SetProgressStage(stage); internal void TrySaveProgressForModule() => TrySaveProgress(); internal void ActivateCorridorEnemiesForRunForModule() => ActivateCorridorEnemiesForRun(); internal void ScheduleUnlockStageTransitionForModule() => Invoke(nameof(UnlockStageTransition), stageMessageDelay);
    internal void PlayEnemyRespawnScaleForModule(EnemyUnit enemy) => PlayEnemyRespawnScale(enemy); internal void ClearEnemyRespawnCacheForModule() => effectsModule.ClearEnemyRespawnCache(); internal void RememberEnemyBaseScaleForModule(EnemyUnit enemy) => effectsModule.RememberEnemyBaseScale(enemy); internal void RestorePlacedSceneEnemiesForModule() => RestorePlacedSceneEnemies();
    internal void BeginAttemptForModule(Robot robot) => BeginAttempt(robot); internal void HandleFinalRobotSpawnedForModule(Robot robot) => HandleFinalRobotSpawned(robot);
    internal void StabilizeFinalSpawnedRobotForModule(Robot robot) => StabilizeFinalSpawnedRobot(robot); internal void DeactivateAllSceneEnemiesForModule() => DeactivateAllSceneEnemies(); internal void UpdateFinalSquadSpacingForModule() => UpdateFinalSquadSpacing(); internal void ApplyFinalSquadPulseIfNeededForModule(float interval, float damage, bool enabled) => ApplyFinalSquadPulseIfNeeded(interval, damage, enabled);
    internal bool AreAllActiveEnemiesDefeatedForModule() => AreAllActiveEnemiesDefeated(); internal void CompleteCurrentSectionForModule() => CompleteCurrentSection(); internal Sprite GetRobotIconForModule(RobotType robotType) => GetRobotIcon(robotType); internal void RefreshSectionFromProgressForModule() => RefreshSectionFromProgress(); internal void AdvanceAfterRequiredRobotTestForModule(string message, bool openGate) => AdvanceAfterRequiredRobotTest(message, openGate); internal void FailCurrentSectionForModule(string message) => FailCurrentSection(message); internal bool HasLivingFinalRobotForModule() => HasLivingFinalRobot();
    internal void UnsubscribeFinalRobotDeathForModule(Robot robot) { if (robot != null) robot.Died -= HandleFinalRobotDied; } internal void UnsubscribePlayerRobotDeathForModule(Robot robot) { if (robot != null) robot.Died -= HandlePlayerRobotDied; } internal void UnsubscribeEscortRobotDeathForModule(Robot robot) { if (robot != null) robot.Died -= HandleEscortRobotDied; }
    internal void CancelUnlockStageTransitionInvokeForModule() => CancelInvoke(nameof(UnlockStageTransition)); internal void CancelSquadDeploymentForModule() => squadDeploymentModule.CancelDeployment(); internal void SetGateClosedForModule(string gateName, bool closed) => SetGateClosed(gateName, closed); internal void EnterSectionForModule(SectionDefinition section, string statusOverride) => EnterSection(section, statusOverride); internal SectionDefinition GetSectionForModule(SectionId id) => GetSection(id); internal SectionDefinition DetermineCurrentSectionForModule() => DetermineCurrentSection(); internal void ClampProgressStageToUnlocksForModule() => ClampProgressStageToUnlocks();
    internal bool TryStartCompletionTransitionForModule() => TryStartCompletionTransition();

    private void Awake() { EnsureModules(); setupModule.RunAwake(this); }
    private void OnEnable()
    {
        EnsureModules();
        eventsModule.RunOnEnable(this);
    }
    private void Start() => setupModule.RunStart(this);
    private void Update() => updateModule.RunUpdate(this);
    private void OnDisable()
    {
        if (eventsModule == null)
            return;

        eventsModule.RunOnDisable(this);
    }

    public void DebugPrepareFinalSquad()
    {
        EnsureModules();
        ResolveReferences();

        _suppressProgressRefresh = true;
        try
        {
            if (unlockManager != null)
            {
                unlockManager.ApplyProgress(new RobotProgressData
                {
                    unlockedRobotTypes = new List<int>
                    {
                        (int)RobotType.Base,
                        (int)RobotType.Attacker,
                        (int)RobotType.Healer,
                        (int)RobotType.Defender
                    }
                });
            }

            _progressStage = 4;
            _finalSectionUnlocked = true;
            _levelCompleted = false;
            _completionTransitionStarted = false;

            SectionDefinition finalSection = GetSection(SectionId.Final);
            if (finalSection == null)
            {
                Debug.LogWarning($"[{nameof(Level4FlowController)}] Debug complete skipped: final section is not configured.", this);
                return;
            }

            CleanupAttempt(destroyPlayerRobot: true);
            EnterSection(finalSection, "DEBUG: режим отряда активирован.");
            ShowSquadModeHint(showReminderOnly: false);

            if (selectionUI != null)
            {
                selectionUI.SetSpawnOnSelectionEnabled(false);
                selectionUI.SetSelectedRobot(RobotType.Attacker, false);
                selectionUI.RefreshUnlockState();
            }

            foreach (RobotWindowController window in FindObjectsByType<RobotWindowController>(FindObjectsSortMode.None))
            {
                if (window != null)
                    window.RefreshRuntimeState();
            }

            RefreshStatus();
            RefreshSquadHud();
            Debug.Log($"[{nameof(Level4FlowController)}] Debug complete applied: squad mode unlocked.", this);
        }
        finally
        {
            _suppressProgressRefresh = false;
        }
    }

    internal void TickCurrentSection()
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

    internal void EnsureModules()
    {
        setupModule = EnsureModule(setupModule); eventsModule = EnsureModule(eventsModule); updateModule = EnsureModule(updateModule); progressModule = EnsureModule(progressModule); effectsModule = EnsureModule(effectsModule);
        squadMovementModule = EnsureModule(squadMovementModule); squadHudModule = EnsureModule(squadHudModule); squadDeploymentModule = EnsureModule(squadDeploymentModule); localizationModule = EnsureModule(localizationModule); squadCompositionModule = EnsureModule(squadCompositionModule);
        enemyCorridorModule = EnsureModule(enemyCorridorModule); combatRuntimeModule = EnsureModule(combatRuntimeModule); statusModule = EnsureModule(statusModule); sectionNavigationModule = EnsureModule(sectionNavigationModule); stageFlowModule = EnsureModule(stageFlowModule);
        attemptFlowModule = EnsureModule(attemptFlowModule); spawnFlowModule = EnsureModule(spawnFlowModule); sectionEventsModule = EnsureModule(sectionEventsModule); sceneContentModule = EnsureModule(sceneContentModule); sectionLifecycleModule = EnsureModule(sectionLifecycleModule);
        bootstrapModule = EnsureModule(bootstrapModule); finalSquadCoreModule = EnsureModule(finalSquadCoreModule);
    }

    private T EnsureModule<T>(T existing) where T : Component { if (existing != null) return existing; T found = GetComponent<T>(); if (found != null) return found; return gameObject.AddComponent<T>(); }



    // ===== from Level4FlowController.SceneSetup.cs =====
internal void ResolveReferences() => bootstrapModule.ResolveReferences(this);
internal void HideLevelUpWindowAtStartup() => bootstrapModule.HideLevelUpWindowAtStartup(this);
internal void DisableLegacySceneHelpers() => bootstrapModule.DisableLegacySceneHelpers(this);
internal void CacheSceneObjects() => sceneContentModule.CacheSceneObjects(this);
internal void PrepareStaticScene() => sceneContentModule.PrepareStaticScene(this);
internal void EnsureRuntimeFinalSectionLayout() => sceneContentModule.EnsureRuntimeFinalSectionLayout(this);
internal void BuildSections() => sceneContentModule.BuildSections(this);

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

    // ===== from Level4FlowController.SpawnFlow.cs =====
internal SpawnPermissionResult ValidateSpawnRequest(RobotType requestedType) => spawnFlowModule.ValidateSpawnRequest(this, requestedType);
internal void HandleRobotSpawned(Robot robot) => spawnFlowModule.HandleRobotSpawned(this, robot);
internal void HandleSpawnDenied(RobotType robotType, string reason) => spawnFlowModule.HandleSpawnDenied(this, robotType, reason);
private void TryReplayRequiredRobotUnlockHint(RobotType requestedType) => spawnFlowModule.TryReplayRequiredRobotUnlockHint(this, requestedType);
private void ShowSquadModeHint(bool showReminderOnly) => spawnFlowModule.ShowSquadModeHint(this, showReminderOnly);
internal void HandleProgressApplied() => spawnFlowModule.HandleProgressApplied(this);
private void BeginAttempt(Robot robot) => attemptFlowModule.BeginAttempt(this, robot);
private void InitializeSectionAttempt() => attemptFlowModule.InitializeSectionAttempt(this);

    // ===== from Level4FlowController.SectionProgression.cs =====
private void UpdateBaseSection() { if (_playerRobot == null || !_playerRobot.IsAlive) return; }
private void UpdateAttackerSection() { if (_playerRobot == null || !_playerRobot.IsAlive) return; }
private void UpdateHealerSection() { if (_playerRobot == null || !_playerRobot.IsAlive) return; }
private void UpdateDefenderSection() { if (_playerRobot == null || !_playerRobot.IsAlive) return; }
internal void HandleEnemyDied(EnemyUnit enemy) => sectionEventsModule.HandleEnemyDied(this, enemy);
private void HandlePlayerRobotDied(Robot robot) => sectionEventsModule.HandlePlayerRobotDied(this, robot);
private void HandleEscortRobotDied(Robot robot) => sectionEventsModule.HandleEscortRobotDied(this, robot);
private void HandleFinalRobotDied(Robot robot) => sectionEventsModule.HandleFinalRobotDied(this, robot);
private void ActivateWave(int waveIndex) => sectionEventsModule.ActivateWave(this, waveIndex);
private void ActivateSectionEnemies(SectionDefinition section) => sectionEventsModule.ActivateSectionEnemies(this, section);
private void SpawnEscortRobot() => sectionLifecycleModule.SpawnEscortRobot(this);
private void AdvanceAfterRequiredRobotTest(string message, bool openGate) => sectionLifecycleModule.AdvanceAfterRequiredRobotTest(this, message, openGate);
private void CompleteCurrentSection() => sectionLifecycleModule.CompleteCurrentSection(this);
private void FailCurrentSection(string message) => sectionLifecycleModule.FailCurrentSection(this, message);
internal void RefreshSectionFromProgress() => sectionLifecycleModule.RefreshSectionFromProgress(this);
private SectionDefinition DetermineCurrentSection() => sectionNavigationModule.DetermineCurrentSection(this);
private SectionDefinition GetSection(SectionId id) => sectionNavigationModule.GetSection(this, id);
private void ApplySectionLayout(SectionDefinition section) => sectionNavigationModule.ApplySectionLayout(this, section);
private void EnterSection(SectionDefinition section, string statusOverride) => sectionNavigationModule.EnterSection(this, section, statusOverride);
private void SetSelectionForSection(SectionDefinition section) => sectionNavigationModule.SetSelectionForSection(this, section);
private void ResetSectionState(SectionDefinition section) => sectionLifecycleModule.ResetSectionState(this, section);
internal void CleanupAttempt(bool destroyPlayerRobot) => sectionLifecycleModule.CleanupAttempt(this, destroyPlayerRobot);
private void DeactivateAllSceneEnemies() => enemyCorridorModule.DeactivateAllSceneEnemies(this);
private void RestorePlacedSceneEnemies() => enemyCorridorModule.RestorePlacedSceneEnemies(this);
private void ActivateCorridorEnemiesForRun() { enemyCorridorModule.ActivateCorridorEnemiesForRun(this); UpdateStatus(); }
private void AdvanceStage(string message) => stageFlowModule.AdvanceStage(this, message);
private void UnlockStageTransition() => stageFlowModule.UnlockStageTransition(this);

    // ===== from Level4FlowController.RuntimeRules.cs =====
private bool AreAllActiveEnemiesDefeated() => combatRuntimeModule.AreAllActiveEnemiesDefeated(this);
private float GetHealthRatio(Robot robot) => combatRuntimeModule.GetHealthRatio(robot);
private bool HasLivingFinalRobot() => combatRuntimeModule.HasLivingFinalRobot(this);
private int GetLivingFinalRobotCount() => combatRuntimeModule.GetLivingFinalRobotCount(this);
private void ApplyPlayerPulseIfNeeded(float interval, float damage, bool enabled) => combatRuntimeModule.ApplyPlayerPulseIfNeeded(this, interval, damage, enabled);
private void ApplyEscortPulseIfNeeded(float interval, float damage, bool enabled) => combatRuntimeModule.ApplyEscortPulseIfNeeded(this, interval, damage, enabled);

private void ApplyPlayerRobotTuning(Robot robot)
    {
        if (robot == null)
            return;

        robot.SetMoveSpeedMultiplier(playerRobotMoveSpeedMultiplier);
    }

private void ApplyFinalSquadPulseIfNeeded(float interval, float damage, bool enabled) => combatRuntimeModule.ApplyFinalSquadPulseIfNeeded(this, interval, damage, enabled);
private void UpdateStatus() => statusModule.UpdateStatus(this);
private string GetActiveObjectiveText() => statusModule.GetActiveObjectiveText(this);
private string GetRuntimeStateText() => statusModule.GetRuntimeStateText(this);

    // ===== from Level4FlowController.FinalSquad.cs =====
private void HandleFinalRobotSpawned(Robot robot) => finalSquadCoreModule.HandleFinalRobotSpawned(this, robot);
private void BeginFinalAssembly() => finalSquadCoreModule.BeginFinalAssembly(this);
internal void BeginFinalAssemblyFromModule() => finalSquadCoreModule.BeginFinalAssembly(this);
internal void RefreshStatus() => UpdateStatus();
internal string GetRobotDisplayName(RobotType robotType) => GetRobotName(robotType);
private void StartFinalRun() => finalSquadCoreModule.StartFinalRun(this);
internal void HandleSquadHudClearClicked() => finalSquadCoreModule.HandleSquadHudClearClicked(this);
private void UpdateFinalSection() => finalSquadCoreModule.UpdateFinalSection(this);
private void UpdateFinalSquadSpacing() { Transform spawnPoint = spawner != null ? spawner.SpawnPoint : null; squadMovementModule.UpdateFinalSquadSpacing(this, _finalSquad, spawnPoint); }
private bool IsAllowedFinalComposition() => squadCompositionModule.IsAllowedFinalComposition(this);
private void GetFinalCompositionCounts(out int attackers, out int healers, out int defenders, out int bases, out int total) => squadCompositionModule.GetFinalCompositionCounts(this, out attackers, out healers, out defenders, out bases, out total);
private void GetCommittedFinalCompositionCounts(out int attackers, out int healers, out int defenders, out int bases, out int total) => squadCompositionModule.GetCommittedFinalCompositionCounts(this, out attackers, out healers, out defenders, out bases, out total);
internal void HandleRobotSelectionButtonClicked(RobotType selectedType) => squadDeploymentModule.HandleRobotSelectionButtonClicked(this, selectedType);
private void StabilizeFinalSpawnedRobot(Robot robot) => squadMovementModule.StabilizeFinalSpawnedRobot(this, robot);

    // ===== from Level4FlowController.Hud.cs =====
private void UpdateSquadHud() => squadHudModule.UpdateSquadHud(this);
internal void EnsureSquadHud() => squadHudModule.EnsureSquadHud(this);

internal void BuildSquadHudSnapshot(
        out bool visible,
        out int limit,
        out bool canClear,
        out int attackers,
        out int healers,
        out int defenders,
        out int bases,
        out int total)
    {
        visible = _currentSection != null && _currentSection.Id == SectionId.Final && !_levelCompleted;
        limit = _currentSection != null && _currentSection.Id == SectionId.Final
            ? Mathf.Max(1, _currentSection.MaxSpawns)
            : 5;

        if (_finalRunStarted)
            GetCommittedFinalCompositionCounts(out attackers, out healers, out defenders, out bases, out total);
        else
            GetFinalCompositionCounts(out attackers, out healers, out defenders, out bases, out total);

        canClear = _currentSection != null
            && _currentSection.Id == SectionId.Final
            && !_finalRunStarted
            && !_isFinalDeploying
            && total > 0;
    }

private string GetRobotName(RobotType robotType) => localizationModule.GetRobotName(this, robotType);
private Sprite GetRobotIcon(RobotType robotType) => localizationModule.GetRobotIcon(this, robotType);
internal void NormalizeLocalizedHintText() => localizationModule.NormalizeLocalizedHintText(this);

private bool TryStartCompletionTransition()
    {
        if (_completionTransitionStarted)
            return true;

        if (SceneTransitionService.IsRunning)
        {
            _completionTransitionStarted = true;
            return true;
        }

        bool started = SceneTransitionService.StartPortalTransition(
            completionTargetSceneBuildIndex,
            completedLevelIndex,
            completionFadeInDuration,
            completionFadeOutDuration,
            completionLoadingStatus,
            completionFinalStatus,
            completionHintText,
            completionLockCursorAfterLoad,
            null,
            null);

        if (started)
            _completionTransitionStarted = true;

        return started;
    }

    // ===== from Level4FlowController.Effects.cs =====
private void PlayEnemyRespawnScale(EnemyUnit enemy) => effectsModule.PlayEnemyRespawnScale(this, enemy);
internal void StopAllEnemyRespawnScaleCoroutines() => effectsModule.StopAllEnemyRespawnScaleCoroutines();

    // ===== from Level4FlowController.Progress.cs =====
private void TrySaveProgress() => progressModule.TrySaveProgress(this);
internal void LoadProgressStage() => progressModule.LoadProgressStage(this);
private void SetProgressStage(int stage) => progressModule.SetProgressStage(this, stage);
private void ClampProgressStageToUnlocks() => progressModule.ClampProgressStageToUnlocks(this);
private bool IsSquadModeUnlocked() => progressModule.IsSquadModeUnlocked(this);

}

internal sealed class EnemyWaveDefinition
{
    public string[] EnemyNames = Array.Empty<string>();
    public float Health;
    public float Damage;
}

internal sealed class SectionDefinition
{
    public Level4FlowController.SectionId Id;
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

[DisallowMultipleComponent]
public sealed class Level4BootstrapModule : MonoBehaviour
{
    internal void ResolveReferences(Level4FlowController flow)
    {
        if (flow == null)
            return;

        if (flow.SpawnerMutable == null)
            flow.SpawnerMutable = FindFirstObjectByType<RobotSpawner>();

        if (flow.UnlockManagerMutable == null)
            flow.UnlockManagerMutable = FindFirstObjectByType<RobotUnlockManager>();

        if (flow.SelectionUIMutable == null)
            flow.SelectionUIMutable = FindFirstObjectByType<RobotSelectionUI>();

        if (flow.CameraControllerMutable == null)
            flow.CameraControllerMutable = FindFirstObjectByType<VCamController>();

        // Intentionally do not auto-resolve status text by name.
        // This prevents accidental writes into scene-authored HintText labels.

        if (flow.LevelUpWindowObjectMutable == null && !string.IsNullOrWhiteSpace(flow.LevelUpWindowObjectName))
        {
            foreach (Transform candidate in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (candidate == null || !candidate.gameObject.scene.IsValid() || candidate.hideFlags != HideFlags.None)
                    continue;

                if (string.Equals(candidate.name, flow.LevelUpWindowObjectName, StringComparison.Ordinal))
                {
                    flow.LevelUpWindowObjectMutable = candidate.gameObject;
                    break;
                }
            }
        }
    }

    internal void HideLevelUpWindowAtStartup(Level4FlowController flow)
    {
        if (flow == null || !flow.HideLevelUpWindowOnStart)
            return;

        if (flow.LevelUpWindowObjectMutable != null)
            flow.LevelUpWindowObjectMutable.SetActive(false);
    }

    internal void DisableLegacySceneHelpers(Level4FlowController flow)
    {
        if (flow == null)
            return;

        if (flow.DisableLegacyConveyors)
        {
            foreach (RobotMoverPlatform mover in FindObjectsByType<RobotMoverPlatform>(FindObjectsSortMode.None))
            {
                if (mover != null && mover.name.StartsWith("conveyor", StringComparison.OrdinalIgnoreCase))
                    mover.enabled = false;
            }
        }

        if (flow.DisableLegacyExitTrigger)
        {
            foreach (PlatformExitTrigger trigger in FindObjectsByType<PlatformExitTrigger>(FindObjectsSortMode.None))
            {
                if (trigger != null)
                    trigger.gameObject.SetActive(false);
            }
        }
    }

}

[DisallowMultipleComponent]
public sealed class Level4FinalSquadCoreModule : MonoBehaviour
{
    private const string CompositionFailText = "Отряд дошел до конца, но терминал принимает только сбалансированные составы: 2/2/1, 2/1/2 или 3/1/1 для Атаки / Хила / Защиты.";

    internal void HandleFinalRobotSpawned(Level4FlowController flow, Robot robot)
    {
        if (flow == null || robot == null || !flow.CurrentSectionIsFinal)
            return;

        if (!flow.AttemptActive)
            BeginFinalAssembly(flow);

        if (flow.Spawner != null && flow.Spawner.SpawnPoint != null)
            robot.transform.SetPositionAndRotation(flow.Spawner.SpawnPoint.position, flow.Spawner.SpawnPoint.rotation);

        flow.StabilizeFinalSpawnedRobotForModule(robot);
        flow.SubscribeFinalRobotDeathForModule(robot);
        flow.ApplyPlayerRobotTuningForModule(robot);
        robot.SetAutonomousMode(false);
        flow.FinalSquad.Add(robot);
        flow.StatusOverride = null;

        if (flow.FinalSquad.Count >= flow.FinalSectionSpawnLimit)
            StartFinalRun(flow);
        else
            flow.RefreshStatus();
    }

    internal void BeginFinalAssembly(Level4FlowController flow)
    {
        if (flow == null)
            return;

        flow.CleanupAttemptForModule(destroyPlayerRobot: false);
        flow.CloseAllSectionGatesForModule();
        flow.DeactivateAllSceneEnemiesForModule();

        flow.AttemptActiveValue = true;
        flow.FinalRunStartedMutable = false;
        flow.StageIndexMutable = 0;
        flow.ActiveWaveIndexValue = -1;
        flow.PlayerPulseTimer = 0f;
        flow.EscortPulseTimer = 0f;
        flow.SquadPulseTimer = 0f;
        flow.StatusOverride = null;
        flow.StageTransitionLocked = false;

        flow.RefreshStatus();
    }

    internal void StartFinalRun(Level4FlowController flow)
    {
        if (flow == null || !flow.CurrentSectionIsFinal)
            return;

        GameAudio.PlayUi(AudioCueIds.Level4SquadRun, 1f);
        flow.FinalRunStartedMutable = true;
        flow.SquadPulseTimer = 0f;
        flow.StageIndexMutable = 1;
        flow.GetFinalCompositionCountsForModule(
            out int attackers,
            out int healers,
            out int defenders,
            out int bases,
            out int total);

        flow.FinalCommittedAttackers = attackers;
        flow.FinalCommittedHealers = healers;
        flow.FinalCommittedDefenders = defenders;
        flow.FinalCommittedBases = bases;
        flow.FinalCommittedTotal = total;

        flow.StatusOverride = flow.IsAllowedFinalCompositionForModule()
            ? "Отряд запущен. Камера проверяет, насколько роли-наследники работают вместе."
            : "Отряд запущен. Состав не входит в валидные сбалансированные варианты, терминал может отклонить попытку.";

        for (int i = 0; i < flow.FinalSquad.Count; i++)
        {
            Robot squadRobot = flow.FinalSquad[i];
            if (squadRobot != null && squadRobot.IsAlive)
                squadRobot.SetAutonomousMode(true);
        }

        flow.ActivateCorridorEnemiesForRunForModule();
        flow.RefreshStatus();
    }

    internal void HandleSquadHudClearClicked(Level4FlowController flow)
    {
        if (flow == null || !flow.CurrentSectionIsFinal)
            return;

        if (flow.IsFinalDeploying)
        {
            flow.StatusOverride = "Сейчас идет развертывание отряда. Очистка временно недоступна.";
            flow.RefreshStatus();
            return;
        }

        if (flow.FinalRunStartedValue)
        {
            flow.StatusOverride = "Отряд уже в бою. Очистка временно отключена.";
            flow.RefreshStatus();
            return;
        }

        if (flow.FinalSquad.Count == 0 && flow.PlannedFinalSquad.Count == 0)
        {
            flow.StatusOverride = "Отряд пуст. Добавь роботов для запуска.";
            flow.RefreshStatus();
            return;
        }

        GameAudio.PlayUi(AudioCueIds.Level4SquadClear, 0.95f);

        flow.CleanupAttemptForModule(destroyPlayerRobot: true);
        BeginFinalAssembly(flow);
        flow.StatusOverride = "Отряд очищен. Собери новый состав из 5 роботов.";
        flow.RefreshStatus();
    }

    internal void UpdateFinalSection(Level4FlowController flow)
    {
        if (flow == null || !flow.FinalRunStartedValue)
            return;

        flow.UpdateFinalSquadSpacingForModule();
        flow.ApplyFinalSquadPulseIfNeededForModule(
            flow.CurrentSectionSquadPulseInterval,
            flow.CurrentSectionSquadPulseDamage,
            enabled: false);

        if (!flow.HasLivingFinalRobotForModule())
        {
            flow.FailCurrentSectionForModule(flow.CurrentSectionFailureText);
            return;
        }

        if (!flow.AreAllActiveEnemiesDefeatedForModule())
            return;

        if (flow.IsAllowedFinalCompositionForModule())
            flow.CompleteCurrentSectionForModule();
        else
            flow.FailCurrentSectionForModule(CompositionFailText);
    }
}
