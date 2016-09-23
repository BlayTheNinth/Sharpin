using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Mono.Cecil;

namespace Sharpin2 {
    public class InjectInfo {
        public MethodDefinition NewMethod { get; }
        public string Method { get; }
        public string At { get; }
        public bool Cancellable { get; }
        public int ExpectedInjections { get; }

        public InjectInfo(MethodDefinition newMethod) {
            this.NewMethod = newMethod;
            var attr = newMethod.CustomAttributes.First(a => a.AttributeType.FullName == typeof(Inject).FullName);
            this.Method = AttrHelper.GetAttribute<string>(attr, "method");
            this.At = AttrHelper.GetAttribute<string>(attr, "at");
            this.Cancellable = AttrHelper.GetAttribute<bool>(attr, "cancellable");
            this.ExpectedInjections = AttrHelper.GetAttribute<int>(attr, "expectedInjections", 1);
        }
    }
}
