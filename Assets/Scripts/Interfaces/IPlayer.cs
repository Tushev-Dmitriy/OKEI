using UnityEngine;

public interface IPlayer
{
    void Move(Vector2 direction, Vector3 platformVelocity);
    void Jump();
}
