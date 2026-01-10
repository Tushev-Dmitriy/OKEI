public struct PlayerParamChangedSignal
{
    public PlayerParamType ParamType;
    public float Value;
}

public enum PlayerParamType
{
    JumpHeight,
    MoveSpeed,
    Gravity
}
