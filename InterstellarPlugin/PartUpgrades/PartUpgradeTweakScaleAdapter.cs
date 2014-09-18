using TweakScale;

namespace InterstellarPlugin.PartUpgrades
{
    public class PartUpgradeTweakScaleAdapter : IRescalable<UpgradeModule>
    {
        
        public void OnRescale(ScalingFactor factor)
        {
            module.UpdateUpgrades();
        }

        public PartUpgradeTweakScaleAdapter(UpgradeModule module)
        {
            this.module = module;
        }

        private UpgradeModule module;
    }
}