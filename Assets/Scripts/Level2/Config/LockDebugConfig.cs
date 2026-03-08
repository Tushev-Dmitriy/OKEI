using UnityEngine;

[CreateAssetMenu(fileName = "LockDebugConfig", menuName = "Level2/Lock Config/Debug", order = 70)]
public class LockDebugConfig : ScriptableObject
{
    public bool showDebugCompleteButton = true;
    public bool allowDebugButtonInBuild;
    public Rect debugCompleteButtonRect = new Rect(20f, 20f, 220f, 36f);
    public KeyCode debugCompleteHotkey = KeyCode.F8;
    public string debugCompleteButtonLabel = "DEBUG: \u041f\u0440\u043e\u0439\u0442\u0438 \u0443\u0440\u043e\u0432\u0435\u043d\u044c";
    public float debugCompletePressureBonus = 5f;
    public float debugCompleteTemperatureBuffer = 5f;
    public float debugCompleteIntegrityFloor = 65f;
}
