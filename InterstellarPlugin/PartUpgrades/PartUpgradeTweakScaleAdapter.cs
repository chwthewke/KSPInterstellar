using TweakScale;

namespace InterstellarPlugin.PartUpgrades
{
    public class PartUpgradeTweakScaleAdapter : IRescalable<UpgradeModule>
    {
        
        public void OnRescale(ScalingFactor factor)
        {
            PartUpgrades.LogDebug(() => string.Format("Tweakscale applied to {0}.{1}", module.part.OriginalName(), module.moduleName));
            module.OnRescale(factor.absolute.linear);
        }

        public PartUpgradeTweakScaleAdapter(UpgradeModule module)
        {
            this.module = module;
        }

        private UpgradeModule module;
    }
}