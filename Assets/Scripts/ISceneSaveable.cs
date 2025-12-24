public interface ISceneSaveable
{
    string SaveId { get; }
    SceneObjectStateData CaptureState();
    void RestoreState(SceneObjectStateData data);
}
