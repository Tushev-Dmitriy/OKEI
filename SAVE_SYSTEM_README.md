# Система сохранений

Этот файл описывает, как в проекте `OKEI` работает система сохранений, какие JSON-файлы создаются, где они лежат и как выглядит их структура.

## Где лежат сохранения

Все основные файлы создаются в `Application.persistentDataPath`.

В билде Unity это отдельная папка приложения в системе пользователя.  
В редакторе путь тоже будет платформозависимым, но логика записи та же самая.

Основные файлы:

- `player.json` - основное игровое сохранение.
- `inventory.json` - состояние инвентаря игрока.
- `variable_items.json` - какие предметы-переменные уже были подобраны.
- `bootstrap_menu.json` - прогресс меню, открытые уровни и настройки bootstrap-сцены.

Дополнительно есть legacy/fallback-ключ в `PlayerPrefs`:

- `Level4RoomStage` - старый запасной способ хранить стадию Level4, если в `player.json` еще нет `gameplayProgress`.

## Что сохраняется и когда

Главный управляющий класс сохранений - [GameplaySaveManager.cs](/D:/GitRepos/OKEI/Assets/Scripts/SaveSystem/GameplaySaveManager.cs).

Он сохраняет:

- позицию и поворот игрока;
- позицию игрока отдельно для каждого уровня;
- runtime-параметры игрока: скорость, прыжок, гравитация, размер;
- настройки звука;
- служебную информацию о сохранении;
- состояния объектов сцены через `ISceneSaveable`;
- прогресс разблокировки роботов;
- прогресс Level4;
- инвентарь через `InventorySaver`.

Сохранение происходит:

- автоматически каждые `10` секунд;
- при выходе из приложения;
- при изменении инвентаря;
- при вызове `GameplaySaveManager.SaveCurrentGame()`.

## Общая схема

Логика разбита на несколько слоев:

1. [GameplaySaveManager.cs](/D:/GitRepos/OKEI/Assets/Scripts/SaveSystem/GameplaySaveManager.cs) собирает общее состояние игры.
2. [PlayerSaveSystem.cs](/D:/GitRepos/OKEI/Assets/Scripts/SaveSystem/PlayerSaveSystem.cs) пишет и читает `player.json`.
3. [InventorySaveSystem.cs](/D:/GitRepos/OKEI/Assets/Scripts/SaveSystem/InventorySaveSystem.cs) отдельно пишет и читает `inventory.json`.
4. [VariableItemSaveSystem.cs](/D:/GitRepos/OKEI/Assets/Scripts/SaveSystem/VariableItemSaveSystem.cs) отдельно пишет и читает `variable_items.json`.
5. [LevelProgressManager.cs](/D:/GitRepos/OKEI/Assets/Scripts/Bootstrap/LevelProgressManager.cs) через `BootstrapMenuSaveSystem` хранит меню и настройки в `bootstrap_menu.json`.

## `player.json`

Это главный файл сохранения. Его модель описана в [SaveData.cs](/D:/GitRepos/OKEI/Assets/Scripts/SaveSystem/SaveData.cs).

### Структура

