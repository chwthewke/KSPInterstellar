using System;
using UnityEngine;

namespace InterstellarPlugin.PartUpgrades
{
    public class Upgrade
    {

        public void Apply(float scaleFactor = 1.0f)
        {
            targetField.SetValue(upgradeSource.GetValue(scaleFactor), target.Component);
#if DEBUG
            Debug.Log(string.Format("Set {0}.{1} = {2}",
                target.ComponentName, targetField.name, targetField.GetValue(target.Component)));
#endif
        }

        public override string ToString()
        {
            return string.Format("{0}.{1} <- {2}", target.ComponentName, targetField.name, upgradeSource);
        }


        internal Upgrade(IUpgradableComponent target, BaseField targetField, IUpgradeSource upgradeSource)
        {
            this.target = target;
            this.targetField = targetField;
            this.upgradeSource = upgradeSource;
        }


        private readonly IUpgradableComponent target;
        private readonly BaseField targetField;
        private readonly IUpgradeSource upgradeSource;
    }

    internal interface IUpgradableComponent
    {
        object Component { get; }
        string ComponentName { get; }
    }

    internal class UpgradablePartModule: IUpgradableComponent
    {
        public UpgradablePartModule(PartModule module)
        {
            this.module = module;
        }

        public object Component
        {
            get { return module; }
        }

        public string ComponentName
        {
            get { return module.moduleName; }
        }

        private readonly PartModule module;
    }

    internal class UpgradablePart : IUpgradableComponent
    {
        public UpgradablePart(Part part)
        {
            this.part = part;
        }

        public object Component
        {
            get { return part; }
        }

        public string ComponentName
        {
            get { return part.OriginalName(); }
        }

        private readonly Part part;
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