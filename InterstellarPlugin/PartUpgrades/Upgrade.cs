using System;
using UnityEngine;

namespace InterstellarPlugin.PartUpgrades
{
    public class Upgrade
    {

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

        internal Upgrade(PartModule targetModule, BaseField targetField, IUpgradeSource upgradeSource)
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

        public ValueUpgradeSource(object value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return value.ToString();
        }

        private readonly object value;
    }

    internal class ScalableUpgradeSource : IUpgradeSource
    {
        public object GetValue(float scaleFactor)
        {
            double scaled = value * Math.Pow(scaleFactor, exponent);
            return integral ? (int) scaled : (float) scaled;
        }

        public ScalableUpgradeSource(float value, float exponent, bool integral)
        {
            this.value = value;
            this.exponent = exponent;
            this.integral = integral;
        }

        public override string ToString()
        {
            return string.Format("{0} (exponent = {1})", value, exponent);
        }

        private readonly float value;
        private readonly float exponent;
        private bool integral;
    }

}