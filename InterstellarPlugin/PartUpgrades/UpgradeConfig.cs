using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InterstellarPlugin.PartUpgrades
{
    // Function object for loading and validating UPGRADE nodes
    class UpgradeConfig
    {
        public const string UpgradeKey = "UPGRADE";
        public const string ModuleKey = "module";
        public const string TargetKey = "target";
        public const string ValueKey = "value";
        public const string ExponentKey = "scaleExponent";

        public UpgradeConfig(Part part, ConfigNode node, int index)
        {
            this.part = part;
            this.index = index;
            module = node.GetValue(ModuleKey);
            target = node.GetValue(TargetKey);
            value = node.GetValue(ValueKey);
            scaleExponent = node.GetValue(ExponentKey);
        }

        // Using IEnumerable as a poor man's option.
        public IEnumerable<Upgrade> Load()
        {
            var error = ValidateUpgrade();
            if (error == null)
                return new List<Upgrade> { new Upgrade(targetComponent, targetField, upgradeSource) };

            Debug.LogWarning(
                string.Format("[Interstellar] {0} for {1} could not load {2} node {3}: {4}.",
                    GetType().Name, part.OriginalName(), UpgradeKey, index, error));

            return Enumerable.Empty<Upgrade>();
        }

        // Checks the validity of an UPGRADE config node. Returns a non-null error message when something is wrong.
        private string ValidateUpgrade()
        {
            BaseFieldList fields;
            // check the existence of the target module, otherwise the upgrade is understood to target a Part field.
            if (string.IsNullOrEmpty(module))
            {
                fields = part.Fields;
                targetComponent = new UpgradablePart(part);
            }
            else
            {
                var targetModule = part.Modules.OfType<PartModule>().FirstOrDefault(m => m.moduleName == module);
                if (targetModule == null)
                    return string.Format("no module named {0}", module);
                fields = targetModule.Fields;
                targetComponent = new UpgradablePartModule(targetModule);
            }

            // check for the target field on target module/part
            if (string.IsNullOrEmpty(target))
                return string.Format("'{0}' must not be missing or empty", TargetKey);
            targetField = fields.OfType<BaseField>().FirstOrDefault(f => f.name == target);
            if (targetField == null)
                return string.Format("module {0} does not have a field named {1}", module, target);

            // Check for the value
            if (string.IsNullOrEmpty(value))
                return string.Format("'{0}' must not be missing or empty.", ValueKey);

            Type fieldType = targetField.FieldInfo.FieldType;


            if (!string.IsNullOrEmpty(scaleExponent))
            {
                bool scalableType = false;
                bool integral = false;

                if (fieldType == typeof(float))
                {
                    scalableType = true;
                }
                else if (fieldType == typeof(int))
                {
                    scalableType = true;
                    integral = true;
                }

                // The scale exponent must target a numeric field.
                if (!scalableType)
                    return string.Format("Cannot use {0} for upgrade of field {1} as it is not a numeric field.",
                        ExponentKey, targetField.name);

                // The scale exponent must be a valid float.
                float exponentValue;
                if (!float.TryParse(scaleExponent, out exponentValue))
                    return string.Format("Could not parse '{0}' {1} as a float.", ExponentKey, scaleExponent);

                // The value must be parseable as a float.
                float valueFloat;
                if (!float.TryParse(value, out valueFloat))
                    return string.Format("Could not parse '{0}' {1} as a float.", ValueKey, value);

                upgradeSource = new ScalableUpgradeSource(valueFloat, exponentValue, integral);
            }
            else
            {
                // The value must agree with the target's type.
                object valueObj;
                if (!FieldParser.TryParse(fieldType, value, out valueObj))
                    return String.Format("The value {0} cannot be parsed as {1}:{2}",
                        value, target, fieldType.Name);

                upgradeSource = new ValueUpgradeSource(valueObj);

            }


            return null;
        }

        private readonly Part part;
        private readonly int index;
        private readonly string module;
        private readonly string target;
        private readonly string value;
        private readonly string scaleExponent;
        private IUpgradableComponent targetComponent;
        private BaseField targetField;
        private IUpgradeSource upgradeSource;


     }
}