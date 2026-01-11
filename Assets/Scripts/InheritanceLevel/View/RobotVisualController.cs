using System.Collections.Generic;
using UnityEngine;

public class RobotVisualController : MonoBehaviour
{
    [System.Serializable]
    public struct ModuleBinding
    {
        public VisualModuleType type;
        public GameObject moduleObject;
    }

    [Header("Bindings")]
    [SerializeField] private List<ModuleBinding> modules;

    public void InitializeVisuals(RobotConfigSO config)
    {
        foreach (var binding in modules)
        {
            if (binding.moduleObject != null)
                binding.moduleObject.SetActive(false);
        }

        if (config.activeModules != null)
        {
            foreach (var moduleType in config.activeModules)
            {
                ActivateModule(moduleType);
            }
        }
    }

    private void ActivateModule(VisualModuleType type)
    {
        foreach (var binding in modules)
        {
            if (binding.type == type && binding.moduleObject != null)
            {
                binding.moduleObject.SetActive(true);
                return;
            }
        }
    }
}
