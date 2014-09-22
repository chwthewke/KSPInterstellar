using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InterstellarPlugin.PartUpgrades
{
    public class UpgradeModule : PartModule
    {
        // Must be unique per part, used for persistence
        [KSPField]
        public string id;

        // Used for passing existing config from OnLoad to OnStart
        [KSPField(isPersistant = false)]
        public ConfigNode Config;

        // TODO move concern to PartUpgradeScenario
        [Obsolete]
        [KSPField(isPersistant = true)]
        public bool IsUnlocked = false;

        // Sets whether this upgrade is applied to the part.
        [KSPField(isPersistant = true)]
        public bool IsApplied = false;

        // What parts to automatically upgrade when first unlocking the tech
        // Possible values are None, Part, Vessel, All.
        [KSPField]
        public UnlockMode onUnlock = UnlockMode.None;

        // When true, all requirements must be met to unlock, otherwise a single requirement is enough
        [KSPField]
        public bool requireAllToUnlock = true;

        // When true, all retrofitting requirements must be met to retrofit, otherwise a single requirement is enough
        [KSPField]
        public bool requireAllToRetrofit = true;

        public override void OnLoad(ConfigNode node)
        {
            // AFAIK, this is more or less the only way to copy complex data structures from the loaded part prefab
            // to an instance in editor/flight (other that packing data to strings).
            Config = new ConfigNode();

            CopyNodes(node, UpgradeConfig.UpgradeKey, Config);
            CopyNodes(node, RequirementConfig.RequirementKey, Config);

            var retrofitNode = node.GetNode(RequirementConfig.RetrofitKey);
            var retrofitCopy = Config.AddNode(RequirementConfig.RetrofitKey);
            if (retrofitNode != null)
                CopyNodes(retrofitNode, RequirementConfig.RetrofitKey, retrofitCopy);
            
                
#if DEBUG
            Debug.Log(string.Format("[Interstellar] Loaded {0}.", this));
#endif
        }


        public override void OnStart(StartState state)
        {
            upgrades = Config.GetNodes(UpgradeConfig.UpgradeKey)
                .SelectMany((n, i) => new UpgradeConfig(part, n, i).Load())
                .ToList();
            unlockRequirements = LoadRequirements(Config);
            retrofitRequirements = LoadRequirements(Config.GetNode(RequirementConfig.RetrofitKey));

            if (state == StartState.Editor)
                CheckRequirements();
#if DEBUG
            Debug.Log(string.Format("[Interstellar] Started {0}.", this));
#endif

        }

        private List<UpgradeRequirement> LoadRequirements(ConfigNode requirementNode)
        {
            return requirementNode.GetNodes(RequirementConfig.RequirementKey)
                .SelectMany((n, i) => new RequirementConfig(part, n, i).Load()).ToList();
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            // TODO pass correct requirement node to each requirement for save.
        }

        public void CheckRequirements()
        {
            IsUnlocked = unlockRequirements.All(req => req.IsFulfilled());

            UpdateUpgrades();
        }

        public void UpdateUpgrades()
        {
            if (IsUnlocked)
                UpgradePart();
        }

        private void UpgradePart()
        {
            foreach (var upgrade in upgrades)
                upgrade.Apply();
        }

        public override string ToString()
        {
            return string.Format("{0} for {1}: upgrades = [{2}], unlockRequirements = [{3}], retrofitRequirements = {4}, config = <{5}>",
                GetType(), part.OriginalName(),
                upgrades == null ? "null" : string.Join(", ", upgrades.Select(o => o.ToString()).ToArray()),
                unlockRequirements == null ? "null" : string.Join(", ", unlockRequirements.Select(o => o.ToString()).ToArray()),
                retrofitRequirements == null ? "null" : string.Join(", ", retrofitRequirements.Select(o => o.ToString()).ToArray()),
                Config);
        }


        private static void CopyNodes(ConfigNode node, string name, ConfigNode target)
        {
            foreach (var upgradeNode in node.GetNodes(name))
                target.AddData(upgradeNode);
        }

        private IList<Upgrade> upgrades;
        private IList<UpgradeRequirement> unlockRequirements;
        private IList<UpgradeRequirement> retrofitRequirements;
    }
}