using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace InterstellarPlugin
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT, GameScenes.EDITOR, GameScenes.SPH)]
    public class PartUpgradeScenario : ScenarioModule
    {
        public const string ScenarioName = "KspilPartUpgrades";

        internal const string UnlockedRequirementNode = "UNLOCKED";

        private readonly ICollection<FulfilledRequirement> fulfilledRequirements =
            new HashSet<FulfilledRequirement>();

        private static PartUpgradeScenario instance;

        public static PartUpgradeScenario Instance
        {
            get { return instance; }
        }

        public override void OnAwake()
        {
            if (instance != null)
                return;
            instance = this;
        }

        public void FulfillRequirement(FulfilledRequirement requirement)
        {
            fulfilledRequirements.Add(requirement);
        }

        public bool IsFulfilled(FulfilledRequirement requirement)
        {
            return fulfilledRequirements.Contains(requirement);
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            node.ClearData();

            foreach (var unlockedRequirement in fulfilledRequirements)
            {
                node.AddNode(unlockedRequirement.Node);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            foreach (var requirementNode in node.GetNodes(UnlockedRequirementNode))
            {
                fulfilledRequirements.Add(new FulfilledRequirement(requirementNode));
            }

#if DEBUG
            Debug.Log("[Interstellar] PartUpgradeScenario loaded with fulfilled requirements: " +
            string.Join(", ", fulfilledRequirements.Select(r => r.ToString()).ToArray()));
#endif
        }
    }

    public struct FulfilledRequirement
    {
        private const string PartKey = "part";
        private const string IdKey = "id";

        private readonly string partName;
        private readonly string requirementId;

        public FulfilledRequirement(string partName, string requirementId)
        {
            this.partName = partName;
            this.requirementId = requirementId;
        }

        // TODO validate null or empty, warn, or ConfigNode.Load
        public FulfilledRequirement(ConfigNode node)
        {
            partName = node.GetValue(PartKey);
            requirementId = node.GetValue(IdKey);
        }

        public ConfigNode Node
        {
            get
            {
                var configNode = new ConfigNode(PartUpgradeScenario.UnlockedRequirementNode);
                configNode.AddValue(PartKey, partName);
                configNode.AddValue(IdKey, requirementId);
                return configNode;
            }
        }

        public string PartName
        {
            get { return partName; }
        }

        public string RequirementId
        {
            get { return requirementId; }
        }

        public bool Equals(FulfilledRequirement other)
        {
            return string.Equals(partName, other.partName) && string.Equals(requirementId, other.requirementId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is FulfilledRequirement && Equals((FulfilledRequirement)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (partName.GetHashCode() * 397) ^ requirementId.GetHashCode();
            }
        }

        public override string ToString()
        {
            return string.Format("Requirement {0} for part {1}", requirementId, partName);
        }

    }
}
