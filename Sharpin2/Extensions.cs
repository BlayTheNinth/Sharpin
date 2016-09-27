using System.Collections.Generic;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Sharpin2 {
    public static class Extensions {
        public static MethodReference MakeHostInstanceGeneric(this MethodReference self, params TypeReference[] arguments) {
            var reference = new MethodReference(self.Name, self.ReturnType, self.DeclaringType.MakeGenericInstanceType(arguments)) {
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention
            };

            foreach (var parameter in self.Parameters)
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

            foreach (var generic_parameter in self.GenericParameters)
                reference.GenericParameters.Add(new GenericParameter(generic_parameter.Name, reference));

            return reference;
        }

        public static Instruction ToModule(this Instruction inst, ModuleDefinition module) {
            if (inst.Operand is MethodReference) {
                inst.Operand = module.ImportReference(inst.Operand as MethodReference);
            } else if (inst.Operand is FieldReference) {
                inst.Operand = module.ImportReference(inst.Operand as FieldReference);
            } else if (inst.Operand is TypeReference) {
                inst.Operand = module.ImportReference(inst.Operand as TypeReference);
            }
            inst.Offset = 0;
            return inst;
        }

        public static VariableDefinition ToModule(this VariableDefinition local, ModuleDefinition module) {
            local.VariableType = module.ImportReference(local.VariableType);
            return local;
        }

        public static void InsertAfter(this ILProcessor il, Instruction after, List<Instruction> insnList) {
            var lastInst = after;
            foreach(var inst in insnList) {
                il.InsertAfter(lastInst, inst);
                lastInst = inst;
            }
        }

        public static void InsertBefore(this ILProcessor il, Instruction before, List<Instruction> insnList) {
            Instruction lastInst = null;
            foreach (var inst in insnList) {
                if (lastInst == null) {
                    il.InsertBefore(before, inst);
                } else {
                    il.InsertAfter(lastInst, inst);
                }
                lastInst = inst;
            }
        }

        public static ParameterDefinition ToModule(this ParameterDefinition def, ModuleDefinition module) {
            def.ParameterType = module.ImportReference(def.ParameterType);
            return def;
        }

        public static Instruction CreateLdarg(this ILProcessor il, int index) {
            switch (index) {
                case 0: return il.Create(OpCodes.Ldarg_0);
                case 1: return il.Create(OpCodes.Ldarg_1);
                case 2: return il.Create(OpCodes.Ldarg_2);
                case 3: return il.Create(OpCodes.Ldarg_3);
            }
            if(index < byte.MaxValue) {
                return il.Create(OpCodes.Ldarg_S, (byte) index);
            }
            return il.Create(OpCodes.Ldarg, (ushort) index);
        }

        public static Instruction CreateStloc(this ILProcessor il, VariableDefinition var, int index) {
            switch (index) {
                case 0:
                    return il.Create(OpCodes.Stloc_0);
                case 1:
                    return il.Create(OpCodes.Stloc_1);
                case 2:
                    return il.Create(OpCodes.Stloc_2);
                case 3:
                    return il.Create(OpCodes.Stloc_3);
            }
            if (index < byte.MaxValue) {
                return il.Create(OpCodes.Stloc_S, var);
            }
            return il.Create(OpCodes.Stloc, var);
        }

        public static Instruction CreateLdloc(this ILProcessor il, VariableDefinition var, int index) {
            switch (index) {
                case 0:
                    return il.Create(OpCodes.Ldloc_0);
                case 1:
                    return il.Create(OpCodes.Ldloc_1);
                case 2:
                    return il.Create(OpCodes.Ldloc_2);
                case 3:
                    return il.Create(OpCodes.Ldloc_3);
            }
            if (index < byte.MaxValue) {
                return il.Create(OpCodes.Ldloc_S, var);
            }
            return il.Create(OpCodes.Ldloc, var);
        }
    }
}
