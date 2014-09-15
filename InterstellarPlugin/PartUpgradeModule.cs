using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TweakScale;
using UnityEngine;

namespace InterstellarPlugin
{
    public class PartUpgradeModule : PartModule
    {
        public const string RequirementsNode = "REQUIREMENT";

        [KSPField(isPersistant = true)]
        private bool isUpgraded;

        public bool IsUpgraded
        {
            get { return isUpgraded; }
            set
            {
                isUpgraded = value;
                UpgradePart();
            }
        }

        private readonly IList<PartUpgradeRequirement> requirements = new List<PartUpgradeRequirement>();



        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            foreach (var requirementNode in node.GetNodes(RequirementsNode))
            {
                requirements.Add(PartUpgradeRequirements.CreateRequirement(this, requirementNode));
            }

#if DEBUG
            Debug.Log(string.Format("PartUpgradeModule for {0} loaded with requirements {1}, isUpgraded = {2}.",
                part.partName, string.Join(", ", requirements.Select(r => r.ToString()).ToArray()), isUpgraded));
#endif
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            foreach (var requirement in requirements)
                requirement.OnStart();

            if (state == StartState.Editor)
                CheckRequirements();
        }

        public void CheckRequirements()
        {
            if (requirements.All(r => r.IsFulfilled()))
                IsUpgraded = true;
        }

        private void UpgradePart()
        {
            if (!IsUpgraded)
                return;
#if DEBUG
            Debug.Log(string.Format("Upgrade part {0}.", part.partName));
#endif
        }


        public class TweakScaleUpdater : IRescalable<PartUpgradeModule>
        {
            public TweakScaleUpdater(PartUpgradeModule module)
            {
                this.module = module;
            }

            public void OnRescale(ScalingFactor factor)
            {
                module.UpgradePart();
            }

            private readonly PartUpgradeModule module;
        }
    }
}
