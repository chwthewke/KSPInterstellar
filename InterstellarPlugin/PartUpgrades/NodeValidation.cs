using System;
using System.Linq;
using System.Runtime.InteropServices;
using KSPAchievements;

namespace InterstellarPlugin.PartUpgrades
{
    internal static class NodeValidation
    {
        public static string GetValue(this ConfigNode node, string key, params Func<string, ValidationResult>[] validators)
        {
            return validators.Aggregate(node.GetValue(key), (v, validator) =>
            {
                var res = validator(v);
                if (res.error != null)
                    throw new ArgumentException(string.Format("In a {0} node, {1} {2}.", node.name, key, res.error));
                return res.value;
            });
        }

        public static readonly Func<string, ValidationResult> NonEmpty = 
            s => string.IsNullOrEmpty(s) ? ValidationResult.Error("must not be empty") : ValidationResult.Ok(s);

    }


    internal struct ValidationResult
    {
        public static ValidationResult Ok(string value)
        {
            return new ValidationResult(null, value);
        }

        public ValidationResult(string error, string value)
        {
            this.error = error;
            this.value = value;
        }

        public readonly string error;
        public readonly string value;

        public static ValidationResult Error(string error)
        {
            return new ValidationResult(error, null);
        }
    }
}