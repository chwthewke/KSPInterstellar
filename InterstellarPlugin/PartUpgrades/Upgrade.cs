using System;

namespace InterstellarPlugin.PartUpgrades
{
    public class Upgrade
    {

        public static Upgrade FromSourceField(
            PartModule targetModule, BaseField targetField, PartModule sourceModule, BaseField sourceField)
        {
            return new Upgrade(targetModule, targetField, new FieldUpgradeSource(sourceModule, sourceField));
        }

        public static Upgrade FromValue(
            PartModule targetModule, BaseField targetField, string value)
        {
            return new Upgrade(targetModule, targetField, new ValueUpgradeSource(targetField.FieldInfo.FieldType, value));
        }

        public void Apply()
        {
            targetField.SetValue(upgradeSource.Value, targetModule);
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
        object Value { get; }
    }

    internal class ValueUpgradeSource : IUpgradeSource
    {
        public object Value { get { return value; } }

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

    internal class FieldUpgradeSource : IUpgradeSource
    {
        public object Value
        {
            get { return field.GetValue(module); }
        }

        public FieldUpgradeSource(PartModule module, BaseField field)
        {
            this.module = module;
            this.field = field;
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}", module, field);
        }

        private readonly PartModule module;
        private readonly BaseField field;
    }



}