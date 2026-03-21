using StarterAssets;
using UnityEngine;
using Zenject;

public class Level3PlayerInstaller : MonoInstaller
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private Vector3 _playerPos;

    public override void InstallBindings()
    {
        Container.Bind<ThirdPersonController>()
            .FromComponentInNewPrefab(_playerPrefab)
            .AsSingle()
            .OnInstantiated<ThirdPersonController>((_, player) =>
            {
                player.transform.root.position = _playerPos;
            })
            .NonLazy();
    }
}
