using Mono.Cecil;

namespace Sharpin2 {
    public struct OverwriteInfo {
        public string Target { get; }
        public MethodDefinition NewMethod { get; }

        public OverwriteInfo(TypeDefinition targetType, MethodDefinition newMethod) {
            this.NewMethod = newMethod;
            this.Target = newMethod.ReturnType.FullName + " " + targetType.FullName + "::" + newMethod.FullName.Substring(newMethod.FullName.LastIndexOf(':') + 1);
        }
    }
}
