using StarterAssets;
using UnityEngine;
using Zenject;

public class Level3PlayerInstaller : MonoInstaller
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private Vector3 _playerPos;
    [SerializeField] private Vector3 _playerScale = Vector3.one;
    public override void InstallBindings()
    {
        _playerPrefab.transform.position = _playerPos;
        _playerPrefab.transform.localScale = _playerScale;
        Container.Bind<ThirdPersonController>()
            .FromComponentInNewPrefab(_playerPrefab)
            .AsSingle()
            .NonLazy();
    }
}
