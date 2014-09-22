using System;
using System.CodeDom;
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
        public const string ExponentKey = "scaleExponent";
        public const string RequirementKey = "REQUIREMENT";
        public const string NameKey = "name";

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

            if (state == StartState.Editor)
                CheckRequirements();
#if DEBUG
            Debug.Log(string.Format("[Interstellar] Started {0}.", this));
#endif

        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            // TODO pass correct requirement node to each requirement for save.
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

        // TODO refactor Validate/Load to external class ("ProtoUpgrade"?)
        /// <summary>
        /// Checks the validity of an UPGRADE config node.
        /// </summary>
        /// <param name="node">The config node to validate</param>
        /// <returns><code>null</code> if this <code>ProtoUpgrade</code> is valid, or an error message string.</returns>
        public string ValidateUpgrade(ConfigNode node)
        {
            var module = node.GetValue(ModuleKey);
            var target = node.GetValue(TargetKey);
            var value = node.GetValue(ValueKey);
            var scaleExponent = node.GetValue(ExponentKey);

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

            // Check for the value
            if (string.IsNullOrEmpty(value))
                return string.Format("'{0}' must not be missing or empty.", ValueKey);

            if (!string.IsNullOrEmpty(scaleExponent))
            {
                // The scale exponent must target a numeric field.
                if (targetField.FieldInfo.FieldType != typeof(float) && targetField.FieldInfo.FieldType != typeof(int))
                    return string.Format("Cannot use {0} for upgrade of field {1} as it is not a numeric field.",
                        ExponentKey, targetField.name);

                // The scale exponent must be a valid float.
                float exponentValue;
                if (!float.TryParse(scaleExponent, out exponentValue))
                    return string.Format("Could not parse '{0}' {1} as a float.", ExponentKey, scaleExponent);
            }


            // The value must agree with the target's type.
            Type targetType = targetField.FieldInfo.FieldType;
            object valueObj;
            Type fieldType = targetType;
            if (!FieldParser.TryParse(fieldType, value, out valueObj))
                return string.Format("The value {0} cannot be parsed as {1}:{2}",
                    value, target, fieldType.Name);

            return null;
        }

        private Upgrade LoadUpgrade(ConfigNode node)
        {
            var module = node.GetValue(ModuleKey);
            var targetModule = part.Modules.OfType<PartModule>().FirstOrDefault(m => m.moduleName == module);
            var targetField = FindField(targetModule, node.GetValue(TargetKey));

            var scaleExponent = node.GetValue(ExponentKey);

            if (string.IsNullOrEmpty(scaleExponent))
                return Upgrade.FromValue(targetModule, targetField, node.GetValue(ValueKey));

            var exponent = float.Parse(scaleExponent);
            var value = float.Parse(node.GetValue(ValueKey));
            return Upgrade.FromScalableValue(targetModule, targetField, value, exponent);
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
                    @object.GetType().AssemblyQualifiedName, typeof(UpgradeRequirement).Name);
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