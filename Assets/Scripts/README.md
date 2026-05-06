# Scripts Architecture

Этот документ описывает авторскую логику проекта в `Assets/Scripts`: что за что отвечает, как связаны уровни, где лежат основные точки входа и зачем нужен каждый скрипт.

Документ специально написан как рабочая карта проекта:
- для быстрого онбординга;
- для ручной навигации по коду;
- для использования ИИ без необходимости каждый раз перечитывать весь `Assets/Scripts`.

## Scope

Документ покрывает только `Assets/Scripts`.

Отдельно в проекте еще используются внешние или полу-внешние зависимости:
- `Assets/OtherAssets/StarterAssets/...` - контроллер игрока и базовый movement/camera runtime;
- `Assets/Inventory/...` - предметы и `VariableItemSpawn`;
- `Assets/Plugins/Zenject/...` - DI / сигналы;
- `DOTween`, `TMP`, `URP`, `Input System`.

Если нужно, для этих внешних частей лучше делать отдельный README, чтобы не смешивать авторский код и пакеты.

## High-Level Map

Основной сценарий проекта такой:

1. `Bootstrap` поднимает меню, настройки, выбор уровней и хранение мета-прогресса.
2. Игровые сцены используют `GameplaySaveManager` как общий runtime save/load слой.
3. `Player`, `UI`, `Door`, `Platforms`, `Bridge`, `Other` дают общие механики, которые переиспользуются между уровнями.
4. Каждый уровень имеет свою тематическую подсистему:
   - `Level1` - двери, условия, предметы, переменные;
   - `Level2` - шлюз, циклы `for/while`, корабль, системные параметры;
   - `Level3` - артефакты, терминалы, финальный портал;
   - `Level4` - роботы, наследование, боевка, squad mode.
5. `Installers`, `Signals`, `Interfaces`, `Configs` и часть `Controllers` склеивают зависимости и общий runtime.

## Scene-Oriented Overview

### Bootstrap / Main Menu

Главная цель bootstrap-слоя:
- показать меню;
- хранить открытые и завершенные уровни;
- дать переход в игровые сцены;
- применять базовые runtime-настройки вроде fullscreen/resolution.

Ключевые скрипты:
- `MainMenuController` - основной контроллер меню;
- `LevelProgressManager` - хранение meta-progress и completed flags;
- `LevelSelectPanelController`, `LevelCardUI`, `SceneButtonAction` - UI выбора уровня;
- `SettingsPanelController` - настройки;
- `GameplayPauseMenuController` - внутриигровое pause menu;
- `GameplayCursorPolicy` - политика курсора между gameplay/UI сценами;
- `GameplayDebugHotkeys` - `F9` debug-прохождение уровней.

### Level1

Тема уровня: условия, сравнения, значения и открытие дверей.

Сценарий:
- игрок взаимодействует с дверями и слотами;
- предметы / значения проверяются через условные выражения;
- при корректной комбинации дверь открывается;
- финальный портал завершает уровень и ставит completion в bootstrap-прогресс.

Ключевые подсистемы:
- `Door/*`
- `InventorySaver` / `VariableItemSaveSystem`
- `FinalPortal`

### Level2

Тема уровня: циклы `for` и `while` в виде управления шлюзовой системой.

Сценарий:
- корабль приходит в шлюз;
- игрок управляет давлением, температурой, водой, подъемом, питанием и охлаждением;
- `for` и `while` выражены через повторяемые/поддерживаемые действия;
- по завершению корабль уходит в финальную точку, уровень закрывается transition-ом.

Ключевые подсистемы:
- `Level2/LockControlSystem`
- `Level2/LockInputs`
- `Level2/LockUI`
- `Controllers/ShipController`

### Level3

Тема уровня: сбор артефактов и работа с параметрами игрока через терминалы.