```json
{
  "player": {
    "level": "Level2",
    "position": {
      "x": 12.5,
      "y": 1.0,
      "z": -8.25
    },
    "rotation": {
      "x": 0.0,
      "y": 90.0,
      "z": 0.0
    }
  },
  "playerLevels": [
    {
      "level": "Level1",
      "position": {
        "x": 2.0,
        "y": 0.0,
        "z": 5.0
      },
      "rotation": {
        "x": 0.0,
        "y": 180.0,
        "z": 0.0
      }
    },
    {
      "level": "Level2",
      "position": {
        "x": 12.5,
        "y": 1.0,
        "z": -8.25
      },
      "rotation": {
        "x": 0.0,
        "y": 90.0,
        "z": 0.0
      }
    }
  ],
  "playerRuntime": {
    "moveSpeed": 2.0,
    "jumpHeight": 1.2,
    "gravity": -15.0,
    "size": 1.0
  },
  "settings": {
    "musicVolume": 0.5,
    "sfxVolume": 0.5
  },
  "saveInfo": {
    "saveVersion": "1.1",
    "lastSaveTime": "2026-05-07T20:15:22.1234567+05:00"
  },
  "robotProgress": {
    "unlockedRobotTypes": [1, 2, 3]
  },
  "gameplayProgress": {
    "level4ProgressStage": 2
  },
  "sceneObjects": [
    {
      "sceneName": "Level1",
      "id": "door1",
      "type": 0,
      "state": 1,
      "json": null
    },
    {
      "sceneName": "Level3",
      "id": "Level3Artifact:RoomA/Artifact_01",
      "type": 5,
      "state": 1,
      "json": null
    },
    {
      "sceneName": "Level2",
      "id": "Level2.ShipController",
      "type": 6,
      "state": 1,
      "json": "{\"position\":{\"x\":0.0,\"y\":2.5,\"z\":10.0},\"rotation\":{\"x\":0.0,\"y\":180.0,\"z\":0.0},\"hasStopped\":true,\"isSinking\":false,\"floatOffsetCaptured\":true,\"floatOffsetY\":0.35,\"motionState\":0,\"hasReachedEnd\":false}"
    }
  ]
}
```

### Поля

- `player` - последнее сохраненное положение игрока в текущей сцене.
- `playerLevels` - отдельная точка сохранения игрока для каждой сцены `LevelX`.
- `playerRuntime` - runtime-параметры `ThirdPersonController`.
- `settings` - внутренние настройки сейва.
- `saveInfo` - версия и дата последнего сохранения.
- `robotProgress` - разблокированные типы роботов.
- `gameplayProgress` - отдельный прогресс Level4.
- `sceneObjects` - список объектов сцены, которые реализуют `ISceneSaveable`.

### Какие значения у `type`

Enum объявлен в [SaveData.cs](/D:/GitRepos/OKEI/Assets/Scripts/SaveSystem/SaveData.cs):

- `0` - `Door`
- `1` - `Lever`
- `2` - `Bridge`
- `3` - `Platform`
- `4` - `VariableItem`
- `5` - `Artifact`
- `6` - `Ship`

### Как понимать `sceneObjects[].state`

Значение `state` зависит от конкретного объекта:

- для двери: `0` = закрыта, `1` = открыта;
- для артефакта: `0` = не собран, `1` = собран;
- для моста: процент открытия;
- для корабля: упрощенное состояние, где `1` обычно значит `hasStopped`, а детальная информация лежит в `json`;
- для шлюза Level2: в `state` лежит текущая фаза `LockPhase`.

### Как понимать `sceneObjects[].json`

Поле `json` заполнено не всегда.

Оно используется только там, где одного числа `state` недостаточно. В текущем проекте это точно:

- `ShipController`;
- `LockControlSystem`.

## Примеры `sceneObjects`

### Дверь

Источник: [Door.cs](/D:/GitRepos/OKEI/Assets/Scripts/Door/Door.cs)

```json
{
  "sceneName": "Level1",
  "id": "door4",
  "type": 0,
  "state": 1,
  "json": null
}
```

### Мост

Источник: [Bridge.cs](/D:/GitRepos/OKEI/Assets/Scripts/Bridge/Bridge.cs)

```json
{
  "sceneName": "Level1",
  "id": "bridge_main",
  "type": 2,
  "state": 65,
  "json": null
}
```

Здесь `state` - это `openPercent`.

### Артефакт Level3

Источник: [Level3Artifact.cs](/D:/GitRepos/OKEI/Assets/Scripts/Level3/Level3Artifact.cs)

```json
{
  "sceneName": "Level3",
  "id": "Level3Artifact:Artifacts/Artifact_A",
  "type": 5,
  "state": 1,
  "json": null
}
```

