using System;
using System.Linq;
using Mono.Cecil;

namespace Sharpin2 {
    public struct CaptureFieldInfo {
        public string Field { get; }

        public CaptureFieldInfo(FieldDefinition field) {
            var attr = field.CustomAttributes.First(a => a.AttributeType.FullName == typeof(CaptureField).FullName);
            this.Field = AttrHelper.GetConstructorAttribute<string>(attr, "field");
        }

    }
}