Сценарий:
- игрок собирает артефакты;
- `Level3ArtifactManager` отслеживает общий прогресс;
- после сбора всех артефактов открывается финальная дверь;
- терминалы меняют runtime-параметры игрока;
- финальный портал завершает уровень и ставит completion.

Ключевые подсистемы:
- `Level3/*`
- `UI/TerminalController`
- `UI/*Slider.cs`
- `Signals/PlayerParamChangedSignal`

### Level4

Тема уровня: наследование, специализация роботов, squad mode.

Сценарий:
- игрок по очереди проходит секции базовым, атакующим, хилером и защитником;
- после defender открывается финальная секция отряда;
- в финальной фазе игрок набирает squad и запускает его в коридор;
- прогресс роботов, открытия типов и этапов уровня сохраняются между сессиями.

Ключевые подсистемы:
- `InheritanceLevel/*`
- `Level4/*`
- `UI/RobotSelectionUI`, `RobotWindowController`, `RobotUnlockHintUI`

## Core Runtime Systems

### Save / Load

Это одна из самых важных частей проекта.

Основной runtime save manager:
- `SaveSystem/GameplaySaveManager.cs`

Он отвечает за:
- автосейв каждые 10 секунд;
- сейв по выходу;
- сейв позиции/ротации игрока по сценам;
- сейв runtime-параметров игрока;
- сейв `ISceneSaveable` объектов;
- сейв robot unlock progress;
- восстановление всего этого после загрузки сцены.

Форматы данных:
- `SaveSystem/SaveData.cs`
- `SaveSystem/PlayerSaveSystem.cs`

Специализированные слои:
- `InventorySaveSystem.cs` - инвентарь;
- `VariableItemSaveSystem.cs` - собранные variable items;
- `InventorySaver.cs` - bridge между `ItemCollection` и save system;
- `SaveResetter.cs` - жесткий сброс gameplay/meta save.

Старый/legacy слой:
- `Player/PlayerSaver.cs`

Сейчас он в основном служит fallback-обвязкой. Основной путь в проекте уже через `GameplaySaveManager`.

### Player Runtime

Общая логика вокруг игрока, не считая внешнего `ThirdPersonController`, собрана в:
- `PlayerInteractor.cs` - взаимодействие с объектами;
- `FinalPortal.cs` - финальный портал с переходом и completion;
- `PlayerDefaultsPortal.cs` - сброс параметров игрока к дефолту после пересечения портала;
- `PlayerSaver.cs` - legacy/fallback save bridge.

Изменяемые параметры игрока:
- `MoveSpeed`
- `JumpHeight`
- `Gravity`
- `Size`

Они меняются через UI-слайдеры и передаются сигналом:
- `Signals/PlayerParamChangedSignal.cs`

### UI Runtime

Два крупных UI-направления:

1. Меню и мета-интерфейс (`Bootstrap/*`)
2. Уровневые/системные UI (`UI/*`, `Level2/LockUI`, `Level4SquadHudModule`)

Отдельные важные UI-механики:
- `TerminalController` / `TerminalWindowStyler` - терминалы уровня 3;
- `RobotSelectionUI`, `RobotWindowController`, `RobotUnlockHintUI` - интерфейс роботов уровня 4;
- `FloatingText*` - combat text;
- `InteractableTextConroller` - текстовые подсказки взаимодействия;
- `PreviewJumpController` - UI-визуализация прыжка/превью;
- slider-скрипты - binding между UI и параметрами игрока.

### Cursor / Input Policy

За курсор и UI/gameplay lock policy отвечают:
- `Bootstrap/GameplayCursorPolicy.cs`
- `Bootstrap/GameplayPauseMenuController.cs`
- `UI/TerminalController.cs`
- `Level2/LockControlSystem.cs` для принудительного свободного курсора на уровне 2

Это важный слой, потому что Level1/3 и Level2/4 работают по разной cursor-модели.

## Directory-by-Directory Reference

Ниже перечислены все скрипты из `Assets/Scripts` по папкам.

