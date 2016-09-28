using System;
using System.Linq;
using Mono.Cecil;

namespace Sharpin2 {
    public static class AttrHelper {
        public static bool HasAttribute(this ParameterDefinition param, Type type) {
            return param.CustomAttributes.Any(t => t.AttributeType.FullName == type.FullName);
        }

        public static CustomAttribute GetAttribute(this ParameterDefinition param, Type type) {
            return param.CustomAttributes.Single(t => t.AttributeType.FullName == type.FullName);
        }

        public static T GetAttribute<T>(CustomAttribute attr, string name) {
            return GetAttribute(attr, name, default(T));
        }

        public static T GetAttribute<T>(CustomAttribute attr, string name, T defaultValue) {
            var field = attr.Fields.FirstOrDefault(f => f.Name == name);
            if (field.Name == name) {
                return (T) field.Argument.Value;
            }
            return defaultValue;
        }

        public static T GetConstructorAttribute<T>(CustomAttribute attr, string name) {
            return GetConstructorAttribute<T>(attr, name, 0);
        }

        public static T GetConstructorAttribute<T>(CustomAttribute attr, string name, int index) {
            var field = attr.Fields.FirstOrDefault(f => f.Name == name);
            if (field.Name == name) {
                return (T) field.Argument.Value;
            }
            return (T) attr.ConstructorArguments[index].Value;
        }
    }
}
