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

        public bool IsUpgraded
        {
            get { return isUpgraded; }
            set
            {
                isUpgraded = value;
                UpgradePart();
            }
        }

        [KSPField(isPersistant = true)]
        public bool isUpgraded;

        [SerializeField]
        public List<PartUpgradeRequirement> requirements;

        [SerializeField]
        public List<PartUpgrade> upgrades;

        private static readonly IDictionary<string, PartUpgradeModule> loadedUpgradeModules =
            new Dictionary<string, PartUpgradeModule>();

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            requirements = node.GetNodes(RequirementsNode)
                .Select(n => PartUpgradeRequirements.CreateRequirement(this, n))
                .ToList();

            upgrades = node.GetNodes(UpgradeNode)
                .Select(n => new PartUpgrade(n))
                .ToList();

#if DEBUG
            Debug.Log("[Interstellar] Loaded " + this);
#endif
        }

        public override string ToString()
        {
            return string.Format("PartUpgradeModule for {0}, requirements [ {1} ], upgrades [ {2} ], isUpgraded = {3}.",
                part.name,
                string.Join(", ", requirements == null ? new[] { "null" } : requirements.Select(r => r.ToString()).ToArray()),
                string.Join(", ", upgrades == null ? new[] { "null" } : upgrades.Select(u => u.ToString()).ToArray()),
                isUpgraded);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

#if DEBUG
            Debug.Log("[Interstellar] OnStart " + this);
#endif

            foreach (var requirement in requirements)
                requirement.Start(this);

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
    [Serializable]
    public class PartUpgrade
    {
        private const string ModuleKey = "module";

        [SerializeField]
        public string module;

        [SerializeField]
        public List<PartUpgradeValue> upgradeValues = new List<PartUpgradeValue>();

        public string Module
        {
            get { return module; }
        }

        public PartUpgrade()
        {
        }

        public PartUpgrade(ConfigNode node)
            : this()
        {
            module = node.GetValue(ModuleKey);

            foreach (var value in node.values.OfType<ConfigNode.Value>().Where(value => value.name != ModuleKey))
            {
                upgradeValues.Add(new PartUpgradeValue(value.name, value.value));
            }
        }

        public void UpgradeModule(PartModule module)
        {
            if (module.moduleName != this.module)
                return;
            foreach (var field in upgradeValues)
            {
                module.Fields.ReadValue(field.name, field.value);
            }
        }

        public override string ToString()
        {
            return string.Format("PartUpgrade of {0}: [ {1} ]",
                module,
                string.Join(", ", upgradeValues.Select(uv => string.Format("{0} = {1}", uv.name, uv.value)).ToArray()));
        }

        [Serializable]
        public struct PartUpgradeValue
        {
            [SerializeField]
            public string name;
            [SerializeField]
            public string value;

            public PartUpgradeValue(string name, string value)
            {
                this.name = name;
                this.value = value;
            }
        }
    }
}
