using System.Linq;

using Mono.Cecil;

namespace Sharpin2 {
    public struct StoreLocalInfo {
        public int Index { get; }
        public TypeReference Type { get; }

        public StoreLocalInfo(ParameterDefinition param) {
            var attr = param.CustomAttributes.First(a => a.AttributeType.FullName == typeof(StoreLocal).FullName);
            this.Index = AttrHelper.GetConstructorAttribute<int>(attr, "index", 0);
            this.Type = AttrHelper.GetConstructorAttribute<TypeReference>(attr, "type", 1);
        }
    }
}
