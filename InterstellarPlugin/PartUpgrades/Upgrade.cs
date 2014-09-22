using System;
using UnityEngine;

namespace InterstellarPlugin.PartUpgrades
{
    public class Upgrade
    {

        public static Upgrade FromScalableValue(
            PartModule targetModule, BaseField targetField, float value, float exponent)
        {
            return new Upgrade(targetModule, targetField, new ScalableUpgradeSource(value, exponent));
        }

        public static Upgrade FromValue(
            PartModule targetModule, BaseField targetField, string value)
        {
            return new Upgrade(targetModule, targetField, new ValueUpgradeSource(targetField.FieldInfo.FieldType, value));
        }

        public void Apply(float scaleFactor = 1.0f)
        {
            targetField.SetValue(upgradeSource.GetValue(scaleFactor), targetModule);
#if DEBUG
            Debug.Log(string.Format("Set {0}.{1} = {2}",
                targetModule.moduleName, targetField.name, targetField.GetValue(targetModule)));
#endif
        }

        public override string ToString()
        {
            return string.Format("{0}.{1} <- {2}", targetModule.name, targetField.name, upgradeSource);
        }

        private Upgrade(PartModule targetModule, BaseField targetField, IUpgradeSource upgradeSource)
        {
            this.targetModule = targetModule;
            this.targetField = targetField;
            this.upgradeSource = upgradeSource;
        }


        private readonly PartModule targetModule;
        private readonly BaseField targetField;
        private readonly IUpgradeSource upgradeSource;
    }

    internal interface IUpgradeSource
    {
        object GetValue(float scaleFactor);
    }

    internal class ValueUpgradeSource : IUpgradeSource
    {
        public object GetValue(float scaleFactor)
        {
            return value;
        }

        public ValueUpgradeSource(Type targetType, string sourceValue)
        {
            this.sourceValue = sourceValue;
            value = FieldParser.Parse(targetType, sourceValue);
        }

        public override string ToString()
        {
            return sourceValue;
        }

        private readonly string sourceValue;
        private readonly object value;
    }

    internal class ScalableUpgradeSource : IUpgradeSource
    {
        public object GetValue(float scaleFactor)
        {
            return value * Math.Pow(scaleFactor, exponent);
        }

        public ScalableUpgradeSource(float value, float exponent)
        {
            this.value = value;
            this.exponent = exponent;
        }

        private readonly float value;
        private readonly float exponent;
    }

}