### Корабль Level2

Источник: [ShipController.cs](/D:/GitRepos/OKEI/Assets/Scripts/Controllers/ShipController.cs)

```json
{
  "sceneName": "Level2",
  "id": "Level2.ShipController",
  "type": 6,
  "state": 1,
  "json": "{\"position\":{\"x\":0.0,\"y\":2.5,\"z\":10.0},\"rotation\":{\"x\":0.0,\"y\":180.0,\"z\":0.0},\"hasStopped\":true,\"isSinking\":false,\"floatOffsetCaptured\":true,\"floatOffsetY\":0.35,\"motionState\":0,\"hasReachedEnd\":false}"
}
```

Внутри строки `json` лежит объект такого вида:

```json
{
  "position": {
    "x": 0.0,
    "y": 2.5,
    "z": 10.0
  },
  "rotation": {
    "x": 0.0,
    "y": 180.0,
    "z": 0.0
  },
  "hasStopped": true,
  "isSinking": false,
  "floatOffsetCaptured": true,
  "floatOffsetY": 0.35,
  "motionState": 0,
  "hasReachedEnd": false
}
```

`motionState`:

- `0` - `Idle`
- `1` - `MovingToStop`
- `2` - `MovingToEnd`
- `3` - `Sinking`

### Шлюз Level2

Источник: [LockControlSystem.cs](/D:/GitRepos/OKEI/Assets/Scripts/Level2/LockControlSystem.cs)

```json
{
  "sceneName": "Level2",
  "id": "Level2.LockControlSystem",
  "type": 3,
  "state": 1,
  "json": "{\"phase\":1,\"systemIntegrity\":74.0,\"pressure\":72.0,\"temperature\":18.0,\"waterLevel\":35.5,\"liftPower\":10.0,\"stabilizationTimer\":42.0,\"phase2EmergencyTimer\":0.0,\"failureTriggered\":false,\"gateOpening\":false,\"gameplayStarted\":true,\"sessionTimer\":53.2,\"nextIncidentTimer\":12.4,\"threatTier\":1,\"activeIncident\":0,\"incidentTimer\":0.0,\"incidentDuration\":0.0,\"incidentResolveProgress\":0.0,\"incidentLabel\":\"Нет\",\"incidentHint\":\"\",\"coolingRestartRequiresOff\":false,\"coolingRestartRequiresOn\":false,\"previousCoolingState\":true,\"powerEnabled\":true,\"coolingEnabled\":true,\"safeModeEnabled\":true,\"inputEnabled\":true}"
}
```

Внутри строки `json` лежит расширенное состояние шлюза:

```json
{
  "phase": 1,
  "systemIntegrity": 74.0,
  "pressure": 72.0,
  "temperature": 18.0,
  "waterLevel": 35.5,
  "liftPower": 10.0,
  "stabilizationTimer": 42.0,
  "phase2EmergencyTimer": 0.0,
  "failureTriggered": false,
  "gateOpening": false,
  "gameplayStarted": true,
  "sessionTimer": 53.2,
  "nextIncidentTimer": 12.4,
  "threatTier": 1,
  "activeIncident": 0,
  "incidentTimer": 0.0,
  "incidentDuration": 0.0,
  "incidentResolveProgress": 0.0,
  "incidentLabel": "Нет",
  "incidentHint": "",
  "coolingRestartRequiresOff": false,
  "coolingRestartRequiresOn": false,
  "previousCoolingState": true,
  "powerEnabled": true,
  "coolingEnabled": true,
  "safeModeEnabled": true,
  "inputEnabled": true
}
```

`phase` и `state` соответствуют `LockPhase` из [LockControlTypes.cs](/D:/GitRepos/OKEI/Assets/Scripts/Level2/LockControlTypes.cs):

- `0` - `Stabilization`
- `1` - `WaterLeveling`
- `2` - `LiftPreparation`
- `3` - `Completed`
- `4` - `Failed`

