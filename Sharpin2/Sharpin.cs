using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;

namespace Sharpin2 {

	// TODO ability to mark a mixin as PreSharpin and have it run in setup process so we could use patched things in dev? (I swear I already did this in some way for Stardew Valley though)
	public class Sharpin {
		private readonly List<MixinInfo> _mixinList = new List<MixinInfo>();
		private readonly AssemblyResolver _assemblyResolver = new AssemblyResolver();
		private readonly ModuleDefinition _targetModule;
		private readonly ModuleDefinition _patchModule;

		public Sharpin(string targetLibrary, string patchLibrary) {
			_targetModule = ModuleDefinition.ReadModule(targetLibrary, new ReaderParameters {AssemblyResolver = _assemblyResolver});
			_assemblyResolver.AddToCache(_targetModule);
			_patchModule = ModuleDefinition.ReadModule(patchLibrary, new ReaderParameters {AssemblyResolver = _assemblyResolver});
			_assemblyResolver.AddToCache(_patchModule);
		}

		public void ApplyMixins() {
			var mixinContainers = _patchModule.Types.Where(t => t.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(Mixin).FullName));
			foreach (var mixin in mixinContainers) {
				_mixinList.Add(new MixinInfo(mixin));
			}

			_mixinList.Sort();

			foreach (var mixin in _mixinList) {
				var mixinizer = new Mixor(mixin, _targetModule);
				mixinizer.Apply();
			}
		}

		public void Write(string outputFile) {
			_targetModule.Write(outputFile);
		}
	}

}