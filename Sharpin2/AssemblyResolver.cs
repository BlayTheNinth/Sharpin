using System.Collections.Generic;

using Mono.Cecil;

namespace Sharpin2 {

	public class AssemblyResolver : BaseAssemblyResolver {
		private readonly Dictionary<string, AssemblyDefinition> cache = new Dictionary<string, AssemblyDefinition>();

		public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters) {
			if (name != null) {
				AssemblyDefinition value;
				if (cache.TryGetValue(name.Name, out value)) {
					return value;
				}
			}

			return base.Resolve(name, parameters);
		}

		public void AddToCache(ModuleDefinition module) {
			cache.Add(module.Assembly.Name.Name, module.Assembly);
		}
	}

}