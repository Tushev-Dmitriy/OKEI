using UnityEngine;

public interface IMovingPlatform
{
    void Activate();
    void Deactivate();
    Vector3 Velocity { get; }
}
