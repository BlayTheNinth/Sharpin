using System.Linq;

using Mono.Cecil;

namespace Sharpin2 {
    public struct CaptureLocalInfo {
        public int Index { get; }
        public TypeReference Type { get; }

        public CaptureLocalInfo(ParameterDefinition param) {
            var attr = param.CustomAttributes.First(a => a.AttributeType.FullName == typeof(CaptureLocal).FullName);
            this.Index = AttrHelper.GetConstructorAttribute<int>(attr, "index", 0);
            this.Type = AttrHelper.GetConstructorAttribute<TypeReference>(attr, "type", 1);
        }
    }
}
