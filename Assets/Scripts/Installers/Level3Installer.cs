using StarterAssets;
using UnityEngine;
using Zenject;

public class Level3Installer : MonoInstaller
{
    public override void InstallBindings()
    {
        SignalBusInstaller.Install(Container);

        Container.DeclareSignal<PlayerParamChangedSignal>();
    }
}