### `Bootstrap`

| Script | Purpose |
|---|---|
| `GameplayCursorPolicy.cs` | Единая политика lock/visibility курсора для игровых сцен. |
| `GameplayDebugHotkeys.cs` | `F9` quick-complete для уровней, включая debug-save flow. |
| `GameplayPauseMenuController.cs` | Pause menu в gameplay-сценах. |
| `LevelCardUI.cs` | Отдельная карточка уровня в меню. |
| `LevelProgressManager.cs` | Хранение completion/unlock state и last played level для bootstrap-меню. |
| `LevelSelectPanelController.cs` | Панель выбора уровня. |
| `MainMenuController.cs` | Главный контроллер bootstrap-сцены, переходы, меню, transition helpers. |
| `SceneButtonAction.cs` | Простая привязка UI-кнопки к загрузке сцены/действию. |
| `SettingsPanelController.cs` | Панель настроек меню. |
| `SimpleMenuAnimator.cs` | Простая анимация/переходы UI меню. |

### `Bridge`

| Script | Purpose |
|---|---|
| `AllBridgeController.cs` | Групповой контроллер мостов/bridge-секций. |
| `Bridge.cs` | Сохраняемый bridge-объект (`ISceneSaveable`). |
| `BridgePart.cs` | Локальная часть/сегмент bridge-механики. |

### `Configs`

| Script | Purpose |
|---|---|
| `MovingPlatformConfig.cs` | Конфиг для движущихся платформ. |
| `PlayerConfig.cs` | Базовый конфиг игрока. |

### `Controllers`

| Script | Purpose |
|---|---|
| `ShipController.cs` | Контроллер корабля на Level2: docking, water-follow, move-to-end, sink, save/restore. |
| `VCamController.cs` | Контроллер виртуальной камеры/ограничений камеры. |

### `Door`

| Script | Purpose |
|---|---|
| `Door.cs` | Сохраняемое состояние двери: open/close. |
| `DoorComparisonOperator.cs` | Операторы сравнений для условий двери. |
| `DoorCondition.cs` | Основная логика условной двери: UI, слоты, проверка выражений, открытие, save after success. |
| `DoorConditionClause.cs` | Один clause условного выражения двери. |
| `DoorConditionExpression.cs` | Корневое логическое выражение двери (`AND/OR` + clauses). |
| `DoorLogicalOperator.cs` | Тип логической композиции. |
| `DoorOpener.cs` | Физическое/анимационное открытие створок двери. |
| `DoorTextController.cs` | UI-текст двери: condition text, success/error console output. |
| `DoorValueType.cs` | Тип значения условия: строка/число. |
| `VerticalTriggerDoor.cs` | Простая триггерная вертикальная дверь, не связанная с системой условий. |

### `InheritanceLevel`

#### Root

| Script | Purpose |
|---|---|
| `RobotUnlockEvents.cs` | Event hub для unlock pipeline роботов. |
| `RobotUnlockInstaller.cs` | Zenject installer для robot unlock subsystem. |
| `RobotUnlockManager.cs` | Хранение открытых роботов, apply/capture progress, reset hotkey, unlock rules. |
| `RobotUnlockTrigger.cs` | Триггер, который может открыть тип робота. |

#### `Data`

| Script | Purpose |
|---|---|
| `RobotConfigSO.cs` | `ScriptableObject` с параметрами и иконками робота. |
| `RobotType.cs` | Enum типов роботов. |

#### `Environment`

| Script | Purpose |
|---|---|
| `ConveyorBelt.cs` | Конвейерное движение объектов/роботов. |
| `EnemyUnit.cs` | Враг для robot-combat runtime. |
| `PlatformExitTrigger.cs` | Триггер завершения/выхода для платформенных сегментов. |
| `RobotMoverPlatform.cs` | Платформа/маршрут для роботов. |
| `RobotSpawner.cs` | Спавн роботов, выбор активного типа, валидация запроса на spawn. |