## `inventory.json`

Этот файл хранится отдельно от `player.json` и собирается через `DevionGames.InventorySystem`.

Источник:

- [InventorySaveSystem.cs](/D:/GitRepos/OKEI/Assets/Scripts/SaveSystem/InventorySaveSystem.cs)
- [InventorySaver.cs](/D:/GitRepos/OKEI/Assets/Scripts/SaveSystem/InventorySaver.cs)
- [ItemCollection.cs](/D:/GitRepos/OKEI/Assets/OtherAssets/Devion%20Games/Inventory%20System/Scripts/Runtime/ItemCollection.cs)
- [Item.cs](/D:/GitRepos/OKEI/Assets/OtherAssets/Devion%20Games/Inventory%20System/Scripts/Runtime/Items/Item.cs)

### Общий вид

```json
{
  "Prefab": "Inventory",
  "Position": [0.0, 0.0, 0.0],
  "Rotation": [0.0, 0.0, 0.0],
  "Type": "UI",
  "Items": [
    {
      "Name": "KeyCard",
      "Stack": 1,
      "RarityIndex": 0,
      "Index": 0,
      "Properties": [
        {
          "Name": "Code",
          "Value": "A1"
        }
      ],
      "Slots": [0],
      "Reference": []
    },
    {
      "Name": "Battery",
      "Stack": 3,
      "RarityIndex": 1,
      "Index": 1,
      "Properties": [],
      "Slots": [1]
    }
  ]
}
```

### Поля корня

- `Prefab` - имя объекта инвентаря.
- `Position` - позиция объекта коллекции.
- `Rotation` - поворот объекта коллекции.
- `Type`:
  - `UI` - если это UI-контейнер;
  - `Trigger` - если это объект мира.
- `Items` - список предметов.

### Поля предмета

- `Name` - имя предмета. По нему система ищет шаблон в базе `InventoryManager.Database`.
- `Stack` - размер стака.
- `RarityIndex` - индекс редкости в базе Devion.
- `Index` - индекс слота.
- `Properties` - сериализуемые свойства предмета.
- `Slots` - индексы связанных слотов.
- `Reference` - ссылки на другие контейнеры и слоты, если они есть.

### Важное ограничение

Формат `inventory.json` частично зависит от `DevionGames.InventorySystem`.  
То есть проект гарантированно использует поля:

- `Items`
- `Items[].Name`

а остальные поля определяются реальной структурой `ItemCollection` и `Item`.

## `variable_items.json`

Этот файл хранит факт подбора предметов-переменных, чтобы они не появлялись снова.

Источник:

- [VariableItemSaveSystem.cs](/D:/GitRepos/OKEI/Assets/Scripts/SaveSystem/VariableItemSaveSystem.cs)
- [VariableItemSpawn.cs](/D:/GitRepos/OKEI/Assets/Inventory/Scripts/VariableItemSpawn.cs)

### Структура

```json
{
  "entries": [
    {
      "id": "Level1::VariableItem::Int::5::12.00:1.00:3.50",
      "state": 1
    },
    {
      "id": "Level1::VariableItem::String::hello::15.00:1.00:7.25",
      "state": 0
    }
  ]
}
```

### Поля

- `id` - уникальный id предмета.
- `state`:
  - `0` - не собран;
  - `1` - собран.

### Как формируется `id`

Если `_saveId` не задан вручную, `VariableItemSpawn` строит его так:

```text
{SceneName}::VariableItem::{Type}::{Value}::{X}:{Y}:{Z}
```

Пример:

```text
Level1::VariableItem::Int::5::12.00:1.00:3.50
```

## `bootstrap_menu.json`

Этот файл не относится к игровому миру напрямую. Он нужен для меню, настроек и разблокировки уровней.

Источник: [LevelProgressManager.cs](/D:/GitRepos/OKEI/Assets/Scripts/Bootstrap/LevelProgressManager.cs)

