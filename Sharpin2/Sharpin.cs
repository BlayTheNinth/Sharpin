using System;
using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Sharpin2 {
    public class Sharpin {

        private readonly List<MixinInfo> mixins = new List<MixinInfo>();
        private readonly AssemblyResolver assemblyResolver = new AssemblyResolver();
        private readonly ModuleDefinition targetModule;
        private readonly ModuleDefinition patchModule;

        public Sharpin(string targetLibrary, string patchLibrary) {
            this.targetModule = ModuleDefinition.ReadModule(targetLibrary, new ReaderParameters { AssemblyResolver = assemblyResolver });
            assemblyResolver.AddToCache(this.targetModule);
            this.patchModule = ModuleDefinition.ReadModule(patchLibrary, new ReaderParameters { AssemblyResolver = assemblyResolver });
            assemblyResolver.AddToCache(this.patchModule);
        }

        public void GatherMixins() {
            var mixinContainers = patchModule.Types.Where(t => t.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(Mixin).FullName));
            foreach (var mixin in mixinContainers) {
                mixins.Add(new MixinInfo(mixin));
            }
            mixins.Sort();
        }

        public void ApplyMixins() {
            foreach (var mixin in mixins) {
                ApplyMixin(mixin);
            }
        }

        public void Write(string outputFile) {
            targetModule.Write(outputFile);
        }

        private void ApplyMixin(MixinInfo mixin) {
            TypeDefinition foundType = null;
            if (mixin.TargetType != null) {
                foundType = this.targetModule.GetType(mixin.TargetType.FullName);
                if (foundType == null) {
                    throw new MixinException("Target type '" + mixin.TargetType.FullName + "' not found in target module for " + mixin.MixinContainer.FullName);
                }
            } else if (mixin.Target != null) {
                foundType = this.targetModule.GetType(mixin.Target);
                if (foundType == null) {
                    throw new MixinException("Target type '" + mixin.Target + "' not found in target module for " + mixin.MixinContainer.FullName);
                }
            } else {
                throw new MixinException("Target type not specified for " + mixin.MixinContainer.FullName);
            }
            ApplyMixinToType(mixin, foundType);
        }

        private void ApplyMixinToType(MixinInfo mixin, TypeDefinition target) {
            var overwrites = mixin.MixinContainer.Methods.Where(m => m.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(Overwrite).FullName));
            foreach (var overwrite in overwrites) {
                ApplyOverwrite(mixin, target, new OverwriteInfo(target, overwrite));
            }
            var injects = mixin.MixinContainer.Methods.Where(m => m.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(Inject).FullName));
            foreach (var inject in injects) {
                ApplyInject(mixin, target, new InjectInfo(inject));
            }
        }

        private void ApplyInject(MixinInfo mixin, TypeDefinition targetType, InjectInfo inject) {
            var targetMethod = targetType.Methods.SingleOrDefault(t => t.FullName == inject.Method);
            if (targetMethod == null) {
                throw new MixinException("Target method '" + inject.Method + "' not found in target module for [Inject] at " + inject.NewMethod.FullName + " in " + mixin.MixinContainer.FullName);
            }
            if (inject.Cancellable) {
                ApplyInjectCancellable(mixin, targetType, targetMethod, inject);
            } else {
                ApplyInjectSimple(mixin, targetType, targetMethod, inject);
            }
        }

        private void ApplyInjectCancellable(MixinInfo mixin, TypeDefinition targetType, MethodDefinition targetMethod, InjectInfo inject) {
            // Ensure the patch method has parameters - cancellable means the last parameter MUST be CallbackInfo or CallbackInfoReturnable
            if (inject.NewMethod.Parameters.Count == 0) {
                throw new MixinException("Expected last parameter of " + inject.NewMethod.FullName + " to be CallbackInfo or CallbackInfoReturnable in " + mixin.MixinContainer.FullName);
            }

            // Retrieve the CallbackInfo type from the last parameter of the patch method
            var callbackInfoType = inject.NewMethod.Parameters.Last().ParameterType;
            var callbackInfoTypeDef = callbackInfoType.Resolve();

            // Ensure the last parameter is indeed CallbackInfo or CallbackInfoReturnable
            if (callbackInfoType.FullName != typeof(CallbackInfo).FullName && (callbackInfoTypeDef.BaseType == null || callbackInfoTypeDef.BaseType.FullName != typeof(CallbackInfo).FullName)) {
                throw new MixinException("Expected last parameter of " + inject.NewMethod.FullName + " to be CallbackInfo or CallbackInfoReturnable in " + mixin.MixinContainer.FullName);
            }

            // If CallbackInfo is a generic instance (so CallbackInfoReturnable), ensure the return type matches
            if (callbackInfoType.IsGenericInstance) {
                if (targetMethod.ReturnType.FullName != ((GenericInstanceType) callbackInfoType).GenericArguments[0].FullName) {
                    throw new MixinException("Target method has mismatching return type " + targetMethod.ReturnType.FullName + " at " + inject.NewMethod.FullName + " in " + mixin.MixinContainer.FullName);
                }
            } else if (targetMethod.ReturnType != targetModule.TypeSystem.Void) {
                // It's not CallbackInfoReturnable even though the method has a return value, this is not allowed
                throw new MixinException("Target method has return type " + targetMethod.ReturnType.FullName + " but CallbackInfo was specified, expected CallbackInfoReturnable at " + inject.NewMethod.FullName + " in " + mixin.MixinContainer.FullName);
            }

            // Retrieve the method signatures and compare them (minus the CallbackInfo)
            var targetMethodParams = string.Join(",", targetMethod.Parameters.Select(t => t.ParameterType.FullName));
            var patchMethodParams = string.Join(",", inject.NewMethod.Parameters.Where(t => t.ParameterType != callbackInfoType).Select(t => t.ParameterType.FullName));
            if (patchMethodParams != targetMethodParams) {
                throw new MixinException("Target method has mismatching parameters " + targetMethod.ReturnType.FullName + " at " + inject.NewMethod.FullName + " in " + mixin.MixinContainer.FullName);
            }

            // Create the callback method based on the patch method
            var callbackMethod = new MethodDefinition(mixin.MixinContainer.Name + "_" + inject.NewMethod.Name, MethodAttributes.HideBySig, targetModule.TypeSystem.Void);
            var capturedLocals = new List<VariableDefinition>();
            var storedLocals = new List<VariableDefinition>();
            foreach (var parameter in inject.NewMethod.Parameters) {
                callbackMethod.Parameters.Add(parameter.ToModule(targetModule));
                if (AttrHelper.HasAttribute(parameter, typeof(CaptureLocal))) {
                    var captureInfo = new CaptureLocalInfo(parameter);
                    var local = targetMethod.Body.Variables[captureInfo.Index];
                    if (local.VariableType.FullName != captureInfo.Type.FullName) {
                        throw new MixinException("Failed to capture local, type mismatch " + targetMethod.ReturnType.FullName + " at " + inject.NewMethod.FullName + " in " + mixin.MixinContainer.FullName);
                    }
                    capturedLocals.Add(local);
                    parameter.CustomAttributes.Remove(parameter.GetAttribute(typeof(CaptureLocal)));
                } else if (AttrHelper.HasAttribute(parameter, typeof(StoreLocal))) {
                    var storeInfo = new StoreLocalInfo(parameter);
                    var local = targetMethod.Body.Variables[storeInfo.Index];
                    if (local.VariableType.FullName != storeInfo.Type.FullName) {
                        throw new MixinException("Failed to store local, type mismatch " + targetMethod.ReturnType.FullName + " at " + inject.NewMethod.FullName + " in " + mixin.MixinContainer.FullName);
                    }
                    storedLocals.Add(local);
                    parameter.CustomAttributes.Remove(parameter.GetAttribute(typeof(StoreLocal)));
                }
            }
            var il = callbackMethod.Body.GetILProcessor();
            foreach (var inst in inject.NewMethod.Body.Instructions) {
                il.Append(inst.ToModule(targetModule));
            }
            targetType.Methods.Add(callbackMethod);

            // Append a local variable for the CallbackInfo to the target method
            var callbackInfo = new VariableDefinition(targetModule.ImportReference(callbackInfoType));
            int callbackInfoIndex = targetMethod.Body.Variables.Count;
            targetMethod.Body.Variables.Add(callbackInfo);

            // Grab the CallbackInfo constructor and make sure it's generic for CallbackInfoReturnable
            MethodReference callbackInfoConstructor = callbackInfoTypeDef.Methods.Single(m => m.IsConstructor);
            if (callbackInfoType.IsGenericInstance) {
                callbackInfoConstructor = callbackInfoConstructor.MakeHostInstanceGeneric(targetMethod.ReturnType);
            }
            callbackInfoConstructor = targetModule.ImportReference(callbackInfoConstructor);

            // Time to build patch
            il = targetMethod.Body.GetILProcessor();
            var insnList = new List<Instruction>();

            // 1. Create CallbackInfo object and store it as a local
            insnList.Add(il.Create(OpCodes.Newobj, callbackInfoConstructor));
            insnList.Add(il.CreateStloc(callbackInfo, callbackInfoIndex));

            // 2. Load this, all parameters, the CallbackInfo and the captured locals onto the stack and call the callback method
            insnList.Add(il.CreateLdarg(0));
            for (int i = 0; i < targetMethod.Parameters.Count; i++) {
                insnList.Add(il.CreateLdarg(i + 1));
            }
            insnList.Add(il.CreateLdloc(callbackInfo, callbackInfoIndex));
            foreach (var local in capturedLocals) {
                insnList.Add(il.CreateLdloc(local, local.Index));
            }
            foreach (var local in storedLocals) {
                if (local.Index > byte.MaxValue) {
                    insnList.Add(il.Create(OpCodes.Ldloca, local));
                } else {
                    insnList.Add(il.Create(OpCodes.Ldloca_S, local));
                }
            }
            insnList.Add(il.Create(OpCodes.Call, callbackMethod));

            // 3. If CallbackInfo was cancelled, return here, possibly with return value. Otherwise, skip until after the return to continue normally.
            insnList.Add(il.CreateLdloc(callbackInfo, callbackInfoIndex));
            if (callbackInfoType.IsGenericInstance) {
                insnList.Add(il.Create(OpCodes.Callvirt, targetModule.ImportReference(callbackInfoTypeDef.BaseType.Resolve().Methods.Single(m => m.Name == "get_IsCancelled"))));
            } else {
                insnList.Add(il.Create(OpCodes.Callvirt, targetModule.ImportReference(callbackInfoTypeDef.Methods.Single(m => m.Name == "get_IsCancelled"))));
            }
            if (inject.CancelTarget == "ret") {
                Instruction nopAfterReturn = il.Create(OpCodes.Nop);
                insnList.Add(il.Create(OpCodes.Brfalse, nopAfterReturn));
                if (targetMethod.ReturnType != targetModule.TypeSystem.Void) {
                    insnList.Add(il.CreateLdloc(callbackInfo, callbackInfoIndex));
                    insnList.Add(il.Create(OpCodes.Callvirt, targetModule.ImportReference(callbackInfoTypeDef.Methods.Single(m => m.Name == "get_ReturnValue").MakeHostInstanceGeneric(targetMethod.ReturnType))));
                }
                insnList.Add(il.Create(OpCodes.Ret));
                insnList.Add(nopAfterReturn);
            } else {
                Instruction found = null;
                foreach (Instruction inst in targetMethod.Body.Instructions) {
                    var instString = inst.ToString();
                    if (inject.CancelTarget == instString) {
                        found = inst;
                        while (found.Previous != null && IsLdOpCode(found.Previous.OpCode)) {
                            found = found.Previous;
                        }
                    }
                }
                if (found == null) {
                    throw new MixinException("Could not find cancel target '" + inject.CancelTarget + "' in " + targetMethod.FullName + " at " + inject.NewMethod.FullName + " in " + mixin.MixinContainer.FullName);
                }
                insnList.Add(il.Create(OpCodes.Brtrue, found));
            }

            // Time to apply the patch
            List<Instruction> atList = FindInjectionPoint(inject, targetMethod);
            if (atList.Count != inject.ExpectedInjections) {
                throw new MixinException("Expected a maximum injection count of " + inject.ExpectedInjections + " but got " + atList.Count + " candidates in " + targetMethod.FullName + " at " + inject.NewMethod.FullName + " in " + mixin.MixinContainer.FullName);
            }
            foreach (var at in atList) {
                var newLabelTarget = insnList[0];
                foreach (var inst in targetMethod.Body.Instructions) {
                    if (inst.OpCode.OperandType == OperandType.InlineBrTarget || inst.OpCode.OperandType == OperandType.ShortInlineBrTarget) {
                        inst.Operand = newLabelTarget;
                    }
                }
                il.InsertBefore(at, insnList);
            }
        }

        private void ApplyInjectSimple(MixinInfo mixin, TypeDefinition targetType, MethodDefinition targetMethod, InjectInfo inject) {
            // Retrieve the method signatures and compare them (minus the CallbackInfo)
            var targetMethodParams = string.Join(",", targetMethod.Parameters.Select(t => t.ParameterType.FullName));
            var patchMethodParams = string.Join(",", inject.NewMethod.Parameters
                .Where(t
                    => !t.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(CaptureLocal).FullName)
                    && !t.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(StoreLocal).FullName))
                .Select(t => t.ParameterType.FullName));
            if (patchMethodParams != targetMethodParams) {
                throw new MixinException("Target method has mismatching parameters " + targetMethod.ReturnType.FullName + " at " + inject.NewMethod.FullName + " in " + mixin.MixinContainer.FullName);
            }

            // Create the callback method based on the patch method
            var callbackMethod = new MethodDefinition(mixin.MixinContainer.Name + "_" + inject.NewMethod.Name, MethodAttributes.HideBySig, targetModule.TypeSystem.Void);
            var capturedLocals = new List<VariableDefinition>();
            var storedLocals = new List<VariableDefinition>();
            foreach (var parameter in inject.NewMethod.Parameters) {
                callbackMethod.Parameters.Add(parameter.ToModule(targetModule));
                if (AttrHelper.HasAttribute(parameter, typeof(CaptureLocal))) {
                    var captureInfo = new CaptureLocalInfo(parameter);
                    var local = targetMethod.Body.Variables[captureInfo.Index];
                    if (local.VariableType.FullName != captureInfo.Type.FullName) {
                        throw new MixinException("Failed to capture local, type mismatch " + targetMethod.FullName + " at " + inject.NewMethod.FullName + " in " + mixin.MixinContainer.FullName);
                    }
                    capturedLocals.Add(local);
                    parameter.CustomAttributes.Remove(parameter.GetAttribute(typeof(CaptureLocal)));
                } else if (AttrHelper.HasAttribute(parameter, typeof(StoreLocal))) {
                    var storeInfo = new StoreLocalInfo(parameter);
                    var local = targetMethod.Body.Variables[storeInfo.Index];
                    if (local.VariableType.FullName != storeInfo.Type.FullName) {
                        throw new MixinException("Failed to store local, type mismatch " + targetMethod.FullName + " at " + inject.NewMethod.FullName + " in " + mixin.MixinContainer.FullName);
                    }
                    storedLocals.Add(local);
                    parameter.CustomAttributes.Remove(parameter.GetAttribute(typeof(StoreLocal)));
                }
            }


            var il = callbackMethod.Body.GetILProcessor();
            foreach (var inst in inject.NewMethod.Body.Instructions) {
                il.Append(inst.ToModule(targetModule));
            }
            targetType.Methods.Add(callbackMethod);

            // Time to build patch
            il = targetMethod.Body.GetILProcessor();
            var insnList = new List<Instruction>();

            // 2. Load this, all parameters and the captured locals onto the stack and call the callback method
            insnList.Add(il.CreateLdarg(0));
            for (int i = 0; i < targetMethod.Parameters.Count; i++) {
                insnList.Add(il.CreateLdarg(i + 1));
            }
            foreach (var local in capturedLocals) {
                insnList.Add(il.CreateLdloc(local, local.Index));
            }
            foreach (var local in storedLocals) {
                if (local.Index > byte.MaxValue) {
                    insnList.Add(il.Create(OpCodes.Ldloca, local));
                } else {
                    insnList.Add(il.Create(OpCodes.Ldloca_S, local));
                }
            }
            insnList.Add(il.Create(OpCodes.Call, callbackMethod));

            // Time to apply the patch
            List<Instruction> atList = FindInjectionPoint(inject, targetMethod);
            if (atList.Count != inject.ExpectedInjections) {
                throw new MixinException("Expected a maximum injection count of " + inject.ExpectedInjections + " but got " + atList.Count + " candidates in " + targetMethod.FullName + " at " + inject.NewMethod.FullName + " in " + mixin.MixinContainer.FullName);
            }
            foreach (var at in atList) {
                var newLabelTarget = insnList[0];
                foreach (var inst in targetMethod.Body.Instructions) {
                    if (inst.OpCode.OperandType == OperandType.InlineBrTarget || inst.OpCode.OperandType == OperandType.ShortInlineBrTarget) {
                        inst.Operand = newLabelTarget;
                    }
                }
                il.InsertBefore(at, insnList);
            }
        }

        private static List<Instruction> FindInjectionPoint(InjectInfo inject, MethodDefinition targetMethod) {
            List<Instruction> list = new List<Instruction>();
            if (inject.At == "HEAD") {
                list.Add(targetMethod.Body.Instructions[0]);
            } else if (inject.At == "RETURN") {
                foreach (Instruction inst in targetMethod.Body.Instructions) {
                    if (inst.OpCode == OpCodes.Ret) {
                        Instruction found = inst;
                        while (found.Previous != null && IsLdOpCode(found.Previous.OpCode)) {
                            found = found.Previous;
                        }
                        list.Add(found);
                    }
                }
            } else {
                foreach (Instruction inst in targetMethod.Body.Instructions) {
                    var instString = inst.ToString();
                    if (inject.At == instString) {
                        Instruction found = inst;
                        while (found.Previous != null && IsLdOpCode(found.Previous.OpCode)) {
                            found = found.Previous;
                        }
                        list.Add(found);
                    }
                }
            }
            return list;
        }

        private static bool IsLdOpCode(OpCode opCode) {
            return opCode.StackBehaviourPush != StackBehaviour.Push0;
        }

        private void ApplyOverwrite(MixinInfo mixin, TypeDefinition target, OverwriteInfo overwrite) {
            var targetMethod = target.Methods.SingleOrDefault(m => m.FullName == overwrite.Target);
            if (targetMethod == null) {
                throw new MixinException("Target method '" + overwrite.Target + "' not found in target module for [Overwrite] in " + mixin.MixinContainer.FullName);
            }
            targetMethod.Body.Variables.Clear();
            foreach (var local in overwrite.NewMethod.Body.Variables) {
                targetMethod.Body.Variables.Add(local);
            }
            targetMethod.Body.Instructions.Clear();
            var il = targetMethod.Body.GetILProcessor();
            foreach (var inst in overwrite.NewMethod.Body.Instructions) {
                il.Append(inst.ToModule(targetModule));
            }
        }

    }
}
