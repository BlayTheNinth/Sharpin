using System;
using System.Linq;

using Mono.Cecil;

namespace Sharpin2 {

	public struct MixinInfo : IComparable<MixinInfo> {
		public TypeDefinition MixinContainer { get; }
		public TypeReference TargetType { get; }
		public string Target { get; }
		public int Priority { get; }

		public MixinInfo(TypeDefinition mixinContainer) {
			MixinContainer = mixinContainer;
			var attr = mixinContainer.CustomAttributes.First(a => a.AttributeType.FullName == typeof(Mixin).FullName);
			TargetType = AttrHelper.GetConstructorAttribute<TypeReference>(attr, "TargetType");
			Target = AttrHelper.GetAttribute<string>(attr, "target");
			Priority = AttrHelper.GetAttribute<int>(attr, "priority");
		}

		public int CompareTo(MixinInfo other) {
			return other.Priority - Priority;
		}
	}

}