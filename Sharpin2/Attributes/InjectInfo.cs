using System.Linq;

using Mono.Cecil;

namespace Sharpin2 {
    public class InjectInfo {
        public MethodDefinition NewMethod { get; }
        public string Method { get; }
        public string At { get; }
        public bool Cancellable { get; }
        public string CancelTarget { get; }
        public int ExpectedInjections { get; }

        public InjectInfo(MethodDefinition newMethod) {
            NewMethod = newMethod;
            var attr = newMethod.CustomAttributes.First(a => a.AttributeType.FullName == typeof(Inject).FullName);
            Method = AttrHelper.GetAttribute<string>(attr, "Method");
            At = AttrHelper.GetAttribute<string>(attr, "At");
            Cancellable = AttrHelper.GetAttribute<bool>(attr, "Cancellable");
            CancelTarget = AttrHelper.GetAttribute(attr, "CancelTarget", "ret");
            ExpectedInjections = AttrHelper.GetAttribute(attr, "ExpectedInjections", 1);
        }
    }
}
