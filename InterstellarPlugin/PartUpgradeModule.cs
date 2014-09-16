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
        public const string UpgradeNode = "UPGRADE";

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

        private readonly IList<PartUpgrade> upgrades = new List<PartUpgrade>();


        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            foreach (var requirementNode in node.GetNodes(RequirementsNode))
            {
                requirements.Add(PartUpgradeRequirements.CreateRequirement(this, requirementNode));
            }

            foreach (var upgradeNode in node.GetNodes(UpgradeNode))
            {
                upgrades.Add(new PartUpgrade(upgradeNode));
            }

#if DEBUG
            Debug.Log(string.Format("[Interstellar] PartUpgradeModule for {0} loaded with requirements [ {1} ] " +
                                    "and upgrades [ {2} ], isUpgraded = {3}.",
                part.partName,
                string.Join(", ", requirements.Select(r => r.ToString()).ToArray()),
                string.Join(", ", upgrades.Select(u => u.ToString()).ToArray()),
                isUpgraded));
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
#if DEBUG
            Debug.Log(string.Format("[Interstellar] CheckRequirements {0}",
                string.Join(", ", requirements.Select(r => r.ToString()).ToArray())));
#endif
            IsUpgraded = requirements.All(CheckRequirement);
        }

        private bool CheckRequirement(PartUpgradeRequirement requirement)
        {
            bool fulfilled = requirement.IsFulfilled();
#if DEBUG
            Debug.Log(string.Format("[Interstellar] Upgrade Requirement {0}: {1}", requirement, fulfilled));
#endif
            return fulfilled;
        }

        internal void UpgradePart()
        {
            if (!IsUpgraded)
                return;
#if DEBUG
            Debug.Log(string.Format("[Interstellar] Upgrade part {0}.", part));
#endif

            foreach (var partUpgrade in upgrades)
            {
                UpgradeModule(partUpgrade);
            }
        }

        private void UpgradeModule(PartUpgrade partUpgrade)
        {
            foreach (var module in part.Modules
                .OfType<PartModule>())
            {
                partUpgrade.UpgradeModule(module);
            }
        }
    }

    public class TweakScaleUpdater : IRescalable<PartUpgradeModule>
    {
        public TweakScaleUpdater(PartUpgradeModule module)
        {
            this.module = module;
        }

        public void OnRescale(ScalingFactor factor)
        {
#if DEBUG
            Debug.Log(string.Format("[Interstellar] OnRescale, factor.absolute = {0}, factor.relative = {1}",
                factor.absolute.linear, factor.relative.linear));
#endif
            module.UpgradePart();
        }

        private readonly PartUpgradeModule module;
    }

    // TODO add exponent (if possible: default from original module, overridable)
    public class PartUpgrade
    {
        private const string ModuleKey = "module";

        private readonly string module;
        private readonly IDictionary<string, string> upgradeValues = new Dictionary<string, string>();

        public string Module
        {
            get { return module; }
        }

        public PartUpgrade(ConfigNode node)
        {
            module = node.GetValue(ModuleKey);

            foreach (var value in node.values.OfType<ConfigNode.Value>())
            {
                if (value.name == ModuleKey)
                    continue;
                upgradeValues[value.name] = value.value;
            }
        }

        public void UpgradeModule(PartModule module)
        {
            if (module.moduleName != this.module)
                return;
            foreach (var field in upgradeValues.Keys)
            {
                module.Fields.ReadValue(field, upgradeValues[field]);
            }
        }

        public override string ToString()
        {
            return string.Format("PartUpgrade of {0}: [ {1} ]",
                module,
                string.Join(", ", upgradeValues.Select(kv => string.Format("{0} = {1}", kv.Key, kv.Value)).ToArray()));
        }
    }
}