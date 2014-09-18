using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InterstellarPlugin.PartUpgrades
{
    public class UpgradeModule : PartModule
    {
        public const string UpgradeKey = "UPGRADE";
        public const string RequirementKey = "REQUIREMENT";
        public const string ModuleKey = "module";
        public const string TargetKey = "target";
        public const string ValueKey = "value";
        public const string SourceKey = "source";

        [KSPField(isPersistant = false)]
        public ConfigNode Config;

        [KSPField(isPersistant = true)]
        public bool IsUnlocked = false;

        public override void OnLoad(ConfigNode node)
        {
            // AFAIK, this is more or less the only way to copy complex data structures from the loaded part prefab
            // to an instance in editor/flight (other that packing data to strings).
            Config = new ConfigNode();

            for (int index = 0; index < node.GetNodes(UpgradeKey).Length; index++)
            {
                var upgradeNode = node.GetNodes(UpgradeKey)[index];
                var error = ValidateUpgrade(upgradeNode);
                if (error == null)
                {
                    Config.AddNode(upgradeNode);
                }
                else
                {
                    Debug.LogWarning(
                        string.Format("[Interstellar] {0} for {1} could not load {2} node {3}: {4}.",
                            GetType().Name, part.name, UpgradeKey, index, error));
                }
            }

            for (int index = 0; index < node.GetNodes(RequirementKey).Length; index++)
            {
                var requirementNode = node.GetNodes(RequirementKey)[index];
                var error = ValidateRequirement(requirementNode);
                if (error == null)
                {
                    Config.AddNode(requirementNode);
                }
                else
                {
                    Debug.LogWarning(
                        string.Format("[Interstellar] {0} for {1} could not load {2} node {3}: {4}.",
                            GetType().Name, part.name, RequirementKey, index, error));
                }
            }

#if DEBUG
            Debug.Log(string.Format("[Interstellar] Loaded {0}.", this));
#endif
        }

        public override void OnStart(StartState state)
        {
            
#if DEBUG
            Debug.Log(string.Format("[Interstellar] Started {0}.", this));
#endif
        }

        public override string ToString()
        {
            return string.Format("{0} for {1}: upgrades = [{2}], requirements = [{3}], config = <{4}>", 
                GetType(), part.name,
                string.Join(", ", upgrades.Select(o => o.ToString()).ToArray()),
                string.Join(", ", requirements.Select(o => o.ToString()).ToArray()),
                Config);
        }


        /// <summary>
        /// Checks the validity of an UPGRADE config node.
        /// </summary>
        /// <param name="node">The config node to validate</param>
        /// <returns><code>null</code> if this <code>ProtoUpgrade</code> is valid, or an error message string.</returns>
        public string ValidateUpgrade(ConfigNode node)
        {
            var module = node.GetValue(ModuleKey);
            var target = node.GetValue(TargetKey);
            var source = node.GetValue(SourceKey);
            var value = node.GetValue(ValueKey);

            // check the existence of the target module
            if (string.IsNullOrEmpty(module))
                return string.Format("'{0}' must not be missing or empty", ModuleKey);
            var partModule = part.Modules.OfType<PartModule>().FirstOrDefault(m => m.moduleName == module);
            if (partModule == null)
                return string.Format("no module named {0}", module);

            // check for the target field on target module
            if (string.IsNullOrEmpty(target))
                return string.Format("'{0}' must not be missing or empty", TargetKey);
            var targetField = FindField(partModule, target);
            if (targetField == null)
                return string.Format("module {0} does not have a field named {1}", module, target);

            // Either source or value must be set, not both.
            if (string.IsNullOrEmpty(source) == string.IsNullOrEmpty(value))
                return string.Format("One and only one of '{0}' and '{1}' must be set.", SourceKey, ValueKey);

            // The value must agree with the target's type.
            Type targetType = targetField.FieldInfo.FieldType;
            if (value != null)
            {
                object valueObj;
                Type fieldType = targetType;
                if (!FieldParser.TryParse(fieldType, value, out valueObj))
                    return string.Format("The value {0} of {1} could not be parsed as a(n) {2}",
                        value, target, fieldType.Name);
            }
                // The source field must be found and its type must agree with the target's type.
            else
            {
                var sourceField = FindField(partModule, source);
                if (sourceField == null)
                    return string.Format("Neither module {0} nor this module have a field named {1}",
                        module, source);

                Type sourceType = sourceField.FieldInfo.FieldType;
                bool compatible = sourceType == targetType ||
                                  (targetType == typeof (float) && sourceType == typeof (int));
                if (!compatible)
                    return string.Format("source type {0} is not compatible with target type {1}",
                        sourceType.Name, targetType.Name);
            }

            return null;
        }

        private string ValidateRequirement(ConfigNode node)
        {
            var @object = ConfigNode.CreateObjectFromConfig(node);
            if (@object == null)
                return string.Format("Could not create object {0}", node.GetValue(name));
            var requirement = @object as UpgradeRequirement;
            if (requirement == null)
                return string.Format("Upgrade requirement type {0} is not an {1}",
                    @object.GetType().Name, typeof (UpgradeRequirement).Name);
            return requirement.Validate(part);
        }

        private static BaseField FindField(PartModule partModule, string name)
        {
            return partModule.Fields.OfType<BaseField>().FirstOrDefault(f => f.name == name);
        }

        private IList<Upgrade> upgrades;
        private IList<UpgradeRequirement> requirements;
    }
}