using Mono.Cecil;

namespace Sharpin2 {

	public class MixinInjectionException : MixinException {
		public MethodDefinition TargetMethod { get; private set; }

		public MixinInjectionException(string message) : base(message) {
		}

		public MixinInjectionException(string message, MethodDefinition targetMethod) : base(message) {
			TargetMethod = targetMethod;
		}
	}

}