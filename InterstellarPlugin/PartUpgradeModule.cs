using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TweakScale;

namespace InterstellarPlugin
{
    public class PartUpgradeModule: PartModule
    {
        public const string RequirementsNode = "REQUIREMENT";

        private IList<IPartUpgradeRequirement> requirements = new List<IPartUpgradeRequirement>(); 

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            node.GetNodes(RequirementsNode);


        }

        public class TweakScaleUpdater : IRescalable<PartUpgradeModule>
        {
            public TweakScaleUpdater(PartUpgradeModule module)
            {
                this.module = module;
            }

            public void OnRescale(ScalingFactor factor)
            {
                throw new NotImplementedException();
            }

            private readonly PartUpgradeModule module;
        }
    }
}
