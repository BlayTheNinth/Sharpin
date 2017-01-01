using System.Linq;
using Mono.Cecil;

namespace Sharpin2 {
    public struct CaptureFieldInfo {
        public string Field { get; }

        public CaptureFieldInfo(ICustomAttributeProvider field) {
            var attr = field.CustomAttributes.First(a => a.AttributeType.FullName == typeof(CaptureField).FullName);
            Field = AttrHelper.GetConstructorAttribute<string>(attr, "Field");
        }

    }
}
