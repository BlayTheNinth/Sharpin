using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;

namespace Sharpin2 {

	public class Remapper {
		// TODO Need support for Unity's Message system and Coroutine string version (but need to see first if it really works like that or if unity does its own weird caching thing or something)
		// TODO Need support for ARG

		[Flags]
		public enum RemapOptions {
			None = 0,
			IsUnity = 1
		}

		public class RemappedType {
			public string OriginalName { get; }
			public string NewName { get; }
			public Dictionary<string, string> FieldMap { get; } = new Dictionary<string, string>();
			public Dictionary<string, string> MethodMap { get; } = new Dictionary<string, string>();

			public TypeDefinition TypeDefinition { get; set; }

			public RemappedType(string originalName, string newName) {
				OriginalName = originalName;
				NewName = newName;
			}
		}

		public static void Remap(string targetFile, string outputFile, string mappings, RemapOptions options) {
			var typeMap = new Dictionary<string, RemappedType>();
			var reader = new StringReader(mappings);
			string line;
			RemappedType remappedType = null;
			while ((line = reader.ReadLine()) != null) {
				line = line.Trim();
				if (line == "" || line.StartsWith("#")) {
					continue;
				}

				var args = line.Split(' ');
				if (args.Length < 3) {
					throw new Exception("Invalid remapping definition, not enough arguments: " + line);
				}

				args[0] = args[0].ToUpperInvariant();
				switch (args[0]) {
					case "CLASS":
						if (remappedType != null) {
							typeMap.Add(remappedType.OriginalName, remappedType);
						}
						remappedType = new RemappedType(args[1], args[2]);
						break;
					case "FIELD":
						if (remappedType == null) {
							throw new Exception("Invalid remapping definition outside of class scope: " + line);
						}

						remappedType.FieldMap.Add(args[1], args[2]);
						break;
					case "METHOD":
						if (remappedType == null) {
							throw new Exception("Invalid remapping definition outside of class scope: " + line);
						}

						remappedType.MethodMap.Add(args[1], args[2]);
						break;
				}

				if (remappedType != null) {
					typeMap.Add(remappedType.OriginalName, remappedType);
				}
				reader.Close();
				var module = ModuleDefinition.ReadModule(targetFile);
				foreach (var type in module.GetTypes()) {
					if (typeMap.TryGetValue(type.FullName, out remappedType)) {
						if ((options & RemapOptions.IsUnity) == 0) {
							type.Name = remappedType.NewName;
						} else if (type.Name != remappedType.NewName) {
							Console.WriteLine("Warning: Ignoring new type name for {0} - type renaming is not allowed for Unity environments due to technical limitations.", type.Name);
						}
						remappedType.TypeDefinition = type;
					}
				}
				foreach (var type in typeMap.Values) {
					foreach (var entry in type.FieldMap) {
						var foundField = type.TypeDefinition.Fields.FirstOrDefault(t => t.Name == entry.Key);
						if (foundField != null) {
							if ((options & RemapOptions.IsUnity) != 0 && !foundField.IsStatic && (foundField.IsPublic || foundField.CustomAttributes.Any(t => t.AttributeType == module.ImportReference(typeof(UnityEngine.SerializeField))))) {
								var attributeType = module.ImportReference(typeof(UnityEngine.Serialization.FormerlySerializedAsAttribute)).Resolve();
								var attributeConstructor = module.ImportReference(attributeType.Methods.First(m => m.IsConstructor && m.Parameters.All(p => p.ParameterType.FullName == module.TypeSystem.String.FullName)));
								var attribute = new CustomAttribute(attributeConstructor);
								attribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, foundField.Name));
								foundField.CustomAttributes.Add(attribute);
							}
							foundField.Name = entry.Value;
						} else {
							throw new Exception("Invalid Field mapping (" + entry.Key + " => " + entry.Value + "): Field not found");
						}
					}
					foreach (var entry in type.MethodMap) {
						var foundMethod = type.TypeDefinition.Methods.FirstOrDefault(t => t.FullName == entry.Key);
						if (foundMethod != null) {
							foundMethod.Name = entry.Value;
						} else {
							throw new Exception("Invalid Method mapping (" + entry.Key + " => " + entry.Value + "): Field not found");
						}
					}
				}

				module.Write(outputFile);
				module.Dispose();
			}
		}
	}