using UnityEngine;

[DisallowMultipleComponent]
public sealed class Level4LocalizationModule : MonoBehaviour
{
    public string GetRobotName(Level4FlowController flow, RobotType robotType)
    {
        if (flow == null || flow.UnlockManager == null)
            return robotType.ToString();

        RobotConfigSO config = flow.UnlockManager.GetRobotConfig(robotType);
        return config != null && !string.IsNullOrWhiteSpace(config.robotName)
            ? config.robotName
            : robotType.ToString();
    }

    public Sprite GetRobotIcon(Level4FlowController flow, RobotType robotType)
    {
        if (flow == null || flow.UnlockManager == null)
            return null;

        RobotConfigSO config = flow.UnlockManager.GetRobotConfig(robotType);
        return config != null ? config.robotIcon : null;
    }

    public void NormalizeLocalizedHintText(Level4FlowController flow)
    {
        if (flow == null)
            return;

        flow.SquadUnlockedHintText = EnsureRussianHintText(flow.SquadUnlockedHintText, flow.SquadUnlockedHintDefaultText);
        flow.SquadReminderHintText = EnsureRussianHintText(flow.SquadReminderHintText, flow.SquadReminderHintDefaultText);
    }

    public static string EnsureRussianHintText(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        bool hasCyrillic = false;
        bool hasLatin = false;

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if ((c >= 'À' && c <= 'ÿ') || c == '¨' || c == '¸')
                hasCyrillic = true;

            if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                hasLatin = true;
        }

        if (hasLatin && !hasCyrillic)
            return fallback;

        return value.Trim();
    }
}
