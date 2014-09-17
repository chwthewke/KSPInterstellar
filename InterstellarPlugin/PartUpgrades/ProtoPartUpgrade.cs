using System;
using System.Collections.Generic;
using UnityEngine;

namespace InterstellarPlugin.PartUpgrades
{
    [Serializable]
    public class ProtoPartUpgrade
    {
        private const string ModuleKey = "module";

        public static ProtoPartUpgrade Load(ConfigNode node)
        {
            var moduleName = node.GetValue(ModuleKey);
            if (string.IsNullOrEmpty(moduleName))
                throw new ArgumentException(
                string.Format("{0}: {1} cannot be empty.", typeof(ProtoPartUpgrade).Name, ModuleKey));

                return new ProtoPartUpgrade(moduleName, new List<PartUpgradeValue>());
        }

        public ProtoPartUpgrade(string moduleName, IEnumerable<PartUpgradeValue> values)
        {
            this.moduleName = moduleName;
            this.values = new List<PartUpgradeValue>(values);
        }

        internal ProtoPartUpgrade()
        {
        }

        [SerializeField]
        private string moduleName;

        [SerializeField]
        private List<PartUpgradeValue> values;

        public string ModuleName
        {
            get { return moduleName; }
        }

        public List<PartUpgradeValue> Values
        {
            get { return new List<PartUpgradeValue>(values); }
        }

        [Serializable]
        public class PartUpgradeValue
        {
            [SerializeField]
            private string field;

            [SerializeField]
            private string value;

            public string Field
            {
                get { return field; }
            }

            public string Value
            {
                get { return value; }
            }
        }
    }
}