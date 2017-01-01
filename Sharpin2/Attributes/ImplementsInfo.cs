using Mono.Cecil;

namespace Sharpin2 {
    public struct ImplementsInfo {
        public TypeDefinition MixinContainer { get; }
        public TypeReference TargetType { get; }

        public ImplementsInfo(TypeDefinition mixinContainer, CustomAttribute attr) {
            MixinContainer = mixinContainer;
            TargetType = AttrHelper.GetConstructorAttribute<TypeReference>(attr, "TargetType");
        }

    }
}
