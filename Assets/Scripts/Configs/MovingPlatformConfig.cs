using UnityEngine;

[CreateAssetMenu(fileName = "MovingPlatformConfig", menuName = "Configs/MovingPlatformConfig")]
public class MovingPlatformConfig : ScriptableObject
{
    public Vector3 moveOffset;
    public float moveDuration = 2f;
    public bool loop = true;
}