#### `Logic`

| Script | Purpose |
|---|---|
| `AssaultRobot.cs` | Производный атакующий робот. |
| `CombatSystem.cs` | Базовая боевка/расчет урона. |
| `DefenderRobot.cs` | Производный защитный робот. |
| `HealerRobot.cs` | Производный хилер-робот. |
| `Health.cs` | Общее здоровье/урон/смерть для юнитов. |
| `Robot.cs` | Базовый класс робота: движение, жизнь, autonomous mode, бой. |

#### `View`

| Script | Purpose |
|---|---|
| `RobotVisualController.cs` | Визуальная часть/отображение состояния робота. |

### `Installers`

| Script | Purpose |
|---|---|
| `Level1PlayerInstaller.cs` | Спавн/DI игрока для Level1. |
| `Level3Installer.cs` | Установка signal bus и signal declarations для Level3. |
| `Level3PlayerInstaller.cs` | Спавн/DI игрока для Level3. |

### `Interactables`

| Script | Purpose |
|---|---|
| `InteractableAnimator.cs` | Простая анимация/реакция интерактивного объекта. |

### `Interfaces`

| Script | Purpose |
|---|---|
| `IChangeSlider.cs` | Контракт для UI-слайдеров, меняющих параметры игрока. |
| `IMovingPlatform.cs` | Контракт движущейся платформы. |
| `IPlayer.cs` | Общий контракт игрока. |
| `ISceneSaveable.cs` | Ключевой интерфейс save/load для объектов сцены. |

### `Level2`

| Script | Purpose |
|---|---|
| `LockControlInteractable.cs` | Обертка для взаимодействия игрока с элементами системы шлюза. |
| `LockControlSystem.cs` | Сердце Level2: фазы уровня, симуляция, инциденты, финал, save state. |
| `LockControlTypes.cs` | Общие enum/типы для Level2. |
| `LockInputs.cs` | Входы/переключатели шлюза, кнопки UI, immediate save on action. |
| `LockUI.cs` | UI отображения состояния шлюза. |
| `PrimitiveLockBuilder.cs` | Вспомогательная сборка шлюза из примитивов. |

#### `Level2/Config`

Это конфиги симуляции шлюза и вспомогательные reflection/apply утилиты.

| Script | Purpose |
|---|---|
| `LockConfigReflectionApplier.cs` | Применение конфигов к `LockControlSystem`. |
| `LockCoolingFaultIncidentConfig.cs` | Конфиг инцидента cooling fault. |
| `LockCoreConfig.cs` | Core-настройки системы. |
| `LockDebugConfig.cs` | Debug-параметры Level2. |
| `LockFlowSurgeIncidentConfig.cs` | Конфиг инцидента flow surge. |
| `LockIncidentTimelineConfig.cs` | Тайминги/расписание инцидентов. |
| `LockLevelConfig.cs` | Агрегирующий конфиг уровня. |
| `LockLiftJamIncidentConfig.cs` | Конфиг инцидента lift jam. |
| `LockPressureLeakIncidentConfig.cs` | Конфиг инцидента pressure leak. |
| `LockReloadConfig.cs` | Параметры рестарта/перезагрузки. |
| `LockSimulationConfig.cs` | Параметры физики/симуляции шлюза. |
| `LockTextConfig.cs` | Тексты/лейблы UI. |
| `LockVisualConfig.cs` | Визуальные параметры шлюза. |

### `Level3`

| Script | Purpose |
|---|---|
| `FinalDoorController.cs` | Открытие финальной двери после сбора артефактов. |
| `Level3Artifact.cs` | Один артефакт: pickup, visual state, scene save state. |
| `Level3ArtifactCutscene.cs` | Визуальная/cutscene-обвязка для артефактов. |
| `Level3ArtifactManager.cs` | Учет всех артефактов, открытие финальной двери, debug collect all. |