### Структура

```json
{
  "maxUnlockedLevel": 3,
  "lastPlayedLevel": 2,
  "completedLevels": [1, 2],
  "soundVolume": 0.8,
  "qualityIndex": 2,
  "fullscreen": true,
  "resolutionIndex": 0
}
```

### Поля

- `maxUnlockedLevel` - максимальный открытый уровень.
- `lastPlayedLevel` - последний уровень, на котором играл пользователь.
- `completedLevels` - список завершенных уровней.
- `soundVolume` - общая громкость меню/bootstrap.
- `qualityIndex` - индекс качества Unity.
- `fullscreen` - полноэкранный режим.
- `resolutionIndex` - индекс разрешения из списка доступных разрешений.

## Как загружается прогресс Level4

Источник: [Level4ProgressModule.cs](/D:/GitRepos/OKEI/Assets/Scripts/Level4/Level4ProgressModule.cs)

При загрузке Level4 система делает так:

1. Пытается взять `gameplayProgress.level4ProgressStage` из `player.json`.
2. Если этого блока нет, берет fallback из `PlayerPrefs` по ключу `Level4RoomStage`.
3. Потом дополнительно ограничивает стадию по реально открытым роботам.

Структура блока в `player.json`:

```json
{
  "gameplayProgress": {
    "level4ProgressStage": 2
  }
}
```

## Как загружаются объекты сцены

Любой объект, который должен сохраняться, реализует интерфейс [ISceneSaveable.cs](/D:/GitRepos/OKEI/Assets/Scripts/Interfaces/ISceneSaveable.cs):

```csharp
public interface ISceneSaveable
{
    string SaveId { get; }
    SceneObjectStateData CaptureState();
    void RestoreState(SceneObjectStateData data);
}
```

Во время сохранения `GameplaySaveManager`:

- находит все `MonoBehaviour`, которые реализуют `ISceneSaveable`;
- вызывает `CaptureState()`;
- добавляет `sceneName`;
- сохраняет результат в `player.json`.

Во время загрузки:

- читает список `sceneObjects`;
- отбирает состояния только для активной сцены;
- сопоставляет их по `SaveId`;
- вызывает `RestoreState(...)`.

## Сброс сохранений

Источник: [SaveResetter.cs](/D:/GitRepos/OKEI/Assets/Scripts/SaveSystem/SaveResetter.cs)

### `ResetGameplayProgress()`

Удаляет:

- `player.json`
- `inventory.json`
- `variable_items.json`
- `PlayerPrefs["Level4RoomStage"]`

### `ResetSaves()`

Удаляет все выше и дополнительно:

- `bootstrap_menu.json`

## Версии и совместимость

Сейчас актуальная версия, которую пишет `GameplaySaveManager`, это:

```json
{
  "saveInfo": {
    "saveVersion": "1.1"
  }
}
```

Но в проекте остался старый путь сохранения через [PlayerSaver.cs](/D:/GitRepos/OKEI/Assets/Scripts/Player/PlayerSaver.cs), который может записать:

```json
{
  "saveInfo": {
    "saveVersion": "1.0"
  }
}
```

Это важно учитывать, если позже появится миграция форматов.

## Краткий итог

Если смотреть на систему целиком, то сейчас в проекте используются четыре основных JSON-файла и один fallback через `PlayerPrefs`:

- `player.json` - главный прогресс игры и состояние сцены;
- `inventory.json` - инвентарь;
- `variable_items.json` - собранные variable items;
- `bootstrap_menu.json` - меню, настройки и разблокировка уровней;
- `PlayerPrefs["Level4RoomStage"]` - запасной старый прогресс Level4.

Если захочешь, следующим сообщением я могу еще сделать вторую версию этого файла в формате:

- совсем короткая документация для команды;
- техническая документация для разработчиков;
- таблица `файл -> поля -> кто пишет -> кто читает`.
