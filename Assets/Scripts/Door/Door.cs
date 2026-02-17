using UnityEngine;

public class Door : MonoBehaviour, ISceneSaveable
{
    [SerializeField] private DoorOpener doorOpener;
    [SerializeField] private string saveId;
    [SerializeField] SceneObjectType objectType;
    [SerializeField] private bool isOpen;


    public bool OpenChange() => isOpen = !isOpen;
    public bool IsOpen => isOpen;

    public void SetOpen(bool open)
    {
        if (isOpen == open)
        {
            return;
        }

        isOpen = open;
        ApplyInstant();
    }

    public string SaveId => saveId;

    public SceneObjectStateData CaptureState()
    {
        return new SceneObjectStateData
        {
            id = saveId,
            type = objectType,
            state = isOpen ? 1 : 0
        };
    }

    public void RestoreState(SceneObjectStateData data)
    {
        if (data.state == 1)
        {
            isOpen = true;
        } else
        {
            isOpen = false;
        }
        ApplyInstant();
    }

    public void ApplyInstant()
    {
        if (isOpen)
        {
            doorOpener.OpenDoors();
        }
        else
        {
            doorOpener.CloseDoors();
        }
    }
}