### `Level4`

`Level4FlowController` - центральный orchestration-класс. Остальные файлы в папке - выделенные модули по одной ответственности.

| Script | Purpose |
|---|---|
| `Level4AttemptFlowModule.cs` | Старт/сброс попытки секции. |
| `Level4CombatRuntimeModule.cs` | Runtime combat rules во время секции и squad run. |
| `Level4EffectsModule.cs` | FX и визуальные runtime-эффекты. |
| `Level4EnemyCorridorModule.cs` | Коридор врагов и их активация/деактивация. |
| `Level4FlowController.cs` | Главный state machine/controller Level4. |
| `Level4FlowEventsModule.cs` | Подписки на события спавна, прогресса, enemy death и т.д. |
| `Level4FlowSetupModule.cs` | Первичная инициализация сцены и восстановление прогресса. |
| `Level4FlowUpdateModule.cs` | Tick/update orchestration. |
| `Level4LocalizationModule.cs` | Локализованные названия/иконки/подсказки роботов. |
| `Level4ProgressModule.cs` | Сохранение и загрузка stage progress Level4. |
| `Level4SceneContentModule.cs` | Кодовая сборка секций/контента уровня. |
| `Level4SectionEventsModule.cs` | Реакция на завершение волн, смерти и события секции. |
| `Level4SectionLifecycleModule.cs` | Переходы между секциями, completion/fail, cleanup. |
| `Level4SectionNavigationModule.cs` | Выбор текущей секции и переключение layout/selection. |
| `Level4SpawnFlowModule.cs` | Валидация spawn-запросов и squad-mode spawn policy. |
| `Level4SquadCompositionModule.cs` | Проверка валидности состава squad. |
| `Level4SquadDeploymentModule.cs` | Набор отряда и последовательный деплой 5 роботов. |
| `Level4SquadHudModule.cs` | HUD состава отряда и его отображение. |
| `Level4SquadMovementModule.cs` | Движение squad и spacing логика. |
| `Level4StageFlowModule.cs` | Переходы внутри stage/этапов. |
| `Level4StatusModule.cs` | Статус-текст и человекочитаемые описания текущего состояния уровня. |

### `Other`

| Script | Purpose |
|---|---|
| `ActionObjectData.cs` | Вспомогательные данные для объектов-действий. |
| `CameraMoveController.cs` | Простое движение/управление камерой. |
| `RopePart.cs` | Логика сегмента веревки. |
| `SpawnVariableItems.cs` | Спавн variable items с учетом их save state. |

### `Platforms`

| Script | Purpose |
|---|---|
| `MovingPlatform.cs` | Обычная движущаяся платформа. |
| `PushPlatform.cs` | Платформа/объект, двигающий игрока или другие объекты по оси. |

### `Player`

| Script | Purpose |
|---|---|
| `FinalPortal.cs` | Финальный портал уровня: transition и completion в bootstrap progress. |
| `PlayerDefaultsPortal.cs` | Портал, который возвращает параметры игрока к default values. |
| `PlayerInteractor.cs` | Общая логика взаимодействия игрока с объектами. |
| `PlayerSaver.cs` | Legacy/fallback save bridge для игрока. |

### `SaveSystem`

| Script | Purpose |
|---|---|
| `GameplaySaveManager.cs` | Основной runtime save/load слой проекта. |
| `InventorySaver.cs` | Подключение `ItemCollection` к inventory/gameplay save. |
| `InventorySaveSystem.cs` | JSON save/load инвентаря. |
| `PlayerSaveSystem.cs` | Низкоуровневое чтение/запись основного save data JSON. |
| `SaveData.cs` | Структуры save data. |
| `SaveResetter.cs` | Полный сброс gameplay/meta progress. |
| `VariableItemSaveSystem.cs` | Save state собранных variable items. |

### `Signals`

| Script | Purpose |
|---|---|
| `PlayerParamChangedSignal.cs` | Сигнал изменения параметра игрока через Zenject SignalBus. |

