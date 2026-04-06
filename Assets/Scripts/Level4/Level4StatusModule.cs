using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Level4StatusModule : MonoBehaviour
{
    private const string CompletedSuffix = "\n\nУровень пройден. Эта сцена показывает базовый класс, расширение через наследование, поддержку и защитную специализацию.";

    public void UpdateStatus(Level4FlowController flow)
    {
        if (flow == null)
            return;

        TMP_Text statusText = flow.StatusTextRef;
        if (statusText == null)
        {
            flow.RefreshSquadHud();
            return;
        }

        if (!flow.HasCurrentSection)
        {
            flow.SetStatusTextValue("Уровень 4 еще не инициализирован.");
            flow.RefreshSquadHud();
            return;
        }

        if (flow.CurrentSectionIsFinal)
        {
            int limit = flow.FinalSectionSpawnLimit;
            int total;
            if (flow.FinalRunStarted)
                flow.GetCommittedFinalCompositionCountsForModule(out _, out _, out _, out _, out total);
            else
                flow.GetFinalCompositionCountsForModule(out _, out _, out _, out _, out total);

            flow.SetStatusTextValue($"Выбрано: {total}/{limit}");
            flow.RefreshSquadHud();
            return;
        }

        string objective = flow.AttemptActive
            ? GetActiveObjectiveText(flow)
            : flow.CurrentSectionReadyText;

        string runtime = GetRuntimeStateText(flow);
        string message = $"{flow.CurrentSectionHeader}\n{flow.CurrentSectionTheoryText}\n\n{objective}";

        if (!string.IsNullOrWhiteSpace(runtime))
            message += $"\n\n{runtime}";

        if (!string.IsNullOrWhiteSpace(flow.StatusOverride))
            message = $"{flow.StatusOverride}\n\n{message}";

        if (flow.LevelCompleted)
            message += CompletedSuffix;

        flow.SetStatusTextValue(message);
        flow.RefreshSquadHud();
    }

    public string GetActiveObjectiveText(Level4FlowController flow)
    {
        if (flow == null)
            return string.Empty;

        string[] objectives = flow.CurrentSectionObjectiveTexts;
        if (objectives == null || objectives.Length == 0)
            return string.Empty;

        int objectiveIndex = Mathf.Clamp(flow.StageIndexValue, 0, objectives.Length - 1);
        return objectives[objectiveIndex];
    }

    public string GetRuntimeStateText(Level4FlowController flow)
    {
        if (flow == null || !flow.HasCurrentSection)
            return string.Empty;

        if (!flow.AttemptActive)
        {
            if (flow.CurrentSectionIsFinal)
                return "Режим отряда: создай 5 роботов из одной точки, после чего они вместе идут по коридору. Валидные составы: 2/2/1, 2/1/2 или 3/1/1 для Атаки/Хила/Защиты.";

            return $"Запусти {flow.GetRobotDisplayName(flow.CurrentSectionRequiredRobotType)}. Эта секция продвигается только после завершения его испытания и смерти.";
        }

        List<string> lines = new();

        if (flow.CurrentSectionIsFinal)
        {
            int limit = flow.FinalSectionSpawnLimit;
            if (flow.FinalRunStarted)
            {
                flow.GetCommittedFinalCompositionCountsForModule(out int attackers, out int healers, out int defenders, out int bases, out int total);
                lines.Add($"Размер отряда: {total}/{limit}");
                lines.Add($"Состав: База {bases}, Атака {attackers}, Хил {healers}, Защита {defenders}");
                lines.Add($"Живых роботов: {flow.CombatRuntime.GetLivingFinalRobotCount(flow)}");
                lines.Add(flow.IsAllowedFinalCompositionForModule()
                    ? "Проверка состава: корректный сбалансированный отряд."
                    : "Проверка состава: некорректный отряд для финального терминала.");
            }
            else
            {
                flow.GetFinalCompositionCountsForModule(out int attackers, out int healers, out int defenders, out int bases, out int total);
                lines.Add($"Размер отряда: {total}/{limit}");
                lines.Add($"Состав: База {bases}, Атака {attackers}, Хил {healers}, Защита {defenders}");
                lines.Add(total >= limit
                    ? "Отряд заполнен. Роботы выйдут по очереди в порядке кликов."
                    : $"Добавь еще {limit - total} робота(ов), чтобы запустить финальную секцию.");
            }
        }

        Robot player = flow.PlayerRobotRef;
        if (player != null && player.Health != null)
            lines.Add($"Текущий робот: {Mathf.CeilToInt(player.Health.CurrentHealth)}/{Mathf.CeilToInt(player.Health.MaxHealth)} ОЗ");

        Robot escort = flow.EscortRobotRef;
        if (escort != null && escort.Health != null)
            lines.Add($"Сопровождаемый союзник: {Mathf.CeilToInt(escort.Health.CurrentHealth)}/{Mathf.CeilToInt(escort.Health.MaxHealth)} ОЗ");

        if (flow.ActiveEnemies.Count > 0)
            lines.Add($"Активных врагов в секции: {flow.ActiveEnemies.Count}");

        if (flow.CurrentSectionIsDefender)
            lines.Add("В этой секции активировано реакторное давление.");
        else if (flow.CurrentSectionIsHealer && flow.StageIndexValue >= 1)
            lines.Add("Сопровождаемый союзник получает периодический урон, нужна стабильная поддержка.");
        else if (flow.CurrentSectionIsFinal && flow.FinalRunStarted)
            lines.Add("Давление фабрики тикает по всему отряду, поэтому здесь важны и Heal(), и Defend().");

        return string.Join("\n", lines);
    }
}
