using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InterstellarPlugin.PartUpgrades
{
    public class RequirementConfig
    {
        public const string RequirementKey = "REQUIREMENT";
        public const string RetrofitKey = "RETROFIT";
        public const string NameKey = "name";

        public RequirementConfig(Part part, ConfigNode node, int index)
        {
            this.part = part;
            this.node = node;
            this.index = index;
        }

        // Using IEnumerable as a poor man's option.
        public IEnumerable<UpgradeRequirement> Load()
        {
            var error = ValidateRequirement();
            if (error == null)
            {
                return new List<UpgradeRequirement> { requirement };
            }

            Debug.LogWarning(
                string.Format("[Interstellar] {0} for {1} could not load {2} node {3}: {4}.",
                    GetType().Name, part.OriginalName(), RequirementKey, index, error));

            return Enumerable.Empty<UpgradeRequirement>();
        }

        // Validates a REQUIREMENT configuration node. Returns a non-null error message if something goes wrong.
        private string ValidateRequirement()
        {
            // Must find a type matching the requirement name
            type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == node.GetValue(NameKey));

            if (type == null)
                return "Cound not find a requirement type named " + node.GetValue(NameKey);

            // That type must extend UpgradeRequirement
            if (!typeof (UpgradeRequirement).IsAssignableFrom(type))
                return string.Format("Requirement type {0} found but is does not extend {1}.",
                    type.AssemblyQualifiedName, typeof (UpgradeRequirement).Name);

            // The requirement's KSPFields are loaded from config
            var @object = ConfigNode.CreateObjectFromConfig(type.AssemblyQualifiedName, node);
            
            requirement = @object as UpgradeRequirement;
            if (requirement == null)
                return String.Format("Could not create object {0}", type.Name);

            // The requirement object has an opportunity to do custom loading
            requirement.OnLoad(node);

            // The requirement object can validate its config by its own rules
            return requirement.Validate(part);
        }

        private readonly Part part;
        private readonly ConfigNode node;
        private readonly int index;
        private Type type;
        private UpgradeRequirement requirement;
    }
}