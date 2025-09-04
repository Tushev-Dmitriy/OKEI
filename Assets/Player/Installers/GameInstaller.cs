using Cinemachine;
using StarterAssets;
using UnityEngine;
using Zenject;

public class GameInstaller : MonoInstaller
{
    [SerializeField] private GameObject _playerPrefab;

    public override void InstallBindings()
    {
        Container.Bind<ThirdPersonController>()
            .FromComponentInNewPrefab(_playerPrefab)
            .AsSingle()
            .NonLazy();
    }
}
