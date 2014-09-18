using System;
using System.Linq;

namespace InterstellarPlugin.PartUpgrades
{
    public class ProtoUpgrade
    {
        public const string ModuleKey = "module";
        public const string TargetKey = "target";
        public const string ValueKey = "value";
        public const string SourceKey = "source";

        public string Module
        {
            get { return module; }
        }

        public string Target
        {
            get { return target; }
        }

        public string Value
        {
            get { return value; }
        }

        public string Source
        {
            get { return source; }
        }

        /// <summary>
        /// Loads a <code>ProtoUpgrade</code> from config.
        /// </summary>
        /// <param name="node">A config node</param>
        /// <returns>The loaded ProtoUpgrade</returns>
        public static ProtoUpgrade Load(ConfigNode node)
        {
            var module = node.GetValue(ModuleKey);
            var target = node.GetValue(TargetKey);
            var source = node.GetValue(SourceKey);
            var value = node.GetValue(ValueKey);

            return new ProtoUpgrade(module, target, value, source);
        }

        /// <summary>
        /// Checks the validity of this <code>ProtoUpgrade</code> for a given <code>UpgradeModule</code>.
        /// </summary>
        /// <param name="upgradeModule">The <code>UpgradeModule</code></param>
        /// <returns><code>null</code> if this <code>ProtoUpgrade</code> is valid, or an error message string.</returns>
        public string Validate(UpgradeModule upgradeModule)
        {
            var part = upgradeModule.part;
            // check the existence of the target module
            if (string.IsNullOrEmpty(module))
                return ValidationError(part, "'{0}' must not be missing or empty", ModuleKey);
            var partModule = part.Modules.OfType<PartModule>().FirstOrDefault(m => m.moduleName == module);
            if (partModule == null)
                return ValidationError(part, "no module named {0}", module);

            // check for the target field on target module
            if (string.IsNullOrEmpty(target))
                return ValidationError(part, "'{0}' must not be missing or empty", TargetKey);
            var targetField = FindField(partModule, target);
            if (targetField == null)
                return ValidationError(part, string.Concat("module {0} does not have a field named {1}",
                    module, target));

            // Either source or value must be set, not both.
            if (string.IsNullOrEmpty(source) == string.IsNullOrEmpty(value))
                return ValidationError(part, "One and only one of '{0}' and '{1}' must be set.",
                    SourceKey, ValueKey);

            // The value must agree with the target's type.
            Type targetType = targetField.FieldInfo.FieldType;
            if (value != null)
            {
                object valueObj;
                Type fieldType = targetType;
                if (!FieldParser.TryParse(fieldType, value, out valueObj))
                    return ValidationError(part, "The value {0} of {1} could not be parsed as a(n) {2}",
                        value, target, fieldType.Name);
            }
            // The source field mus be found and its type must agree with the target's type.
            else if (source != null)
            {
                var sourceField = FindField(upgradeModule, source);
                if (sourceField == null)
                {
                    sourceField = FindField(partModule, source);
                    if (sourceField == null)
                        return ValidationError(part, "Neither module {0} nor this module have a field named {1}",
                            module, source);

                    Type sourceType = sourceField.FieldInfo.FieldType;
                    bool compatible = sourceType == targetType ||
                                      (targetType == typeof (float) && sourceType == typeof (int));
                    if (!compatible)
                        return ValidationError(part, "source type {0} is not compatible with target type {1}",
                            sourceType.Name, targetType.Name);
                }
            }

            return null;
        }

        private static BaseField FindField(PartModule partModule, string name)
        {
            return partModule.Fields.OfType<BaseField>().FirstOrDefault(f => f.name == name);
        }

        private string ValidationError(Part part, string messageFmt, params object[] objects)
        {
            return string.Format("In {0} for {1}: {2}", GetType().Name, part.name,
                string.Format(messageFmt, objects));
        }

        internal ProtoUpgrade(string module, string target, string value, string source)
        {
            this.module = module;
            this.target = target;
            this.value = value;
            this.source = source;
        }

        private readonly string module;
        private readonly string target;
        private readonly string value;
        private readonly string source;

    }
}