using UnityEngine;

[CreateAssetMenu(fileName = "LockVisualConfig", menuName = "Level2/Lock Config/Visual", order = 50)]
public class LockVisualConfig : ScriptableObject
{
    [Header("Water Visual")]
    public float waterMinY = 3f;
    public float waterMaxY = 14f;

    [Header("Gate Visual")]
    public Vector3 gateOpenOffset = new Vector3(0f, 8f, 0f);
    public Vector3 gateOpenEulerOffset;
    public float gateOpenSpeed = 2.5f;
}
