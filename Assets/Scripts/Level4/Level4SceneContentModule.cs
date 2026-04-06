using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Level4SceneContentModule : MonoBehaviour
{
    internal void CacheSceneObjects(Level4FlowController flow)
    {
        if (flow == null)
            return;

        flow.SceneTransformsMap.Clear();
        flow.SceneEnemiesMap.Clear();
        flow.SceneEnemiesOrdered.Clear();
        flow.ClearEnemyRespawnCacheForModule();

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
                flow.SceneTransformsMap[candidate.name] = candidate;
            }
        }

        foreach (EnemyUnit enemy in Resources.FindObjectsOfTypeAll<EnemyUnit>())
        {
            if (enemy == null || !enemy.gameObject.scene.IsValid() || enemy.hideFlags != HideFlags.None)
                continue;

            string key = enemy.gameObject.name;
            if (string.IsNullOrWhiteSpace(key))
                key = $"Enemy_{flow.SceneEnemiesMap.Count}";

            if (flow.SceneEnemiesMap.ContainsKey(key))
                key = $"{key}_{flow.SceneEnemiesMap.Count}";

            enemy.SetDestroyOnDeath(false);
            flow.SceneEnemiesMap[key] = enemy;
            flow.SceneEnemiesOrdered.Add(enemy);
            flow.RememberEnemyBaseScaleForModule(enemy);
        }

        flow.SceneEnemiesOrdered.Sort((left, right) => left.transform.position.z.CompareTo(right.transform.position.z));
    }

    internal void PrepareStaticScene(Level4FlowController flow)
    {
        if (flow == null)
            return;

        flow.CloseAllSectionGatesForModule();
        flow.RestorePlacedSceneEnemiesForModule();
    }

    internal void EnsureRuntimeFinalSectionLayout(Level4FlowController flow)
    {
        if (flow == null)
            return;

        if (flow.SceneTransformsMap.ContainsKey("L4Room_Final") &&
            flow.SceneTransformsMap.ContainsKey("L4Spawn_Final") &&
            flow.SceneTransformsMap.ContainsKey("L4Goal_Final"))
        {
            return;
        }

        if (!flow.TryGetSceneTransformForModule("L4Room_Defender", out Transform defenderRoom) ||
            !flow.TryGetSceneTransformForModule("L4Spawn_Defender", out Transform defenderSpawn) ||
            !flow.TryGetSceneTransformForModule("L4Camera_Defender", out Transform defenderCamera) ||
            !flow.TryGetSceneTransformForModule("L4Goal_Defender", out Transform defenderGoal) ||
            !flow.TryGetSceneTransformForModule("L4Marker_Defender_MidA", out Transform defenderMidA) ||
            !flow.TryGetSceneTransformForModule("L4Marker_Defender_MidB", out Transform defenderMidB) ||
            !flow.TryGetSceneTransformForModule("SectionGate_Defender_1", out Transform defenderGate))
        {
            return;
        }

        Transform runtimeRoot = new GameObject("L4RuntimeFinalSection").transform;
        Vector3 firstOffset = new(0f, 0f, 36f);
        Vector3 secondOffset = new(0f, 0f, 72f);

        CloneRuntimeObject(flow, defenderRoom.gameObject, "L4Room_Final", defenderRoom.position + firstOffset, defenderRoom.rotation, runtimeRoot);
        CloneRuntimeObject(flow, defenderRoom.gameObject, "L4Room_Final_Ext", defenderRoom.position + secondOffset, defenderRoom.rotation, runtimeRoot);
        CloneRuntimeObject(flow, defenderGate.gameObject, "SectionGate_Final_1", defenderGate.position + firstOffset, defenderGate.rotation, runtimeRoot);

        CreateRuntimeSectionTransform(flow, "L4Spawn_Final", defenderSpawn.position + firstOffset, defenderSpawn.rotation, runtimeRoot);
        CreateRuntimeSectionTransform(flow, "L4Camera_Final", defenderCamera.position + new Vector3(0f, 0f, 54f), defenderCamera.rotation, runtimeRoot);
        CreateRuntimeSectionTransform(flow, "L4Marker_Final_MidA", defenderMidB.position + firstOffset, defenderMidB.rotation, runtimeRoot);
        CreateRuntimeSectionTransform(flow, "L4Marker_Final_MidB", defenderMidA.position + secondOffset, defenderMidA.rotation, runtimeRoot);
        CreateRuntimeSectionTransform(flow, "L4Goal_Final", defenderGoal.position + secondOffset, defenderGoal.rotation, runtimeRoot);

        float enemyY = 2.54f;
        if (flow.SceneEnemiesMap.TryGetValue("L4Enemy_Defender_A_1", out EnemyUnit defenderTemplate) && defenderTemplate != null)
            enemyY = defenderTemplate.transform.position.y;

        CreateRuntimeFinalEnemy(flow, "L4Enemy_Attacker_A_1", "L4Enemy_Final_A_1", new Vector3(2.5f, enemyY, 168f), runtimeRoot);
        CreateRuntimeFinalEnemy(flow, "L4Enemy_Attacker_A_2", "L4Enemy_Final_A_2", new Vector3(7.5f, enemyY, 170f), runtimeRoot);
        CreateRuntimeFinalEnemy(flow, "L4Enemy_Healer_A_1", "L4Enemy_Final_B_1", new Vector3(3f, enemyY, 185f), runtimeRoot);
        CreateRuntimeFinalEnemy(flow, "L4Enemy_Attacker_B_1", "L4Enemy_Final_B_2", new Vector3(7f, enemyY, 188f), runtimeRoot);
        CreateRuntimeFinalEnemy(flow, "L4Enemy_Defender_A_1", "L4Enemy_Final_C_1", new Vector3(5f, enemyY, 201f), runtimeRoot);
        CreateRuntimeFinalEnemy(flow, "L4Enemy_Defender_B_1", "L4Enemy_Final_C_2", new Vector3(5f, enemyY, 210f), runtimeRoot);
    }

    internal void BuildSections(Level4FlowController flow)
    {
        if (flow == null)
            return;

        flow.Sections.Clear();

        flow.Sections.Add(new SectionDefinition
        {
            Id = Level4FlowController.SectionId.Base,
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
            Header = "Секция 1. Базовый робот",
            TheoryText = "BaseRobot\\n- Move()\\n- TakeDamage()\\nБазовый класс задает общее поведение для всех роботов-наследников.",
            ReadyText = "Запусти базового робота. Он умеет двигаться и получать урон, но эта секция специально сделана так, чтобы он в итоге проиграл.",
            FailureText = "Базовый робот не завершил вводную секцию. Запусти его снова и посмотри, как работает общая логика.",
            SuccessText = "Секция базового робота пройдена. Теперь открыт первый дочерний класс.",
            ObjectiveTexts = new[]
            {
                "Шаг 1 из 3. Пройди по коридору до первой метки.",
                "Шаг 2 из 3. Пройди зону давления и покажи, что робот живет под уроном.",
                "Шаг 3 из 3. Дойди до охраны. После смерти базового робота откроется атакующий."
            }
        });

        flow.Sections.Add(new SectionDefinition
        {
            Id = Level4FlowController.SectionId.Attacker,
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
            Header = "Секция 2. Атакующий : BaseRobot",
            TheoryText = "AttackRobot : BaseRobot\\n- наследует: Move(), TakeDamage()\\n- добавляет: Attack()\\nНаследование сохраняет базовое поведение и добавляет боевую роль.",
            ReadyText = "Запусти атакующего. Здесь уже нужны боевые возможности, просто пройти не получится.",
            FailureText = "Атакующий не смог зачистить участок. В этой секции Attack() обязателен.",
            SuccessText = "Секция атакующего пройдена. Открывается следующая специализация.",
            ObjectiveTexts = new[]
            {
                "Шаг 1 из 3. Сломай первых врагов в коридоре.",
                "Шаг 2 из 3. Продави середину участка.",
                "Шаг 3 из 3. Дойди до терминала. Здесь решает Attack()."
            }
        });

        flow.Sections.Add(new SectionDefinition
        {
            Id = Level4FlowController.SectionId.Healer,
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
            Header = "Секция 3. Хилер : BaseRobot",
            TheoryText = "HealRobot : BaseRobot\\n- наследует: Move(), TakeDamage()\\n- добавляет: Heal()\\nНе каждый дочерний класс сильнее в уроне. У него может быть другая роль.",
            ReadyText = "Запусти хилера. Здесь важна выживаемость: Heal() должен компенсировать постоянный урон.",
            FailureText = "Секция хилера провалена. Запусти снова и удержи робота в живых.",
            SuccessText = "Секция хилера пройдена. Открывается защитный робот.",
            ObjectiveTexts = new[]
            {
                "Фаза 1. Пройди первую зону и покажи работу Heal().",
                "Фаза 2. Дойди до средней метки под постоянным давлением.",
                "Фаза 3. Доживи и дойди до выхода."
            }
        });

        flow.Sections.Add(new SectionDefinition
        {
            Id = Level4FlowController.SectionId.Defender,
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
            Header = "Секция 4. Защитник : BaseRobot",
            TheoryText = "DefenseRobot : BaseRobot\\n- наследует базовое поведение\\n- добавляет: Defend() через снижение урона и танкование\\nЭта секция показывает разницу ролей при одной базовой основе.",
            ReadyText = "Запусти защитника. Здесь постоянный урон, и нужна роль танка.",
            FailureText = "Защитник не удержал участок. Попробуй снова.",
            SuccessText = "Секция защитника пройдена. Режим отряда готов.",
            ObjectiveTexts = new[]
            {
                "Шаг 1 из 3. Войди в участок реактора и удержи вход.",
                "Шаг 2 из 3. Продави центр.",
                "Шаг 3 из 3. Дойди до выхода под уроном."
            }
        });

        flow.Sections.Add(new SectionDefinition
        {
            Id = Level4FlowController.SectionId.Final,
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
            Header = "Секция 5. Отряд наследников",
            TheoryText = "BaseRobot\\n- Move()\\n- TakeDamage()\\n\\nAttackRobot : BaseRobot\\n- Attack()\\nHealRobot : BaseRobot\\n- Heal()\\nDefenseRobot : BaseRobot\\n- Defend()\\n\\nОдин базовый класс, три роли-наследника, одна общая цель.",
            ReadyText = "Собери отряд из 5 роботов. Финал требует сбалансированный состав, а не спам одной роли.",
            FailureText = "Отряд не прошел финальный участок. Собери состав заново.",
            SuccessText = "Уровень пройден. Наследование показано через работу ролей в одном бою.",
            ObjectiveTexts = new[]
            {
                "Фаза 1 из 2. Собери ровно 5 роботов.",
                "Фаза 2 из 2. Зачисти весь коридор и сохрани хотя бы одного живым."
            }
        });
    }

    private Transform CloneRuntimeObject(Level4FlowController flow, GameObject source, string newName, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (flow == null || source == null)
            return null;

        GameObject clone = Instantiate(source, position, rotation, parent);
        clone.name = newName;
        clone.SetActive(true);
        flow.RuntimeFinalObjects.Add(clone);
        return clone.transform;
    }

    private Transform CreateRuntimeSectionTransform(Level4FlowController flow, string objectName, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (flow == null)
            return null;

        GameObject go = new(objectName);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(position, rotation);
        flow.RuntimeFinalObjects.Add(go);
        return go.transform;
    }

    private EnemyUnit CreateRuntimeFinalEnemy(Level4FlowController flow, string templateName, string enemyName, Vector3 position, Transform parent)
    {
        if (flow == null || !flow.SceneEnemiesMap.TryGetValue(templateName, out EnemyUnit template) || template == null)
            return null;

        GameObject clone = Instantiate(template.gameObject, position, template.transform.rotation, parent);
        clone.name = enemyName;
        clone.SetActive(false);
        flow.RuntimeFinalObjects.Add(clone);

        return clone.GetComponent<EnemyUnit>();
    }
}

