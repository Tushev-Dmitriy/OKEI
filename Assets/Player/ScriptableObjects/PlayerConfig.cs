using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Configs/Player")]
public class PlayerConfig : ScriptableObject
{
    public float moveSpeed;
    public float jumpForce;
    public Vector3 spawnPosition;
}
