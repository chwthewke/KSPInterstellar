using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InterstellarPlugin.PartUpgrades
{
    [Serializable]
    public class ProtoUpgrade
    {
        private const string ModuleKey = "module";
        private const string UpgradeValueKey = "FIELD";

        public static ProtoUpgrade Load(ConfigNode node)
        {
            var moduleName = node.GetValue(ModuleKey, NodeValidation.NonEmpty);

            var upgradeValues = node.GetNodes(UpgradeValueKey).Select(ProtoUpgradeField.Load).ToList();

            return new ProtoUpgrade(moduleName, upgradeValues);
        }

        public ProtoUpgrade(string moduleName, IEnumerable<ProtoUpgradeField> values)
        {
            this.moduleName = moduleName;
            this.fields = new List<ProtoUpgradeField>(values);
        }

        internal ProtoUpgrade()
        {
        }

        [SerializeField]
        private string moduleName;

        [SerializeField]
        private List<ProtoUpgradeField> fields;

        public string ModuleName
        {
            get { return moduleName; }
        }

        public List<ProtoUpgradeField> Fields
        {
            get { return new List<ProtoUpgradeField>(fields); }
        }

        public override string ToString()
        {
            return string.Format("{0} for {1} [{2}]",
                GetType().Name,
                moduleName,
                string.Join(", ", fields.Select(v => v.ToString()).ToArray()));
        }
    }

    [Serializable]
    public class ProtoUpgradeField
    {
        public const string TargetKey = "target";
        public const string SourceKey = "source";

        [SerializeField]
        private string targetField;

        [SerializeField]
        private string sourceField;

        public string TargetField
        {
            get { return targetField; }
        }

        public string SourceField
        {
            get { return sourceField; }
        }

        internal ProtoUpgradeField()
        {
        }

        public static ProtoUpgradeField Load(ConfigNode node)
        {
            var source = node.GetValue(SourceKey, NodeValidation.NonEmpty); // TODO module has field
            var target = node.GetValue(TargetKey, NodeValidation.NonEmpty); // TODO module has type-compatible field

            return new ProtoUpgradeField(target, source);
        }

        public ProtoUpgradeField(string targetField, string sourceField)
        {
            this.targetField = targetField;
            this.sourceField = sourceField;
        }

        public override string ToString()
        {
            return string.Format("{0} <- {1}", targetField, sourceField);
        }
    }
}