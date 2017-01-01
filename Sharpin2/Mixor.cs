using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Sharpin2 {

	public class Mixor {
		private readonly MixinInfo _mixin;
		private readonly ModuleDefinition _targetModule;
		private readonly TypeDefinition _targetType;

		private readonly Dictionary<string, FieldDefinition> _fieldReferenceMap = new Dictionary<string, FieldDefinition>();

		public Mixor(MixinInfo mixin, ModuleDefinition targetModule) {
			_mixin = mixin;
			_targetModule = targetModule;
			TypeDefinition foundType;
			if (mixin.TargetType != null) {
				foundType = _targetModule.GetType(mixin.TargetType.FullName);
				if (foundType == null) {
					throw new MixinException("Target Type '" + mixin.TargetType.FullName + "' not found in target module for " + mixin.MixinContainer.FullName);
				}
			} else if (mixin.Target != null) {
				foundType = _targetModule.GetType(mixin.Target);
				if (foundType == null) {
					throw new MixinException("Target Type '" + mixin.Target + "' not found in target module for " + mixin.MixinContainer.FullName);
				}
			} else {
				throw new MixinException("Target Type not specified for " + mixin.MixinContainer.FullName);
			}

			_targetType = foundType;
		}

		public void Apply() {
			var implements = _mixin.MixinContainer.CustomAttributes.Where(a => a.AttributeType.FullName == typeof(Implements).FullName); // TODO the attribute will exist in the patchModule so just grab it from there and compare directly
			foreach (var implement in implements) {
				ApplyImplements(new ImplementsInfo(_targetType, implement));
			}

			var overwrites = _mixin.MixinContainer.Methods.Where(m => m.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(Overwrite).FullName)); // TODO the attribute will exist in the patchModule so just grab it from there and compare directly
			foreach (var overwrite in overwrites) {
				ApplyOverwrite(new OverwriteInfo(_targetType, overwrite));
			}

			var injects = _mixin.MixinContainer.Methods.Where(m => m.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(Inject).FullName)); // TODO the attribute will exist in the patchModule so just grab it from there and compare directly
			foreach (var inject in injects) {
				ApplyInject(new InjectInfo(inject));
			}
		}

		private void InitializeFieldReferenceMap() {
			if (_fieldReferenceMap.Count > 0) {
				throw new System.Exception("The field reference map may only be initialized once.");
			}

			foreach (var field in _mixin.MixinContainer.Fields) {
				if (field.HasAttribute(typeof(CaptureField))) {
					var captureField = new CaptureFieldInfo(field);
					var targetField = _targetType.Fields.SingleOrDefault(t => t.Name == captureField.Field);
					if (targetField == null) {
						throw new MixinException("Failed to capture field, " + captureField.Field + " not found in " + _targetType.FullName + " for " + _mixin.MixinContainer.FullName);
					}

					_fieldReferenceMap.Add(field.Name, targetField);
				} else {
					// TODO name collision check
					var targetField = new FieldDefinition(field.Name, field.Attributes, field.FieldType);
					// TODO all the other stuff that fields need
					_targetType.Fields.Add(targetField);
					_fieldReferenceMap.Add(field.Name, targetField);
				}
			}
		}

		private void ApplyInject(InjectInfo inject) {
			var targetMethod = _targetType.Methods.SingleOrDefault(t => t.FullName == inject.Method);
			if (targetMethod == null) {
				throw new MixinException("Target Method '" + inject.Method + "' not found in target module for [Inject] At " + inject.NewMethod.FullName + " in " + _mixin.MixinContainer.FullName);
			}

			if (inject.Cancellable) {
			} else {
				ApplyInjectSimple(inject, targetMethod);
			}
		}

		private void ApplyOverwrite(OverwriteInfo overwrite) {
			var targetMethod = _targetType.Methods.SingleOrDefault(m => m.FullName == overwrite.Target);
			if (targetMethod == null) {
				throw new MixinException("Target Method '" + overwrite.Target + "' not found in target module for [Overwrite] in " + _mixin.MixinContainer.FullName);
			}

			TransferMethod(targetMethod, overwrite.NewMethod);
		}

		private void ApplyImplements(ImplementsInfo implements) {
			var interfaceType = _targetModule.ImportReference(implements.TargetType);
			var impl = new InterfaceImplementation(interfaceType);
			_targetType.Interfaces.Add(impl);

			var interfaceTypeDef = interfaceType.Resolve();
			foreach (var expectedMethod in interfaceTypeDef.Methods) {
				string expectedParameters = string.Join(",", expectedMethod.Parameters.Select(t => t.ParameterType.FullName).ToArray()); // TODO disgusting
				var foundMethod = _mixin.MixinContainer.Methods.SingleOrDefault(t => t.Name == expectedMethod.Name && expectedParameters == string.Join(",", t.Parameters.Select(p => p.ParameterType.FullName).ToArray())); // TODO ewwwwww
				if (foundMethod == null) {
					throw new MixinException("Interface Method '" + expectedMethod.FullName + "' not found in mixin class for [Implements] in " + _mixin.MixinContainer.FullName);
				}
				// TODO the following should be replaced by a general "PortMethodToModule" thing but use ApplyInjectCancellable as base
				var newMethod = new MethodDefinition(foundMethod.Name, foundMethod.Attributes, _targetModule.ImportReference(foundMethod.ReturnType));
				newMethod.Body = new MethodBody(newMethod);
				foreach (var local in foundMethod.Body.Variables) {
					newMethod.Body.Variables.Add(local.ToModule(_targetModule));
				}

				var il = newMethod.Body.GetILProcessor();
				foreach (var inst in foundMethod.Body.Instructions) {
					il.Append(inst.ToModule(_targetModule));
				}

				_targetType.Methods.Add(newMethod);
			}
		}

		private void AddNewMethod(MethodDefinition newMethod) {
			var targetMethod = new MethodDefinition(_mixin.MixinContainer.Name + "_" + newMethod.Name, MethodAttributes.HideBySig, _targetModule.TypeSystem.Void);
			TransferMethod(targetMethod, newMethod);
			_targetType.Methods.Add(targetMethod);
		}

		private void TransferMethod(MethodDefinition newMethod, MethodDefinition sourceMethod) {
			// Just overwrite the old body
			newMethod.Body = new MethodBody(newMethod);

			foreach (var parameter in sourceMethod.Parameters) {
				newMethod.Parameters.Add(parameter.ToModule(_targetModule));
			}

			// Transfer local variables
			foreach (var local in sourceMethod.Body.Variables) {
				newMethod.Body.Variables.Add(local.ToModule(_targetModule));
			}

			// Port all instructions to the target method and replace field references with our field reference map.
			var il = newMethod.Body.GetILProcessor();
			foreach (var inst in sourceMethod.Body.Instructions) {
				var newInst = inst.ToModule(_targetModule);
				if (newInst.OpCode.OperandType == OperandType.InlineField) {
					FieldDefinition targetField;
					if (_fieldReferenceMap.TryGetValue(((FieldReference) newInst.Operand).Name, out targetField)) {
						newInst.Operand = targetField;
					}
				}
				il.Append(newInst);
			}
		}

		private void GrabLocalParameters(MethodDefinition targetMethod, MethodReference newMethod, out List<VariableDefinition> capturedLocals, out List<VariableDefinition> storedLocals) {
			capturedLocals = new List<VariableDefinition>();
			storedLocals = new List<VariableDefinition>();
			foreach (var parameter in newMethod.Parameters) {
				if (parameter.HasAttribute(typeof(CaptureLocal))) {
					var captureInfo = new CaptureLocalInfo(parameter);
					var local = targetMethod.Body.Variables[captureInfo.Index];
					if (local.VariableType.FullName != captureInfo.Type.FullName) {
						throw new MixinException("Failed to capture local, Type mismatch " + targetMethod.ReturnType.FullName + " at " + newMethod.FullName + " in " + _mixin.MixinContainer.FullName);
					}

					capturedLocals.Add(local);
					parameter.CustomAttributes.Remove(parameter.GetAttribute(typeof(CaptureLocal)));
				} else if (parameter.HasAttribute(typeof(StoreLocal))) {
					var storeInfo = new StoreLocalInfo(parameter);
					var local = targetMethod.Body.Variables[storeInfo.Index];
					if (local.VariableType.FullName != storeInfo.Type.FullName) {
						throw new MixinException("Failed to store local, Type mismatch " + targetMethod.ReturnType.FullName + " at " + newMethod.FullName + " in " + _mixin.MixinContainer.FullName);
					}

					storedLocals.Add(local);
					parameter.CustomAttributes.Remove(parameter.GetAttribute(typeof(StoreLocal)));
				}
			}
		}

		private bool CompareMethodParams(IMethodSignature targetMethod, IMethodSignature newMethod, IMetadataTokenProvider callbackInfoType) {
			// TODO callbackinfo yeah: .Where(t => t.ParameterType != callbackInfoType)
			// TODO ugly hack atm
			string targetMethodParams = string.Join(",", targetMethod.Parameters.Select(t => t.ParameterType.FullName).ToArray());
			string patchMethodParams = string.Join(",", newMethod.Parameters
				.Where(t
					=> t.CustomAttributes.All(a => a.AttributeType.FullName != typeof(CaptureLocal).FullName) &&
					   t.CustomAttributes.All(a => a.AttributeType.FullName != typeof(StoreLocal).FullName) &&
					   t.ParameterType != callbackInfoType)

				.Select(t => t.ParameterType.FullName)
				.ToArray());
			return patchMethodParams == targetMethodParams;
		}

		/// <summary>
		/// Returns a list of injection points based on the InjectInfo passed to the function.
		/// * For HEAD, the injection point will always be the first instruction, and there will only be one at all times.
		/// * For RETURN, there will be one injection point per ret opcode. The injection point will be moved right before something is loaded onto the stack.
		/// * Anything else is interpreted as IL-Code and will result in one injection point per matching instruction. The injection point will be moved right before something is loaded onto the stack.
		/// </summary>
		/// <returns>A list of instructions each serving as an injection point.</returns>
		private List<Instruction> FindInjectionPoint(InjectInfo inject, MethodDefinition targetMethod) {
			var list = new List<Instruction>();
			switch (inject.At) {
				case "HEAD":
					list.Add(targetMethod.Body.Instructions[0]);
					break;
				case "RETURN":
					foreach (var inst in targetMethod.Body.Instructions) {
						if (inst.OpCode == OpCodes.Ret) {
							var found = inst;
							while (found.Previous != null && found.Previous.IsLdInstruction(_targetModule)) {
								found = found.Previous;
							}

							list.Add(found);
						}
					}

					break;
				default:
					foreach (var inst in targetMethod.Body.Instructions) {
						string instString = inst.ToString();
						if (inject.At == instString) {
							var found = inst;
							while (found.Previous != null && found.Previous.IsLdInstruction(_targetModule)) {
								found = found.Previous;
							}

							list.Add(found);
							break;
						}
					}

					break;
			}

			return list;
		}

		private MethodDefinition CreateCallbackMethod(InjectInfo inject, MethodDefinition targetMethod, IMetadataTokenProvider callbackInfoType, out List<VariableDefinition> capturedLocals, out List<VariableDefinition> storedLocals) {
			if (!CompareMethodParams(targetMethod, inject.NewMethod, callbackInfoType)) {
				throw new MixinException("Target Method has mismatching parameters " + targetMethod.ReturnType.FullName + " At " + inject.NewMethod.FullName + " in " + _mixin.MixinContainer.FullName);
			}

			var callbackMethod = new MethodDefinition(_mixin.MixinContainer.Name + "_" + inject.NewMethod.Name, MethodAttributes.HideBySig, _targetModule.TypeSystem.Void);

			TransferMethod(callbackMethod, inject.NewMethod);

			GrabLocalParameters(targetMethod, callbackMethod, out capturedLocals, out storedLocals);

			_targetType.Methods.Add(callbackMethod);
			return callbackMethod;
		}

		private void PatchMethod(InjectInfo inject, MethodDefinition targetMethod, ILProcessor il, List<Instruction> patchBody) {
			// Time to actually apply the patch at its injection points
			var atList = FindInjectionPoint(inject, targetMethod);
			// If the amount of injection points does not match expectations, abort - target code has likely changed
			if (atList.Count != inject.ExpectedInjections) {
				throw new MixinInjectionException("Expected an injection count of " + inject.ExpectedInjections + " but got " + atList.Count + " candidates in " + targetMethod.FullName + " at " + inject.NewMethod.FullName + " in " + _mixin.MixinContainer.FullName, targetMethod);
			}
			// Update all jumps to injection point(s) to now jump to our hook instead (so they won't just skip past us)
			foreach (var at in atList) {
				var newLabelTarget = patchBody[0];
				foreach (var inst in targetMethod.Body.Instructions) {
					if (inst.OpCode.OperandType == OperandType.InlineBrTarget || inst.OpCode.OperandType == OperandType.ShortInlineBrTarget) {
						if (inst.Operand == at) {
							inst.Operand = newLabelTarget;
						}
					}
				}

				il.InsertBefore(at, patchBody);
			}
		}

		private void ApplyInjectSimple(InjectInfo inject, MethodDefinition targetMethod) {
			List<VariableDefinition> capturedLocals, storedLocals;
			var callbackMethod = CreateCallbackMethod(inject, targetMethod, null, out capturedLocals, out storedLocals);

			// Time to build patch
			var il = targetMethod.Body.GetILProcessor();
			// ReSharper disable once UseObjectOrCollectionInitializer
			var insnList = new List<Instruction>();

			// 2. Load this, all parameters and the captured locals onto the stack and call the callback Method
			insnList.Add(il.CreateLdarg(0));
			for (int i = 0; i < targetMethod.Parameters.Count; i++) {
				insnList.Add(il.CreateLdarg(i + 1));
			}

			insnList.AddRange(capturedLocals.Select(local => il.CreateLdloc(local, local.Index)));
			insnList.AddRange(storedLocals.Select(local => local.Index > byte.MaxValue
				? il.Create(OpCodes.Ldloca, local)
				: il.Create(OpCodes.Ldloca_S, local)));

			insnList.Add(il.Create(OpCodes.Call, callbackMethod));

			PatchMethod(inject, targetMethod, il, insnList);
		}

		private void ApplyInjectCancellable(InjectInfo inject, MethodDefinition targetMethod) {
			// Ensure the patch method has parameters - Cancellable means the last parameter MUST be CallbackInfo or CallbackInfoReturnable
			if (inject.NewMethod.Parameters.Count == 0) {
				throw new MixinException("Expected last parameter of " + inject.NewMethod.FullName + " to be CallbackInfo or CallbackInfoReturnable in " + _mixin.MixinContainer.FullName);
			}

			// Retrieve the CallbackInfo Type from the last parameter of the patch Method
			var callbackInfoType = inject.NewMethod.Parameters.Last().ParameterType;
			var callbackInfoTypeDef = callbackInfoType.Resolve();

			// Ensure the last parameter is indeed CallbackInfo or CallbackInfoReturnable
			if (callbackInfoType.FullName != typeof(CallbackInfo).FullName && (callbackInfoTypeDef.BaseType == null || callbackInfoTypeDef.BaseType.FullName != typeof(CallbackInfo).FullName)) {
				// TODO string comp not needed, patch knows these attributes
				throw new MixinException("Expected last parameter of " + inject.NewMethod.FullName + " to be CallbackInfo or CallbackInfoReturnable in " + _mixin.MixinContainer.FullName);
			}

			// If CallbackInfo is a generic instance (so CallbackInfoReturnable), ensure the return Type matches
			if (callbackInfoType.IsGenericInstance) {
				if (targetMethod.ReturnType.FullName != ((GenericInstanceType) callbackInfoType).GenericArguments[0].FullName) {
					throw new MixinException("Target Method has mismatching return Type " + targetMethod.ReturnType.FullName + " at " + inject.NewMethod.FullName + " in " + _mixin.MixinContainer.FullName);
				}
			} else if (targetMethod.ReturnType != _targetModule.TypeSystem.Void) {
				// It's not CallbackInfoReturnable even though the Method has a return value, this is not allowed
				throw new MixinException("Target Method has return Type " + targetMethod.ReturnType.FullName + " but CallbackInfo was specified, expected CallbackInfoReturnable at " + inject.NewMethod.FullName + " in " + _mixin.MixinContainer.FullName);
			}

			List<VariableDefinition> capturedLocals, storedLocals;
			var callbackMethod = CreateCallbackMethod(inject, targetMethod, callbackInfoType, out capturedLocals, out storedLocals);

			// Append a local variable for the CallbackInfo to the target method (this is where we store the CallbackInfo object that is passed to the injected hook)
			var callbackInfo = new VariableDefinition(_targetModule.ImportReference(callbackInfoType));
			int callbackInfoIndex = targetMethod.Body.Variables.Count;
			targetMethod.Body.Variables.Add(callbackInfo);

			// Grab the CallbackInfo constructor (gets a generic constructor for CallbackInfoReturnable)
			MethodReference callbackInfoConstructor = callbackInfoTypeDef.Methods.Single(m => m.IsConstructor);
			if (callbackInfoType.IsGenericInstance) {
				callbackInfoConstructor = callbackInfoConstructor.MakeHostInstanceGeneric(targetMethod.ReturnType);
			}
			callbackInfoConstructor = _targetModule.ImportReference(callbackInfoConstructor);

			// Time to build patch
			var il = targetMethod.Body.GetILProcessor();
			// ReSharper disable once UseObjectOrCollectionInitializer
			var insnList = new List<Instruction>();

			// 1. Create CallbackInfo object and store it as a local
			insnList.Add(il.Create(OpCodes.Newobj, callbackInfoConstructor));
			insnList.Add(il.CreateStloc(callbackInfo, callbackInfoIndex));

			// 2. Load this, all parameters, the CallbackInfo, and the captured locals onto the stack and call the callback Method
			insnList.Add(il.CreateLdarg(0));
			for (int i = 0; i < targetMethod.Parameters.Count; i++) {
				insnList.Add(il.CreateLdarg(i + 1));
			}

			insnList.Add(il.CreateLdloc(callbackInfo, callbackInfoIndex));
			insnList.AddRange(capturedLocals.Select(local => il.CreateLdloc(local, local.Index)));
			insnList.AddRange(storedLocals.Select(local => local.Index > byte.MaxValue
				? il.Create(OpCodes.Ldloca, local)
				: il.Create(OpCodes.Ldloca_S, local)));
			insnList.Add(il.Create(OpCodes.Call, callbackMethod));

			// 3. If CallbackInfo was cancelled, return here, possibly with return value. Otherwise, skip until after the return to continue normally.
			insnList.Add(il.CreateLdloc(callbackInfo, callbackInfoIndex));
			insnList.Add(callbackInfoType.IsGenericInstance
				? il.Create(OpCodes.Callvirt, _targetModule.ImportReference(callbackInfoTypeDef.BaseType.Resolve().Methods.Single(m => m.Name == "get_IsCancelled")))
				: il.Create(OpCodes.Callvirt, _targetModule.ImportReference(callbackInfoTypeDef.Methods.Single(m => m.Name == "get_IsCancelled"))));
			if (inject.CancelTarget == "ret") {
				// 3.1. In case of a 'ret' cancel target, we want to exit the function - if cancelled, jump to the NOP after the RET, otherwise just run into the RET and grab the return value if necessary.
				var nopAfterReturn = il.Create(OpCodes.Nop);
				insnList.Add(il.Create(OpCodes.Brfalse, nopAfterReturn));
				if (targetMethod.ReturnType != _targetModule.TypeSystem.Void) {
					insnList.Add(il.CreateLdloc(callbackInfo, callbackInfoIndex));
					insnList.Add(il.Create(OpCodes.Callvirt, _targetModule.ImportReference(callbackInfoTypeDef.Methods.Single(m => m.Name == "get_ReturnValue").MakeHostInstanceGeneric(targetMethod.ReturnType))));
				}
				insnList.Add(il.Create(OpCodes.Ret));
				insnList.Add(nopAfterReturn);
			} else {
				// 3.2. This means the hook wants us to jump somewhere else within the function (as in, any other instruction traced back to empty stack)
				Instruction found = null;
				foreach (var inst in targetMethod.Body.Instructions) {
					string instString = inst.ToString();
					if (inject.CancelTarget == instString) {
						found = inst;
						while (found.Previous != null && found.Previous.IsLdInstruction(_targetModule)) {
							found = found.Previous;
						}
					}
				}

				if (found == null) {
					throw new MixinException("Could not find cancel target '" + inject.CancelTarget + "' in " + targetMethod.FullName + " at " + inject.NewMethod.FullName + " in " + _mixin.MixinContainer.FullName);
				}

				insnList.Add(il.Create(OpCodes.Brtrue, found));
			}

			PatchMethod(inject, targetMethod, il, insnList);
		}
	}

}