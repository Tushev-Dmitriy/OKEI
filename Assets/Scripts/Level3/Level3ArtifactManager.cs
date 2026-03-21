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
            _finalDoorController.Open();
            return;
        }

        Debug.LogWarning($"{nameof(Level3ArtifactManager)}: FinalDoorController is not assigned.", this);
    }
}
