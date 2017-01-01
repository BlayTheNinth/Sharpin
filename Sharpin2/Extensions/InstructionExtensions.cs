using System;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Sharpin2 {

	public static class InstructionExtensions {

		public static Instruction ToModule(this Instruction inst, ModuleDefinition module) {
			if (inst.Operand is MethodReference) {
				inst.Operand = module.ImportReference(inst.Operand as MethodReference);
			} else if (inst.Operand is FieldReference) {
				inst.Operand = module.ImportReference(inst.Operand as FieldReference);
			} else if (inst.Operand is TypeReference) {
				inst.Operand = module.ImportReference(inst.Operand as TypeReference);
			} else {
				Console.WriteLine(inst.Operand);
			}
			inst.Offset = 0;
			return inst;
		}

		/// <summary>
		/// Checks an instruction for its behaviour towards the stack. This is used to determine a safe injection point before an instruction where a clear stack can be expected.
		/// </summary>
		/// <param name="inst">the instruction to be checked</param>
		/// <param name="targetModule"></param>
		/// <returns>true if this instruction pushes onto the stack</returns>
		/// <exception cref="NotSupportedException">if things go weird and need to be fixed in Sharpin</exception>
		public static bool IsLdInstruction(this Instruction inst, ModuleDefinition targetModule) {
			if (inst.OpCode.StackBehaviourPush == StackBehaviour.Varpush) {
				var operand = inst.Operand as MethodReference;
				if (operand != null) {
					return operand.ReturnType != targetModule.TypeSystem.Void;
				}

				throw new NotSupportedException("I'm not sure for which this one would happen anymore, so here, fix me: " + inst);
			}

			return inst.OpCode.StackBehaviourPush != StackBehaviour.Push0;
		}

	}

}