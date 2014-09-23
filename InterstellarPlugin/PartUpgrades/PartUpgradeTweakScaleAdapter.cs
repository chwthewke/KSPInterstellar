using TweakScale;
using UnityEngine;

namespace InterstellarPlugin.PartUpgrades
{
    public class PartUpgradeTweakScaleAdapter : IRescalable<UpgradeModule>
    {
        
        public void OnRescale(ScalingFactor factor)
        {
#if DEBUG
            Debug.Log(string.Format("Tweakscale applied to {0}.{1}", module.part.OriginalName(), module.moduleName));
#endif
            module.OnRescale(factor);
        }

        public PartUpgradeTweakScaleAdapter(UpgradeModule module)
        {
            this.module = module;
        }

        private UpgradeModule module;
    }
}