### `UI`

| Script | Purpose |
|---|---|
| `GravitySlider.cs` | UI-слайдер gravity игрока + immediate save. |
| `InteractableTextConroller.cs` | Текстовые подсказки интерактивных объектов. |
| `JumpHeightSlider.cs` | UI-слайдер jump height игрока + immediate save. |
| `MoveSpeedSlider.cs` | UI-слайдер move speed игрока + immediate save. |
| `PreviewJumpController.cs` | UI/preview логика прыжка. |
| `RobotSelectionButton.cs` | Маркер типа робота на UI-кнопке выбора. |
| `RobotSelectionUI.cs` | Панель выбора роботов на Level4. |
| `RobotUnlockHintUI.cs` | Hint/popup об открытии робота и squad mode. |
| `RobotWindowController.cs` | Карточка/окно параметров выбранного робота. |
| `SizeSlider.cs` | UI-слайдер размера игрока + immediate save. |
| `TerminalController.cs` | Открытие/закрытие терминала, управление курсором, sync со slider UI. |
| `TerminalWindowStyler.cs` | Runtime-стилизация окон терминалов. |

#### `UI/CombatText`

| Script | Purpose |
|---|---|
| `FloatingTextBillboard.cs` | Поворот floating text к камере. |
| `FloatingTextSpawner.cs` | Спавн боевых текстов. |
| `FloatingTextType.cs` | Типы боевых текстов. |
| `FloatingTextView.cs` | Отображение одного floating text instance. |
| `SimpleObjectPool.cs` | Пул объектов для floating texts. |

## Practical Navigation Tips

Если нужно быстро править конкретный сценарий, обычно стоит идти так:

- меню / completion / галочки:
  `Bootstrap/MainMenuController.cs` + `Bootstrap/LevelProgressManager.cs` + `Player/FinalPortal.cs`

- сейвы:
  `SaveSystem/GameplaySaveManager.cs` + `SaveSystem/SaveData.cs` + конкретный `ISceneSaveable`

- Level1:
  `Door/`

- Level2:
  `Level2/LockControlSystem.cs` + `Level2/LockInputs.cs` + `Controllers/ShipController.cs`

- Level3:
  `Level3/Level3Artifact*.cs` + `UI/TerminalController.cs` + slider files

- Level4:
  `Level4/Level4FlowController.cs` + `Level4SectionLifecycleModule.cs` + `InheritanceLevel/`

- курсор и UI-blocking:
  `Bootstrap/GameplayCursorPolicy.cs` + `UI/TerminalController.cs` + `Level2/LockControlSystem.cs`

## Known Architectural Realities

Несколько вещей важно помнить при работе с этим проектом:

1. В проекте есть смешение legacy save-path (`PlayerSaver`) и нового общего save-path (`GameplaySaveManager`), но фактический основной путь уже новый.
2. `Level4` намеренно разбит на много модулей. Правки туда лучше делать через текущий flow, а не городить параллельный shortcut-path.
3. `Level3` параметры игрока меняются сигналами через Zenject и UI-слайдеры, а не напрямую через один центральный manager.
4. `Level2` завязан на stateful симуляцию, поэтому там критично сохранять не только фазы, но и ship state, inputs и incident state.
5. Значимая часть поведения сцены задается не только кодом, но и сериализованными значениями в `.unity` сценах.

## Recommended Next Docs

Если проект будет дальше расти, полезно завести еще 3 документа:

1. `Assets/Scenes/README.md`
   Ключевые scene objects, важные serialized references, порталы, spawn points.

2. `Assets/Scripts/SaveSystem/README.md`
   Полная схема save data, lifecycle restore, кто и когда обязан звать `SaveCurrentGame()`.

3. `Assets/Scripts/Level4/README.md`
   Отдельная карта flow-модулей Level4 и sequence переходов между секциями.
