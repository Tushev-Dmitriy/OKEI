using Zenject;
using UnityEngine;

public class RobotUnlockInstaller : MonoInstaller
{
    [SerializeField] private RobotUnlockManager robotUnlockManager;

    public override void InstallBindings()
    {
        Container.Bind<RobotUnlockEvents>()
            .AsSingle()
            .NonLazy();

        if (robotUnlockManager != null)
        {
            Container.Bind<RobotUnlockManager>()
                .FromInstance(robotUnlockManager)
                .AsSingle()
                .NonLazy();
        }
        else
        {
            Container.Bind<RobotUnlockManager>()
                .FromComponentInHierarchy()
                .AsSingle()
                .NonLazy();
        }

    }
}

