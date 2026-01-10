using StarterAssets;
using UnityEngine;
using Zenject;

public class Level3PlayerInstaller : MonoInstaller
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private Vector3 _playerPos;
    public override void InstallBindings()
    {
        _playerPrefab.transform.position = _playerPos;
        Container.Bind<ThirdPersonController>()
            .FromComponentInNewPrefab(_playerPrefab)
            .AsSingle()
            .NonLazy();
    }
}
