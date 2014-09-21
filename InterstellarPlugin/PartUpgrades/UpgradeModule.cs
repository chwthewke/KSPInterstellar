using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InterstellarPlugin.PartUpgrades
{
    public class UpgradeModule : PartModule
    {
        public const string UpgradeKey = "UPGRADE";
        public const string ModuleKey = "module";
        public const string TargetKey = "target";
        public const string ValueKey = "value";
        public const string SourceKey = "source";
        public const string RequirementKey = "REQUIREMENT";
        public const string NameKey = "name";

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
                            GetType().Name, part.OriginalName(), UpgradeKey, index, error));
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
                            GetType().Name, part.OriginalName(), RequirementKey, index, error));
                }
            }

#if DEBUG
            Debug.Log(string.Format("[Interstellar] Loaded {0}.", this));
#endif
        }

        public override void OnStart(StartState state)
        {
            upgrades = Config.GetNodes(UpgradeKey).Select(LoadUpgrade).ToList();
            requirements = Config.GetNodes(RequirementKey).Select(LoadRequirement).ToList();
#if DEBUG
            Debug.Log(string.Format("[Interstellar] Started {0}.", this));
#endif
            
        }

        public void CheckRequirements()
        {
            IsUnlocked = requirements.All(req => req.IsFulfilled());

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
            return string.Format("{0} for {1}: upgrades = [{2}], requirements = [{3}], config = <{4}>",
                GetType(), part.OriginalName(),
                upgrades == null ? "null" : string.Join(", ", upgrades.Select(o => o.ToString()).ToArray()),
                requirements == null ? "null" : string.Join(", ", requirements.Select(o => o.ToString()).ToArray()),
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
                    return string.Format("The value {0} cannot be parsed as {1}:{2}",
                        value, target, fieldType.Name);
            }
                // The source field must be found and its type must agree with the target's type.
            else
            {
                var sourceField = FindField(partModule, source);
                if (sourceField == null)
                    return string.Format("Module {0} has no field named {1}",
                        module, source);

                Type sourceType = sourceField.FieldInfo.FieldType;
                bool compatible = sourceType == targetType ||
                                  (targetType == typeof (float) && sourceType == typeof (int));
                if (!compatible)
                    return string.Format("source {0}: {1} is not compatible with target type {2}: {3}",
                        source, sourceType.Name, target, targetType.Name);
            }

            return null;
        }

        private Upgrade LoadUpgrade(ConfigNode node)
        {
            var module = node.GetValue(ModuleKey);
            var targetModule = part.Modules.OfType<PartModule>().FirstOrDefault(m => m.moduleName == module);
            var targetField = FindField(targetModule, node.GetValue(TargetKey));
            string source = node.GetValue(SourceKey);
            if (source != null)
            {
                var sourceField = FindField(targetModule, source);
                return Upgrade.FromSourceField(targetModule, targetField, sourceField);
            }
            return Upgrade.FromValue(targetModule, targetField, node.GetValue(ValueKey));
        }

        private string ValidateRequirement(ConfigNode node)
        {
            var type = RequirementType(node);

            if (type == null)
                return "Cound not find a requirement type named " + node.GetValue(NameKey);

            var @object = ConfigNode.CreateObjectFromConfig(type.AssemblyQualifiedName, node);
            if (@object == null)
                return string.Format("Could not create object {0}", type.Name);
            var requirement = @object as UpgradeRequirement;
            if (requirement == null)
                return string.Format("Upgrade requirement type {0} is not an {1}",
                    @object.GetType().AssemblyQualifiedName, typeof (UpgradeRequirement).Name);
            return requirement.Validate(part);
        }

        private static Type RequirementType(ConfigNode node)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == node.GetValue(NameKey));
            return type;
        }

        private UpgradeRequirement LoadRequirement(ConfigNode node)
        {
            var typeName = RequirementType(node).AssemblyQualifiedName;
            return ConfigNode.CreateObjectFromConfig(typeName, node) as UpgradeRequirement;
        }

        private static BaseField FindField(PartModule partModule, string name)
        {
            return partModule.Fields.OfType<BaseField>().FirstOrDefault(f => f.name == name);
        }

        private IList<Upgrade> upgrades;
        private IList<UpgradeRequirement> requirements;
    }
}