using UnityEngine;

public class Door : MonoBehaviour, ISceneSaveable
{
    [SerializeField] private DoorOpener doorOpener;
    [SerializeField] private string saveId;
    [SerializeField] private bool isOpen;

    public bool OpenChange() => isOpen = !isOpen;

    public string SaveId => saveId;

    public SceneObjectStateData CaptureState()
    {
        return new SceneObjectStateData
        {
            id = saveId,
            type = SceneObjectType.Door,
            state = isOpen
        };
    }

    public void RestoreState(SceneObjectStateData data)
    {
        isOpen = data.state;
        ApplyInstant();
    }

    public void ApplyInstant()
    {
        if (isOpen)
            doorOpener.OpenDoors();
        else
            doorOpener.CloseDoors();
    }
}
