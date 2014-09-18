using System;

namespace InterstellarPlugin.PartUpgrades
{
    /// <summary>
    /// Utility functions to Parse (or TryParse) some of the KSPField types. Currently supported are
    /// bool, int, float, string, and enums.
    /// </summary>
    internal class FieldParser
    {
        public static bool TryParse(Type type, string value, out object obj)
        {
            var parser = ParserOf(type);
            if (parser == null)
            {
                obj = null;
                return false;
            }

            return parser.Apply(value, out obj);
        }

        public static object Parse(Type type, string value)
        {
            object result;
            if (!TryParse(type, value, out result))
                return null;
            return result;
        }

        private interface ITypeParser
        {
            bool Apply(string value, out object obj);
        }

        abstract class TypeParser<T> : ITypeParser
        {
            protected abstract bool DoTryParse(string value, out T result);

            public bool Apply(string value, out object obj)
            {
                T result;
                var parsed = DoTryParse(value, out result);
                obj = result;
                return parsed;
            }
        }

        class BoolParser : TypeParser<bool>
        {
            protected override bool DoTryParse(string value, out bool result)
            {
                return bool.TryParse(value, out result);
            }
        }

        class IntParser : TypeParser<int>
        {
            protected override bool DoTryParse(string value, out int result)
            {
                return int.TryParse(value, out result);
            }
        }

        class FloatParser : TypeParser<float>
        {
            protected override bool DoTryParse(string value, out float result)
            {
                return float.TryParse(value, out result);
            }
        }

        class StringParser : TypeParser<string>
        {
            protected override bool DoTryParse(string value, out string result)
            {
                result = value;
                return true;
            }
        }

        class EnumParser : TypeParser<object>
        {
            protected override bool DoTryParse(string value, out object result)
            {
                try
                {
                    result = Enum.Parse(enumType, value);
                    return true;
                }
                catch
                {
                    result = null;
                    return false;
                }
            }

            public EnumParser(Type enumType)
            {
                this.enumType = enumType;
            }

            private readonly Type enumType;
        }

        private static ITypeParser ParserOf(Type type)
        {
            if (type == typeof(bool))
                return new BoolParser();
            if (type == typeof(int))
                return new IntParser();
            if (type == typeof(float))
                return new FloatParser();
            if (type == typeof(string))
                return new StringParser();
            if (type.IsEnum)
                return new EnumParser(type);
            return null;
        }



    }
}