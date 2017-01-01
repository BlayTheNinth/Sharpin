using System.Linq;

using Mono.Cecil;

namespace Sharpin2 {
    public struct StoreLocalInfo {
        public int Index { get; }
        public TypeReference Type { get; }

        public StoreLocalInfo(ICustomAttributeProvider param) {
            var attr = param.CustomAttributes.First(a => a.AttributeType.FullName == typeof(StoreLocal).FullName);
            Index = AttrHelper.GetConstructorAttribute<int>(attr, "Index", 0);
            Type = AttrHelper.GetConstructorAttribute<TypeReference>(attr, "Type", 1);
        }
    }
}
