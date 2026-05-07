using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Level3ArtifactManager : MonoBehaviour
{
    [Header("Artifacts")]
    [SerializeField] private List<Level3Artifact> _artifacts = new();
    [SerializeField] private bool _autoFindArtifactsIfListEmpty = true;

    [Header("Final Door")]
    [SerializeField] private FinalDoorController _finalDoorController;

    private readonly HashSet<Level3Artifact> _collectedArtifacts = new();
    private bool _finalDoorOpened;

    public int TotalArtifacts => _artifacts.Count;
    public int CollectedArtifacts => _collectedArtifacts.Count;

    private void Awake()
    {
        if (_autoFindArtifactsIfListEmpty && _artifacts.Count == 0)
        {
            _artifacts.AddRange(FindObjectsByType<Level3Artifact>(FindObjectsSortMode.None));
        }

        _artifacts.RemoveAll(artifact => artifact == null);
    }

    public void NotifyArtifactCollected(Level3Artifact artifact)
    {
        NotifyArtifactCollectedInternal(artifact, playSound: true);
    }

    public void NotifyArtifactRestored(Level3Artifact artifact)
    {
        NotifyArtifactCollectedInternal(artifact, playSound: false);
    }

    private void NotifyArtifactCollectedInternal(Level3Artifact artifact, bool playSound)
    {
        if (artifact == null)
        {
            return;
        }

        if (!_artifacts.Contains(artifact))
        {
            _artifacts.Add(artifact);
        }

        if (!_collectedArtifacts.Add(artifact))
        {
            return;
        }

        bool isLastArtifact = _artifacts.Count > 0 && _collectedArtifacts.Count >= _artifacts.Count;
        if (playSound)
        {
            Vector3 soundPosition = artifact.transform.position;
            if (isLastArtifact)
            {
                GameAudio.PlayAtPoint(AudioCueIds.Level3ArtifactLastPickup, soundPosition, 1f, 1f, 18f);
            }
            else
            {
                GameAudio.PlayRandomAtPoint(soundPosition, 0.95f, AudioCueIds.Level3ArtifactPickupVariants);
            }
        }

        if (_finalDoorOpened)
        {
            return;
        }

        if (_artifacts.Count == 0 || _collectedArtifacts.Count < _artifacts.Count)
        {
            return;
        }

        _finalDoorOpened = true;

        if (_finalDoorController != null)
        {
            _finalDoorController.Open(playSound);
            return;
        }

        Debug.LogWarning($"{nameof(Level3ArtifactManager)}: FinalDoorController is not assigned.", this);
    }

    public void DebugCollectAllArtifacts()
    {
        if (_autoFindArtifactsIfListEmpty && _artifacts.Count == 0)
        {
            _artifacts.AddRange(FindObjectsByType<Level3Artifact>(FindObjectsSortMode.None));
        }

        _artifacts.RemoveAll(artifact => artifact == null);

        foreach (Level3Artifact artifact in _artifacts)
        {
            artifact?.DebugCollect();
        }

        Debug.Log($"[{nameof(Level3ArtifactManager)}] Debug complete applied: collected {_collectedArtifacts.Count}/{_artifacts.Count} artifacts.");
    }
}
