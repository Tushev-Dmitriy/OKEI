using UnityEngine;
using UnityEngine.SceneManagement;

public class Door : MonoBehaviour, ISceneSaveable
{
    [SerializeField] private DoorOpener doorOpener;
    [SerializeField] private string saveId;
    [SerializeField] SceneObjectType objectType;
    [SerializeField] private bool isOpen;
    [SerializeField, Min(0f)] private float soundCooldown = 0.35f;

    private float _lastSoundTime = -999f;


    public bool OpenChange() => isOpen = !isOpen;
    public bool IsOpen => isOpen;

    public void SetOpen(bool open)
    { 
        if (isOpen == open)
        {
            return;
        }

        isOpen = open;
        bool isLevel3 = SceneManager.GetActiveScene().name == "Level3";
        string cueId = open
            ? (isLevel3 ? AudioCueIds.Level3DoorOpenLight : AudioCueIds.DoorOpenHeavy)
            : (isLevel3 ? AudioCueIds.Level3DoorCloseLight : AudioCueIds.DoorCloseHeavy);
        if (Time.time >= _lastSoundTime + soundCooldown)
        {
            _lastSoundTime = Time.time;
            GameAudio.PlayAtPoint(cueId, transform.position, 0.95f, 1.5f, 22f);
        }
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
