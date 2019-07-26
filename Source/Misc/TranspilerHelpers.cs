// Currently disable because it does not play well with Harmony

using Harmony;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace TranspilerHelpers
{
	public class Instruction : CodeInstruction
	{
		public Instruction(OpCode opcode, object operand = null) : base(opcode, operand) { }
		public Instruction(CodeInstruction instruction) : base(instruction) { }

		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override string ToString()
		{
			return base.ToString();
		}

		// conversion

		public static implicit operator Instruction(OpCode opcode)
		{
			return new Instruction(opcode);
		}

		public static explicit operator OpCode(Instruction instruction)
		{
			return instruction.opcode;
		}

		// compare Instruction/Instruction

		public static bool operator ==(Instruction instruction1, Instruction instruction2)
		{
			return instruction1.opcode == instruction2.opcode && instruction1.operand == instruction2.operand;
		}

		public static bool operator !=(Instruction instruction1, Instruction instruction2)
		{
			return instruction1.opcode != instruction2.opcode || instruction1.operand != instruction2.operand;
		}

		// compare Instruction/OpCode

		public static bool operator ==(Instruction instruction, OpCode opcode)
		{
			return instruction.opcode == opcode;
		}

		public static bool operator !=(Instruction instruction, OpCode opcode)
		{
			return instruction.opcode != opcode;
		}

		// compare OpCode/Instruction

		public static bool operator ==(OpCode opcode, Instruction instruction)
		{
			return instruction.opcode == opcode;
		}

		public static bool operator !=(OpCode opcode, Instruction instruction)
		{
			return instruction.opcode != opcode;
		}

		// adding

		public static Instruction operator +(Instruction instruction, Label label)
		{
			instruction.labels.Add(label);
			return instruction;
		}

		public static Instruction operator +(Instruction instruction, object operand)
		{
			instruction.operand = operand;
			return instruction;
		}
	}

	class TranspilerHelpers
	{
		public static Instruction Op(this OpCode opcode, object operand = null, Label label = default)
		{
			if (operand is Delegate delegateOperand)
				operand = delegateOperand.Method;
			var instruction = new Instruction(opcode, operand);
			if (label != default)
				instruction.labels.Add(label);
			return instruction;
		}

		public static IEnumerable<Instruction> Yield_ExitMethodIf(this ILGenerator generator, bool condition)
		{
			var label = generator.DefineLabel();
			var branch = condition ? Code.Brfalse : Code.Brtrue;
			yield return branch + (object)label;
			yield return Code.Ret;
			yield return Code.Nop + label;
		}
	}

	public class Code
	{
		public static readonly Instruction Nop = new Instruction(Nop);
		public static readonly Instruction Break = new Instruction(Break);
		public static readonly Instruction Ldarg_0 = new Instruction(Ldarg_0);
		public static readonly Instruction Ldarg_1 = new Instruction(Ldarg_1);
		public static readonly Instruction Ldarg_2 = new Instruction(Ldarg_2);
		public static readonly Instruction Ldarg_3 = new Instruction(Ldarg_3);
		public static readonly Instruction Ldloc_0 = new Instruction(Ldloc_0);
		public static readonly Instruction Ldloc_1 = new Instruction(Ldloc_1);
		public static readonly Instruction Ldloc_2 = new Instruction(Ldloc_2);
		public static readonly Instruction Ldloc_3 = new Instruction(Ldloc_3);
		public static readonly Instruction Stloc_0 = new Instruction(Stloc_0);
		public static readonly Instruction Stloc_1 = new Instruction(Stloc_1);
		public static readonly Instruction Stloc_2 = new Instruction(Stloc_2);
		public static readonly Instruction Stloc_3 = new Instruction(Stloc_3);
		public static readonly Instruction Ldarg_S = new Instruction(Ldarg_S);
		public static readonly Instruction Ldarga_S = new Instruction(Ldarga_S);
		public static readonly Instruction Starg_S = new Instruction(Starg_S);
		public static readonly Instruction Ldloc_S = new Instruction(Ldloc_S);
		public static readonly Instruction Ldloca_S = new Instruction(Ldloca_S);
		public static readonly Instruction Stloc_S = new Instruction(Stloc_S);
		public static readonly Instruction Ldnull = new Instruction(Ldnull);
		public static readonly Instruction Ldc_I4_M1 = new Instruction(Ldc_I4_M1);
		public static readonly Instruction Ldc_I4_0 = new Instruction(Ldc_I4_0);
		public static readonly Instruction Ldc_I4_1 = new Instruction(Ldc_I4_1);
		public static readonly Instruction Ldc_I4_2 = new Instruction(Ldc_I4_2);
		public static readonly Instruction Ldc_I4_3 = new Instruction(Ldc_I4_3);
		public static readonly Instruction Ldc_I4_4 = new Instruction(Ldc_I4_4);
		public static readonly Instruction Ldc_I4_5 = new Instruction(Ldc_I4_5);
		public static readonly Instruction Ldc_I4_6 = new Instruction(Ldc_I4_6);
		public static readonly Instruction Ldc_I4_7 = new Instruction(Ldc_I4_7);
		public static readonly Instruction Ldc_I4_8 = new Instruction(Ldc_I4_8);
		public static readonly Instruction Ldc_I4_S = new Instruction(Ldc_I4_S);
		public static readonly Instruction Ldc_I4 = new Instruction(Ldc_I4);
		public static readonly Instruction Ldc_I8 = new Instruction(Ldc_I8);
		public static readonly Instruction Ldc_R4 = new Instruction(Ldc_R4);
		public static readonly Instruction Ldc_R8 = new Instruction(Ldc_R8);
		public static readonly Instruction Dup = new Instruction(Dup);
		public static readonly Instruction Pop = new Instruction(Pop);
		public static readonly Instruction Jmp = new Instruction(Jmp);
		public static readonly Instruction Call = new Instruction(Call);
		public static readonly Instruction Calli = new Instruction(Calli);
		public static readonly Instruction Ret = new Instruction(Ret);
		public static readonly Instruction Br_S = new Instruction(Br_S);
		public static readonly Instruction Brfalse_S = new Instruction(Brfalse_S);
		public static readonly Instruction Brtrue_S = new Instruction(Brtrue_S);
		public static readonly Instruction Beq_S = new Instruction(Beq_S);
		public static readonly Instruction Bge_S = new Instruction(Bge_S);
		public static readonly Instruction Bgt_S = new Instruction(Bgt_S);
		public static readonly Instruction Ble_S = new Instruction(Ble_S);
		public static readonly Instruction Blt_S = new Instruction(Blt_S);
		public static readonly Instruction Bne_Un_S = new Instruction(Bne_Un_S);
		public static readonly Instruction Bge_Un_S = new Instruction(Bge_Un_S);
		public static readonly Instruction Bgt_Un_S = new Instruction(Bgt_Un_S);
		public static readonly Instruction Ble_Un_S = new Instruction(Ble_Un_S);
		public static readonly Instruction Blt_Un_S = new Instruction(Blt_Un_S);
		public static readonly Instruction Br = new Instruction(Br);
		public static readonly Instruction Brfalse = new Instruction(Brfalse);
		public static readonly Instruction Brtrue = new Instruction(Brtrue);
		public static readonly Instruction Beq = new Instruction(Beq);
		public static readonly Instruction Bge = new Instruction(Bge);
		public static readonly Instruction Bgt = new Instruction(Bgt);
		public static readonly Instruction Ble = new Instruction(Ble);
		public static readonly Instruction Blt = new Instruction(Blt);
		public static readonly Instruction Bne_Un = new Instruction(Bne_Un);
		public static readonly Instruction Bge_Un = new Instruction(Bge_Un);
		public static readonly Instruction Bgt_Un = new Instruction(Bgt_Un);
		public static readonly Instruction Ble_Un = new Instruction(Ble_Un);
		public static readonly Instruction Blt_Un = new Instruction(Blt_Un);
		public static readonly Instruction Switch = new Instruction(Switch);
		public static readonly Instruction Ldind_I1 = new Instruction(Ldind_I1);
		public static readonly Instruction Ldind_U1 = new Instruction(Ldind_U1);
		public static readonly Instruction Ldind_I2 = new Instruction(Ldind_I2);
		public static readonly Instruction Ldind_U2 = new Instruction(Ldind_U2);
		public static readonly Instruction Ldind_I4 = new Instruction(Ldind_I4);
		public static readonly Instruction Ldind_U4 = new Instruction(Ldind_U4);
		public static readonly Instruction Ldind_I8 = new Instruction(Ldind_I8);
		public static readonly Instruction Ldind_I = new Instruction(Ldind_I);
		public static readonly Instruction Ldind_R4 = new Instruction(Ldind_R4);
		public static readonly Instruction Ldind_R8 = new Instruction(Ldind_R8);
		public static readonly Instruction Ldind_Ref = new Instruction(Ldind_Ref);
		public static readonly Instruction Stind_Ref = new Instruction(Stind_Ref);
		public static readonly Instruction Stind_I1 = new Instruction(Stind_I1);
		public static readonly Instruction Stind_I2 = new Instruction(Stind_I2);
		public static readonly Instruction Stind_I4 = new Instruction(Stind_I4);
		public static readonly Instruction Stind_I8 = new Instruction(Stind_I8);
		public static readonly Instruction Stind_R4 = new Instruction(Stind_R4);
		public static readonly Instruction Stind_R8 = new Instruction(Stind_R8);
		public static readonly Instruction Add = new Instruction(Add);
		public static readonly Instruction Sub = new Instruction(Sub);
		public static readonly Instruction Mul = new Instruction(Mul);
		public static readonly Instruction Div = new Instruction(Div);
		public static readonly Instruction Div_Un = new Instruction(Div_Un);
		public static readonly Instruction Rem = new Instruction(Rem);
		public static readonly Instruction Rem_Un = new Instruction(Rem_Un);
		public static readonly Instruction And = new Instruction(And);
		public static readonly Instruction Or = new Instruction(Or);
		public static readonly Instruction Xor = new Instruction(Xor);
		public static readonly Instruction Shl = new Instruction(Shl);
		public static readonly Instruction Shr = new Instruction(Shr);
		public static readonly Instruction Shr_Un = new Instruction(Shr_Un);
		public static readonly Instruction Neg = new Instruction(Neg);
		public static readonly Instruction Not = new Instruction(Not);
		public static readonly Instruction Conv_I1 = new Instruction(Conv_I1);
		public static readonly Instruction Conv_I2 = new Instruction(Conv_I2);
		public static readonly Instruction Conv_I4 = new Instruction(Conv_I4);
		public static readonly Instruction Conv_I8 = new Instruction(Conv_I8);
		public static readonly Instruction Conv_R4 = new Instruction(Conv_R4);
		public static readonly Instruction Conv_R8 = new Instruction(Conv_R8);
		public static readonly Instruction Conv_U4 = new Instruction(Conv_U4);
		public static readonly Instruction Conv_U8 = new Instruction(Conv_U8);
		public static readonly Instruction Callvirt = new Instruction(Callvirt);
		public static readonly Instruction Cpobj = new Instruction(Cpobj);
		public static readonly Instruction Ldobj = new Instruction(Ldobj);
		public static readonly Instruction Ldstr = new Instruction(Ldstr);
		public static readonly Instruction Newobj = new Instruction(Newobj);
		public static readonly Instruction Castclass = new Instruction(Castclass);
		public static readonly Instruction Isinst = new Instruction(Isinst);
		public static readonly Instruction Conv_R_Un = new Instruction(Conv_R_Un);
		public static readonly Instruction Unbox = new Instruction(Unbox);
		public static readonly Instruction Throw = new Instruction(Throw);
		public static readonly Instruction Ldfld = new Instruction(Ldfld);
		public static readonly Instruction Ldflda = new Instruction(Ldflda);
		public static readonly Instruction Stfld = new Instruction(Stfld);
		public static readonly Instruction Ldsfld = new Instruction(Ldsfld);
		public static readonly Instruction Ldsflda = new Instruction(Ldsflda);
		public static readonly Instruction Stsfld = new Instruction(Stsfld);
		public static readonly Instruction Stobj = new Instruction(Stobj);
		public static readonly Instruction Conv_Ovf_I1_Un = new Instruction(Conv_Ovf_I1_Un);
		public static readonly Instruction Conv_Ovf_I2_Un = new Instruction(Conv_Ovf_I2_Un);
		public static readonly Instruction Conv_Ovf_I4_Un = new Instruction(Conv_Ovf_I4_Un);
		public static readonly Instruction Conv_Ovf_I8_Un = new Instruction(Conv_Ovf_I8_Un);
		public static readonly Instruction Conv_Ovf_U1_Un = new Instruction(Conv_Ovf_U1_Un);
		public static readonly Instruction Conv_Ovf_U2_Un = new Instruction(Conv_Ovf_U2_Un);
		public static readonly Instruction Conv_Ovf_U4_Un = new Instruction(Conv_Ovf_U4_Un);
		public static readonly Instruction Conv_Ovf_U8_Un = new Instruction(Conv_Ovf_U8_Un);
		public static readonly Instruction Conv_Ovf_I_Un = new Instruction(Conv_Ovf_I_Un);
		public static readonly Instruction Conv_Ovf_U_Un = new Instruction(Conv_Ovf_U_Un);
		public static readonly Instruction Box = new Instruction(Box);
		public static readonly Instruction Newarr = new Instruction(Newarr);
		public static readonly Instruction Ldlen = new Instruction(Ldlen);
		public static readonly Instruction Ldelema = new Instruction(Ldelema);
		public static readonly Instruction Ldelem_I1 = new Instruction(Ldelem_I1);
		public static readonly Instruction Ldelem_U1 = new Instruction(Ldelem_U1);
		public static readonly Instruction Ldelem_I2 = new Instruction(Ldelem_I2);
		public static readonly Instruction Ldelem_U2 = new Instruction(Ldelem_U2);
		public static readonly Instruction Ldelem_I4 = new Instruction(Ldelem_I4);
		public static readonly Instruction Ldelem_U4 = new Instruction(Ldelem_U4);
		public static readonly Instruction Ldelem_I8 = new Instruction(Ldelem_I8);
		public static readonly Instruction Ldelem_I = new Instruction(Ldelem_I);
		public static readonly Instruction Ldelem_R4 = new Instruction(Ldelem_R4);
		public static readonly Instruction Ldelem_R8 = new Instruction(Ldelem_R8);
		public static readonly Instruction Ldelem_Ref = new Instruction(Ldelem_Ref);
		public static readonly Instruction Stelem_I = new Instruction(Stelem_I);
		public static readonly Instruction Stelem_I1 = new Instruction(Stelem_I1);
		public static readonly Instruction Stelem_I2 = new Instruction(Stelem_I2);
		public static readonly Instruction Stelem_I4 = new Instruction(Stelem_I4);
		public static readonly Instruction Stelem_I8 = new Instruction(Stelem_I8);
		public static readonly Instruction Stelem_R4 = new Instruction(Stelem_R4);
		public static readonly Instruction Stelem_R8 = new Instruction(Stelem_R8);
		public static readonly Instruction Stelem_Ref = new Instruction(Stelem_Ref);
		public static readonly Instruction Ldelem = new Instruction(Ldelem);
		public static readonly Instruction Stelem = new Instruction(Stelem);
		public static readonly Instruction Unbox_Any = new Instruction(Unbox_Any);
		public static readonly Instruction Conv_Ovf_I1 = new Instruction(Conv_Ovf_I1);
		public static readonly Instruction Conv_Ovf_U1 = new Instruction(Conv_Ovf_U1);
		public static readonly Instruction Conv_Ovf_I2 = new Instruction(Conv_Ovf_I2);
		public static readonly Instruction Conv_Ovf_U2 = new Instruction(Conv_Ovf_U2);
		public static readonly Instruction Conv_Ovf_I4 = new Instruction(Conv_Ovf_I4);
		public static readonly Instruction Conv_Ovf_U4 = new Instruction(Conv_Ovf_U4);
		public static readonly Instruction Conv_Ovf_I8 = new Instruction(Conv_Ovf_I8);
		public static readonly Instruction Conv_Ovf_U8 = new Instruction(Conv_Ovf_U8);
		public static readonly Instruction Refanyval = new Instruction(Refanyval);
		public static readonly Instruction Ckfinite = new Instruction(Ckfinite);
		public static readonly Instruction Mkrefany = new Instruction(Mkrefany);
		public static readonly Instruction Ldtoken = new Instruction(Ldtoken);
		public static readonly Instruction Conv_U2 = new Instruction(Conv_U2);
		public static readonly Instruction Conv_U1 = new Instruction(Conv_U1);
		public static readonly Instruction Conv_I = new Instruction(Conv_I);
		public static readonly Instruction Conv_Ovf_I = new Instruction(Conv_Ovf_I);
		public static readonly Instruction Conv_Ovf_U = new Instruction(Conv_Ovf_U);
		public static readonly Instruction Add_Ovf = new Instruction(Add_Ovf);
		public static readonly Instruction Add_Ovf_Un = new Instruction(Add_Ovf_Un);
		public static readonly Instruction Mul_Ovf = new Instruction(Mul_Ovf);
		public static readonly Instruction Mul_Ovf_Un = new Instruction(Mul_Ovf_Un);
		public static readonly Instruction Sub_Ovf = new Instruction(Sub_Ovf);
		public static readonly Instruction Sub_Ovf_Un = new Instruction(Sub_Ovf_Un);
		public static readonly Instruction Endfinally = new Instruction(Endfinally);
		public static readonly Instruction Leave = new Instruction(Leave);
		public static readonly Instruction Leave_S = new Instruction(Leave_S);
		public static readonly Instruction Stind_I = new Instruction(Stind_I);
		public static readonly Instruction Conv_U = new Instruction(Conv_U);
		public static readonly Instruction Prefix7 = new Instruction(Prefix7);
		public static readonly Instruction Prefix6 = new Instruction(Prefix6);
		public static readonly Instruction Prefix5 = new Instruction(Prefix5);
		public static readonly Instruction Prefix4 = new Instruction(Prefix4);
		public static readonly Instruction Prefix3 = new Instruction(Prefix3);
		public static readonly Instruction Prefix2 = new Instruction(Prefix2);
		public static readonly Instruction Prefix1 = new Instruction(Prefix1);
		public static readonly Instruction Prefixref = new Instruction(Prefixref);
		public static readonly Instruction Arglist = new Instruction(Arglist);
		public static readonly Instruction Ceq = new Instruction(Ceq);
		public static readonly Instruction Cgt = new Instruction(Cgt);
		public static readonly Instruction Cgt_Un = new Instruction(Cgt_Un);
		public static readonly Instruction Clt = new Instruction(Clt);
		public static readonly Instruction Clt_Un = new Instruction(Clt_Un);
		public static readonly Instruction Ldftn = new Instruction(Ldftn);
		public static readonly Instruction Ldvirtftn = new Instruction(Ldvirtftn);
		public static readonly Instruction Ldarg = new Instruction(Ldarg);
		public static readonly Instruction Ldarga = new Instruction(Ldarga);
		public static readonly Instruction Starg = new Instruction(Starg);
		public static readonly Instruction Ldloc = new Instruction(Ldloc);
		public static readonly Instruction Ldloca = new Instruction(Ldloca);
		public static readonly Instruction Stloc = new Instruction(Stloc);
		public static readonly Instruction Localloc = new Instruction(Localloc);
		public static readonly Instruction Endfilter = new Instruction(Endfilter);
		public static readonly Instruction Unaligned = new Instruction(Unaligned);
		public static readonly Instruction Volatile = new Instruction(Volatile);
		public static readonly Instruction Tailcall = new Instruction(Tailcall);
		public static readonly Instruction Initobj = new Instruction(Initobj);
		public static readonly Instruction Constrained = new Instruction(Constrained);
		public static readonly Instruction Cpblk = new Instruction(Cpblk);
		public static readonly Instruction Initblk = new Instruction(Initblk);
		public static readonly Instruction Rethrow = new Instruction(Rethrow);
		public static readonly Instruction Sizeof = new Instruction(Sizeof);
		public static readonly Instruction Refanytype = new Instruction(Refanytype);
		public static readonly Instruction Readonly = new Instruction(Readonly);

		public static IEnumerable<Instruction> Yield_Nop(object operand = null) { yield return Nop + operand; }
		public static IEnumerable<Instruction> Yield_Nop(object operand, Label label) { yield return Nop + operand + label; }
		public static IEnumerable<Instruction> Yield_Break(object operand = null) { yield return Break + operand; }
		public static IEnumerable<Instruction> Yield_Break(object operand, Label label) { yield return Break + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldarg_0(object operand = null) { yield return Ldarg_0 + operand; }
		public static IEnumerable<Instruction> Yield_Ldarg_0(object operand, Label label) { yield return Ldarg_0 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldarg_1(object operand = null) { yield return Ldarg_1 + operand; }
		public static IEnumerable<Instruction> Yield_Ldarg_1(object operand, Label label) { yield return Ldarg_1 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldarg_2(object operand = null) { yield return Ldarg_2 + operand; }
		public static IEnumerable<Instruction> Yield_Ldarg_2(object operand, Label label) { yield return Ldarg_2 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldarg_3(object operand = null) { yield return Ldarg_3 + operand; }
		public static IEnumerable<Instruction> Yield_Ldarg_3(object operand, Label label) { yield return Ldarg_3 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldloc_0(object operand = null) { yield return Ldloc_0 + operand; }
		public static IEnumerable<Instruction> Yield_Ldloc_0(object operand, Label label) { yield return Ldloc_0 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldloc_1(object operand = null) { yield return Ldloc_1 + operand; }
		public static IEnumerable<Instruction> Yield_Ldloc_1(object operand, Label label) { yield return Ldloc_1 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldloc_2(object operand = null) { yield return Ldloc_2 + operand; }
		public static IEnumerable<Instruction> Yield_Ldloc_2(object operand, Label label) { yield return Ldloc_2 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldloc_3(object operand = null) { yield return Ldloc_3 + operand; }
		public static IEnumerable<Instruction> Yield_Ldloc_3(object operand, Label label) { yield return Ldloc_3 + operand + label; }
		public static IEnumerable<Instruction> Yield_Stloc_0(object operand = null) { yield return Stloc_0 + operand; }
		public static IEnumerable<Instruction> Yield_Stloc_0(object operand, Label label) { yield return Stloc_0 + operand + label; }
		public static IEnumerable<Instruction> Yield_Stloc_1(object operand = null) { yield return Stloc_1 + operand; }
		public static IEnumerable<Instruction> Yield_Stloc_1(object operand, Label label) { yield return Stloc_1 + operand + label; }
		public static IEnumerable<Instruction> Yield_Stloc_2(object operand = null) { yield return Stloc_2 + operand; }
		public static IEnumerable<Instruction> Yield_Stloc_2(object operand, Label label) { yield return Stloc_2 + operand + label; }
		public static IEnumerable<Instruction> Yield_Stloc_3(object operand = null) { yield return Stloc_3 + operand; }
		public static IEnumerable<Instruction> Yield_Stloc_3(object operand, Label label) { yield return Stloc_3 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldarg_S(object operand = null) { yield return Ldarg_S + operand; }
		public static IEnumerable<Instruction> Yield_Ldarg_S(object operand, Label label) { yield return Ldarg_S + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldarga_S(object operand = null) { yield return Ldarga_S + operand; }
		public static IEnumerable<Instruction> Yield_Ldarga_S(object operand, Label label) { yield return Ldarga_S + operand + label; }
		public static IEnumerable<Instruction> Yield_Starg_S(object operand = null) { yield return Starg_S + operand; }
		public static IEnumerable<Instruction> Yield_Starg_S(object operand, Label label) { yield return Starg_S + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldloc_S(object operand = null) { yield return Ldloc_S + operand; }
		public static IEnumerable<Instruction> Yield_Ldloc_S(object operand, Label label) { yield return Ldloc_S + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldloca_S(object operand = null) { yield return Ldloca_S + operand; }
		public static IEnumerable<Instruction> Yield_Ldloca_S(object operand, Label label) { yield return Ldloca_S + operand + label; }
		public static IEnumerable<Instruction> Yield_Stloc_S(object operand = null) { yield return Stloc_S + operand; }
		public static IEnumerable<Instruction> Yield_Stloc_S(object operand, Label label) { yield return Stloc_S + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldnull(object operand = null) { yield return Ldnull + operand; }
		public static IEnumerable<Instruction> Yield_Ldnull(object operand, Label label) { yield return Ldnull + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_M1(object operand = null) { yield return Ldc_I4_M1 + operand; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_M1(object operand, Label label) { yield return Ldc_I4_M1 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_0(object operand = null) { yield return Ldc_I4_0 + operand; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_0(object operand, Label label) { yield return Ldc_I4_0 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_1(object operand = null) { yield return Ldc_I4_1 + operand; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_1(object operand, Label label) { yield return Ldc_I4_1 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_2(object operand = null) { yield return Ldc_I4_2 + operand; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_2(object operand, Label label) { yield return Ldc_I4_2 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_3(object operand = null) { yield return Ldc_I4_3 + operand; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_3(object operand, Label label) { yield return Ldc_I4_3 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_4(object operand = null) { yield return Ldc_I4_4 + operand; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_4(object operand, Label label) { yield return Ldc_I4_4 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_5(object operand = null) { yield return Ldc_I4_5 + operand; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_5(object operand, Label label) { yield return Ldc_I4_5 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_6(object operand = null) { yield return Ldc_I4_6 + operand; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_6(object operand, Label label) { yield return Ldc_I4_6 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_7(object operand = null) { yield return Ldc_I4_7 + operand; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_7(object operand, Label label) { yield return Ldc_I4_7 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_8(object operand = null) { yield return Ldc_I4_8 + operand; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_8(object operand, Label label) { yield return Ldc_I4_8 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_S(object operand = null) { yield return Ldc_I4_S + operand; }
		public static IEnumerable<Instruction> Yield_Ldc_I4_S(object operand, Label label) { yield return Ldc_I4_S + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldc_I4(object operand = null) { yield return Ldc_I4 + operand; }
		public static IEnumerable<Instruction> Yield_Ldc_I4(object operand, Label label) { yield return Ldc_I4 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldc_I8(object operand = null) { yield return Ldc_I8 + operand; }
		public static IEnumerable<Instruction> Yield_Ldc_I8(object operand, Label label) { yield return Ldc_I8 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldc_R4(object operand = null) { yield return Ldc_R4 + operand; }
		public static IEnumerable<Instruction> Yield_Ldc_R4(object operand, Label label) { yield return Ldc_R4 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldc_R8(object operand = null) { yield return Ldc_R8 + operand; }
		public static IEnumerable<Instruction> Yield_Ldc_R8(object operand, Label label) { yield return Ldc_R8 + operand + label; }
		public static IEnumerable<Instruction> Yield_Dup(object operand = null) { yield return Dup + operand; }
		public static IEnumerable<Instruction> Yield_Dup(object operand, Label label) { yield return Dup + operand + label; }
		public static IEnumerable<Instruction> Yield_Pop(object operand = null) { yield return Pop + operand; }
		public static IEnumerable<Instruction> Yield_Pop(object operand, Label label) { yield return Pop + operand + label; }
		public static IEnumerable<Instruction> Yield_Jmp(object operand = null) { yield return Jmp + operand; }
		public static IEnumerable<Instruction> Yield_Jmp(object operand, Label label) { yield return Jmp + operand + label; }
		public static IEnumerable<Instruction> Yield_Call(object operand = null) { yield return Call + operand; }
		public static IEnumerable<Instruction> Yield_Call(object operand, Label label) { yield return Call + operand + label; }
		public static IEnumerable<Instruction> Yield_Calli(object operand = null) { yield return Calli + operand; }
		public static IEnumerable<Instruction> Yield_Calli(object operand, Label label) { yield return Calli + operand + label; }
		public static IEnumerable<Instruction> Yield_Ret(object operand = null) { yield return Ret + operand; }
		public static IEnumerable<Instruction> Yield_Ret(object operand, Label label) { yield return Ret + operand + label; }
		public static IEnumerable<Instruction> Yield_Br_S(object operand = null) { yield return Br_S + operand; }
		public static IEnumerable<Instruction> Yield_Br_S(object operand, Label label) { yield return Br_S + operand + label; }
		public static IEnumerable<Instruction> Yield_Brfalse_S(object operand = null) { yield return Brfalse_S + operand; }
		public static IEnumerable<Instruction> Yield_Brfalse_S(object operand, Label label) { yield return Brfalse_S + operand + label; }
		public static IEnumerable<Instruction> Yield_Brtrue_S(object operand = null) { yield return Brtrue_S + operand; }
		public static IEnumerable<Instruction> Yield_Brtrue_S(object operand, Label label) { yield return Brtrue_S + operand + label; }
		public static IEnumerable<Instruction> Yield_Beq_S(object operand = null) { yield return Beq_S + operand; }
		public static IEnumerable<Instruction> Yield_Beq_S(object operand, Label label) { yield return Beq_S + operand + label; }
		public static IEnumerable<Instruction> Yield_Bge_S(object operand = null) { yield return Bge_S + operand; }
		public static IEnumerable<Instruction> Yield_Bge_S(object operand, Label label) { yield return Bge_S + operand + label; }
		public static IEnumerable<Instruction> Yield_Bgt_S(object operand = null) { yield return Bgt_S + operand; }
		public static IEnumerable<Instruction> Yield_Bgt_S(object operand, Label label) { yield return Bgt_S + operand + label; }
		public static IEnumerable<Instruction> Yield_Ble_S(object operand = null) { yield return Ble_S + operand; }
		public static IEnumerable<Instruction> Yield_Ble_S(object operand, Label label) { yield return Ble_S + operand + label; }
		public static IEnumerable<Instruction> Yield_Blt_S(object operand = null) { yield return Blt_S + operand; }
		public static IEnumerable<Instruction> Yield_Blt_S(object operand, Label label) { yield return Blt_S + operand + label; }
		public static IEnumerable<Instruction> Yield_Bne_Un_S(object operand = null) { yield return Bne_Un_S + operand; }
		public static IEnumerable<Instruction> Yield_Bne_Un_S(object operand, Label label) { yield return Bne_Un_S + operand + label; }
		public static IEnumerable<Instruction> Yield_Bge_Un_S(object operand = null) { yield return Bge_Un_S + operand; }
		public static IEnumerable<Instruction> Yield_Bge_Un_S(object operand, Label label) { yield return Bge_Un_S + operand + label; }
		public static IEnumerable<Instruction> Yield_Bgt_Un_S(object operand = null) { yield return Bgt_Un_S + operand; }
		public static IEnumerable<Instruction> Yield_Bgt_Un_S(object operand, Label label) { yield return Bgt_Un_S + operand + label; }
		public static IEnumerable<Instruction> Yield_Ble_Un_S(object operand = null) { yield return Ble_Un_S + operand; }
		public static IEnumerable<Instruction> Yield_Ble_Un_S(object operand, Label label) { yield return Ble_Un_S + operand + label; }
		public static IEnumerable<Instruction> Yield_Blt_Un_S(object operand = null) { yield return Blt_Un_S + operand; }
		public static IEnumerable<Instruction> Yield_Blt_Un_S(object operand, Label label) { yield return Blt_Un_S + operand + label; }
		public static IEnumerable<Instruction> Yield_Br(object operand = null) { yield return Br + operand; }
		public static IEnumerable<Instruction> Yield_Br(object operand, Label label) { yield return Br + operand + label; }
		public static IEnumerable<Instruction> Yield_Brfalse(object operand = null) { yield return Brfalse + operand; }
		public static IEnumerable<Instruction> Yield_Brfalse(object operand, Label label) { yield return Brfalse + operand + label; }
		public static IEnumerable<Instruction> Yield_Brtrue(object operand = null) { yield return Brtrue + operand; }
		public static IEnumerable<Instruction> Yield_Brtrue(object operand, Label label) { yield return Brtrue + operand + label; }
		public static IEnumerable<Instruction> Yield_Beq(object operand = null) { yield return Beq + operand; }
		public static IEnumerable<Instruction> Yield_Beq(object operand, Label label) { yield return Beq + operand + label; }
		public static IEnumerable<Instruction> Yield_Bge(object operand = null) { yield return Bge + operand; }
		public static IEnumerable<Instruction> Yield_Bge(object operand, Label label) { yield return Bge + operand + label; }
		public static IEnumerable<Instruction> Yield_Bgt(object operand = null) { yield return Bgt + operand; }
		public static IEnumerable<Instruction> Yield_Bgt(object operand, Label label) { yield return Bgt + operand + label; }
		public static IEnumerable<Instruction> Yield_Ble(object operand = null) { yield return Ble + operand; }
		public static IEnumerable<Instruction> Yield_Ble(object operand, Label label) { yield return Ble + operand + label; }
		public static IEnumerable<Instruction> Yield_Blt(object operand = null) { yield return Blt + operand; }
		public static IEnumerable<Instruction> Yield_Blt(object operand, Label label) { yield return Blt + operand + label; }
		public static IEnumerable<Instruction> Yield_Bne_Un(object operand = null) { yield return Bne_Un + operand; }
		public static IEnumerable<Instruction> Yield_Bne_Un(object operand, Label label) { yield return Bne_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Bge_Un(object operand = null) { yield return Bge_Un + operand; }
		public static IEnumerable<Instruction> Yield_Bge_Un(object operand, Label label) { yield return Bge_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Bgt_Un(object operand = null) { yield return Bgt_Un + operand; }
		public static IEnumerable<Instruction> Yield_Bgt_Un(object operand, Label label) { yield return Bgt_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Ble_Un(object operand = null) { yield return Ble_Un + operand; }
		public static IEnumerable<Instruction> Yield_Ble_Un(object operand, Label label) { yield return Ble_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Blt_Un(object operand = null) { yield return Blt_Un + operand; }
		public static IEnumerable<Instruction> Yield_Blt_Un(object operand, Label label) { yield return Blt_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Switch(object operand = null) { yield return Switch + operand; }
		public static IEnumerable<Instruction> Yield_Switch(object operand, Label label) { yield return Switch + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldind_I1(object operand = null) { yield return Ldind_I1 + operand; }
		public static IEnumerable<Instruction> Yield_Ldind_I1(object operand, Label label) { yield return Ldind_I1 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldind_U1(object operand = null) { yield return Ldind_U1 + operand; }
		public static IEnumerable<Instruction> Yield_Ldind_U1(object operand, Label label) { yield return Ldind_U1 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldind_I2(object operand = null) { yield return Ldind_I2 + operand; }
		public static IEnumerable<Instruction> Yield_Ldind_I2(object operand, Label label) { yield return Ldind_I2 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldind_U2(object operand = null) { yield return Ldind_U2 + operand; }
		public static IEnumerable<Instruction> Yield_Ldind_U2(object operand, Label label) { yield return Ldind_U2 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldind_I4(object operand = null) { yield return Ldind_I4 + operand; }
		public static IEnumerable<Instruction> Yield_Ldind_I4(object operand, Label label) { yield return Ldind_I4 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldind_U4(object operand = null) { yield return Ldind_U4 + operand; }
		public static IEnumerable<Instruction> Yield_Ldind_U4(object operand, Label label) { yield return Ldind_U4 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldind_I8(object operand = null) { yield return Ldind_I8 + operand; }
		public static IEnumerable<Instruction> Yield_Ldind_I8(object operand, Label label) { yield return Ldind_I8 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldind_I(object operand = null) { yield return Ldind_I + operand; }
		public static IEnumerable<Instruction> Yield_Ldind_I(object operand, Label label) { yield return Ldind_I + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldind_R4(object operand = null) { yield return Ldind_R4 + operand; }
		public static IEnumerable<Instruction> Yield_Ldind_R4(object operand, Label label) { yield return Ldind_R4 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldind_R8(object operand = null) { yield return Ldind_R8 + operand; }
		public static IEnumerable<Instruction> Yield_Ldind_R8(object operand, Label label) { yield return Ldind_R8 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldind_Ref(object operand = null) { yield return Ldind_Ref + operand; }
		public static IEnumerable<Instruction> Yield_Ldind_Ref(object operand, Label label) { yield return Ldind_Ref + operand + label; }
		public static IEnumerable<Instruction> Yield_Stind_Ref(object operand = null) { yield return Stind_Ref + operand; }
		public static IEnumerable<Instruction> Yield_Stind_Ref(object operand, Label label) { yield return Stind_Ref + operand + label; }
		public static IEnumerable<Instruction> Yield_Stind_I1(object operand = null) { yield return Stind_I1 + operand; }
		public static IEnumerable<Instruction> Yield_Stind_I1(object operand, Label label) { yield return Stind_I1 + operand + label; }
		public static IEnumerable<Instruction> Yield_Stind_I2(object operand = null) { yield return Stind_I2 + operand; }
		public static IEnumerable<Instruction> Yield_Stind_I2(object operand, Label label) { yield return Stind_I2 + operand + label; }
		public static IEnumerable<Instruction> Yield_Stind_I4(object operand = null) { yield return Stind_I4 + operand; }
		public static IEnumerable<Instruction> Yield_Stind_I4(object operand, Label label) { yield return Stind_I4 + operand + label; }
		public static IEnumerable<Instruction> Yield_Stind_I8(object operand = null) { yield return Stind_I8 + operand; }
		public static IEnumerable<Instruction> Yield_Stind_I8(object operand, Label label) { yield return Stind_I8 + operand + label; }
		public static IEnumerable<Instruction> Yield_Stind_R4(object operand = null) { yield return Stind_R4 + operand; }
		public static IEnumerable<Instruction> Yield_Stind_R4(object operand, Label label) { yield return Stind_R4 + operand + label; }
		public static IEnumerable<Instruction> Yield_Stind_R8(object operand = null) { yield return Stind_R8 + operand; }
		public static IEnumerable<Instruction> Yield_Stind_R8(object operand, Label label) { yield return Stind_R8 + operand + label; }
		public static IEnumerable<Instruction> Yield_Add(object operand = null) { yield return Add + operand; }
		public static IEnumerable<Instruction> Yield_Add(object operand, Label label) { yield return Add + operand + label; }
		public static IEnumerable<Instruction> Yield_Sub(object operand = null) { yield return Sub + operand; }
		public static IEnumerable<Instruction> Yield_Sub(object operand, Label label) { yield return Sub + operand + label; }
		public static IEnumerable<Instruction> Yield_Mul(object operand = null) { yield return Mul + operand; }
		public static IEnumerable<Instruction> Yield_Mul(object operand, Label label) { yield return Mul + operand + label; }
		public static IEnumerable<Instruction> Yield_Div(object operand = null) { yield return Div + operand; }
		public static IEnumerable<Instruction> Yield_Div(object operand, Label label) { yield return Div + operand + label; }
		public static IEnumerable<Instruction> Yield_Div_Un(object operand = null) { yield return Div_Un + operand; }
		public static IEnumerable<Instruction> Yield_Div_Un(object operand, Label label) { yield return Div_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Rem(object operand = null) { yield return Rem + operand; }
		public static IEnumerable<Instruction> Yield_Rem(object operand, Label label) { yield return Rem + operand + label; }
		public static IEnumerable<Instruction> Yield_Rem_Un(object operand = null) { yield return Rem_Un + operand; }
		public static IEnumerable<Instruction> Yield_Rem_Un(object operand, Label label) { yield return Rem_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_And(object operand = null) { yield return And + operand; }
		public static IEnumerable<Instruction> Yield_And(object operand, Label label) { yield return And + operand + label; }
		public static IEnumerable<Instruction> Yield_Or(object operand = null) { yield return Or + operand; }
		public static IEnumerable<Instruction> Yield_Or(object operand, Label label) { yield return Or + operand + label; }
		public static IEnumerable<Instruction> Yield_Xor(object operand = null) { yield return Xor + operand; }
		public static IEnumerable<Instruction> Yield_Xor(object operand, Label label) { yield return Xor + operand + label; }
		public static IEnumerable<Instruction> Yield_Shl(object operand = null) { yield return Shl + operand; }
		public static IEnumerable<Instruction> Yield_Shl(object operand, Label label) { yield return Shl + operand + label; }
		public static IEnumerable<Instruction> Yield_Shr(object operand = null) { yield return Shr + operand; }
		public static IEnumerable<Instruction> Yield_Shr(object operand, Label label) { yield return Shr + operand + label; }
		public static IEnumerable<Instruction> Yield_Shr_Un(object operand = null) { yield return Shr_Un + operand; }
		public static IEnumerable<Instruction> Yield_Shr_Un(object operand, Label label) { yield return Shr_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Neg(object operand = null) { yield return Neg + operand; }
		public static IEnumerable<Instruction> Yield_Neg(object operand, Label label) { yield return Neg + operand + label; }
		public static IEnumerable<Instruction> Yield_Not(object operand = null) { yield return Not + operand; }
		public static IEnumerable<Instruction> Yield_Not(object operand, Label label) { yield return Not + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_I1(object operand = null) { yield return Conv_I1 + operand; }
		public static IEnumerable<Instruction> Yield_Conv_I1(object operand, Label label) { yield return Conv_I1 + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_I2(object operand = null) { yield return Conv_I2 + operand; }
		public static IEnumerable<Instruction> Yield_Conv_I2(object operand, Label label) { yield return Conv_I2 + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_I4(object operand = null) { yield return Conv_I4 + operand; }
		public static IEnumerable<Instruction> Yield_Conv_I4(object operand, Label label) { yield return Conv_I4 + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_I8(object operand = null) { yield return Conv_I8 + operand; }
		public static IEnumerable<Instruction> Yield_Conv_I8(object operand, Label label) { yield return Conv_I8 + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_R4(object operand = null) { yield return Conv_R4 + operand; }
		public static IEnumerable<Instruction> Yield_Conv_R4(object operand, Label label) { yield return Conv_R4 + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_R8(object operand = null) { yield return Conv_R8 + operand; }
		public static IEnumerable<Instruction> Yield_Conv_R8(object operand, Label label) { yield return Conv_R8 + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_U4(object operand = null) { yield return Conv_U4 + operand; }
		public static IEnumerable<Instruction> Yield_Conv_U4(object operand, Label label) { yield return Conv_U4 + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_U8(object operand = null) { yield return Conv_U8 + operand; }
		public static IEnumerable<Instruction> Yield_Conv_U8(object operand, Label label) { yield return Conv_U8 + operand + label; }
		public static IEnumerable<Instruction> Yield_Callvirt(object operand = null) { yield return Callvirt + operand; }
		public static IEnumerable<Instruction> Yield_Callvirt(object operand, Label label) { yield return Callvirt + operand + label; }
		public static IEnumerable<Instruction> Yield_Cpobj(object operand = null) { yield return Cpobj + operand; }
		public static IEnumerable<Instruction> Yield_Cpobj(object operand, Label label) { yield return Cpobj + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldobj(object operand = null) { yield return Ldobj + operand; }
		public static IEnumerable<Instruction> Yield_Ldobj(object operand, Label label) { yield return Ldobj + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldstr(object operand = null) { yield return Ldstr + operand; }
		public static IEnumerable<Instruction> Yield_Ldstr(object operand, Label label) { yield return Ldstr + operand + label; }
		public static IEnumerable<Instruction> Yield_Newobj(object operand = null) { yield return Newobj + operand; }
		public static IEnumerable<Instruction> Yield_Newobj(object operand, Label label) { yield return Newobj + operand + label; }
		public static IEnumerable<Instruction> Yield_Castclass(object operand = null) { yield return Castclass + operand; }
		public static IEnumerable<Instruction> Yield_Castclass(object operand, Label label) { yield return Castclass + operand + label; }
		public static IEnumerable<Instruction> Yield_Isinst(object operand = null) { yield return Isinst + operand; }
		public static IEnumerable<Instruction> Yield_Isinst(object operand, Label label) { yield return Isinst + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_R_Un(object operand = null) { yield return Conv_R_Un + operand; }
		public static IEnumerable<Instruction> Yield_Conv_R_Un(object operand, Label label) { yield return Conv_R_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Unbox(object operand = null) { yield return Unbox + operand; }
		public static IEnumerable<Instruction> Yield_Unbox(object operand, Label label) { yield return Unbox + operand + label; }
		public static IEnumerable<Instruction> Yield_Throw(object operand = null) { yield return Throw + operand; }
		public static IEnumerable<Instruction> Yield_Throw(object operand, Label label) { yield return Throw + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldfld(object operand = null) { yield return Ldfld + operand; }
		public static IEnumerable<Instruction> Yield_Ldfld(object operand, Label label) { yield return Ldfld + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldflda(object operand = null) { yield return Ldflda + operand; }
		public static IEnumerable<Instruction> Yield_Ldflda(object operand, Label label) { yield return Ldflda + operand + label; }
		public static IEnumerable<Instruction> Yield_Stfld(object operand = null) { yield return Stfld + operand; }
		public static IEnumerable<Instruction> Yield_Stfld(object operand, Label label) { yield return Stfld + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldsfld(object operand = null) { yield return Ldsfld + operand; }
		public static IEnumerable<Instruction> Yield_Ldsfld(object operand, Label label) { yield return Ldsfld + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldsflda(object operand = null) { yield return Ldsflda + operand; }
		public static IEnumerable<Instruction> Yield_Ldsflda(object operand, Label label) { yield return Ldsflda + operand + label; }
		public static IEnumerable<Instruction> Yield_Stsfld(object operand = null) { yield return Stsfld + operand; }
		public static IEnumerable<Instruction> Yield_Stsfld(object operand, Label label) { yield return Stsfld + operand + label; }
		public static IEnumerable<Instruction> Yield_Stobj(object operand = null) { yield return Stobj + operand; }
		public static IEnumerable<Instruction> Yield_Stobj(object operand, Label label) { yield return Stobj + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_I1_Un(object operand = null) { yield return Conv_Ovf_I1_Un + operand; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_I1_Un(object operand, Label label) { yield return Conv_Ovf_I1_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_I2_Un(object operand = null) { yield return Conv_Ovf_I2_Un + operand; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_I2_Un(object operand, Label label) { yield return Conv_Ovf_I2_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_I4_Un(object operand = null) { yield return Conv_Ovf_I4_Un + operand; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_I4_Un(object operand, Label label) { yield return Conv_Ovf_I4_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_I8_Un(object operand = null) { yield return Conv_Ovf_I8_Un + operand; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_I8_Un(object operand, Label label) { yield return Conv_Ovf_I8_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_U1_Un(object operand = null) { yield return Conv_Ovf_U1_Un + operand; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_U1_Un(object operand, Label label) { yield return Conv_Ovf_U1_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_U2_Un(object operand = null) { yield return Conv_Ovf_U2_Un + operand; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_U2_Un(object operand, Label label) { yield return Conv_Ovf_U2_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_U4_Un(object operand = null) { yield return Conv_Ovf_U4_Un + operand; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_U4_Un(object operand, Label label) { yield return Conv_Ovf_U4_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_U8_Un(object operand = null) { yield return Conv_Ovf_U8_Un + operand; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_U8_Un(object operand, Label label) { yield return Conv_Ovf_U8_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_I_Un(object operand = null) { yield return Conv_Ovf_I_Un + operand; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_I_Un(object operand, Label label) { yield return Conv_Ovf_I_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_U_Un(object operand = null) { yield return Conv_Ovf_U_Un + operand; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_U_Un(object operand, Label label) { yield return Conv_Ovf_U_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Box(object operand = null) { yield return Box + operand; }
		public static IEnumerable<Instruction> Yield_Box(object operand, Label label) { yield return Box + operand + label; }
		public static IEnumerable<Instruction> Yield_Newarr(object operand = null) { yield return Newarr + operand; }
		public static IEnumerable<Instruction> Yield_Newarr(object operand, Label label) { yield return Newarr + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldlen(object operand = null) { yield return Ldlen + operand; }
		public static IEnumerable<Instruction> Yield_Ldlen(object operand, Label label) { yield return Ldlen + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldelema(object operand = null) { yield return Ldelema + operand; }
		public static IEnumerable<Instruction> Yield_Ldelema(object operand, Label label) { yield return Ldelema + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldelem_I1(object operand = null) { yield return Ldelem_I1 + operand; }
		public static IEnumerable<Instruction> Yield_Ldelem_I1(object operand, Label label) { yield return Ldelem_I1 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldelem_U1(object operand = null) { yield return Ldelem_U1 + operand; }
		public static IEnumerable<Instruction> Yield_Ldelem_U1(object operand, Label label) { yield return Ldelem_U1 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldelem_I2(object operand = null) { yield return Ldelem_I2 + operand; }
		public static IEnumerable<Instruction> Yield_Ldelem_I2(object operand, Label label) { yield return Ldelem_I2 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldelem_U2(object operand = null) { yield return Ldelem_U2 + operand; }
		public static IEnumerable<Instruction> Yield_Ldelem_U2(object operand, Label label) { yield return Ldelem_U2 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldelem_I4(object operand = null) { yield return Ldelem_I4 + operand; }
		public static IEnumerable<Instruction> Yield_Ldelem_I4(object operand, Label label) { yield return Ldelem_I4 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldelem_U4(object operand = null) { yield return Ldelem_U4 + operand; }
		public static IEnumerable<Instruction> Yield_Ldelem_U4(object operand, Label label) { yield return Ldelem_U4 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldelem_I8(object operand = null) { yield return Ldelem_I8 + operand; }
		public static IEnumerable<Instruction> Yield_Ldelem_I8(object operand, Label label) { yield return Ldelem_I8 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldelem_I(object operand = null) { yield return Ldelem_I + operand; }
		public static IEnumerable<Instruction> Yield_Ldelem_I(object operand, Label label) { yield return Ldelem_I + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldelem_R4(object operand = null) { yield return Ldelem_R4 + operand; }
		public static IEnumerable<Instruction> Yield_Ldelem_R4(object operand, Label label) { yield return Ldelem_R4 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldelem_R8(object operand = null) { yield return Ldelem_R8 + operand; }
		public static IEnumerable<Instruction> Yield_Ldelem_R8(object operand, Label label) { yield return Ldelem_R8 + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldelem_Ref(object operand = null) { yield return Ldelem_Ref + operand; }
		public static IEnumerable<Instruction> Yield_Ldelem_Ref(object operand, Label label) { yield return Ldelem_Ref + operand + label; }
		public static IEnumerable<Instruction> Yield_Stelem_I(object operand = null) { yield return Stelem_I + operand; }
		public static IEnumerable<Instruction> Yield_Stelem_I(object operand, Label label) { yield return Stelem_I + operand + label; }
		public static IEnumerable<Instruction> Yield_Stelem_I1(object operand = null) { yield return Stelem_I1 + operand; }
		public static IEnumerable<Instruction> Yield_Stelem_I1(object operand, Label label) { yield return Stelem_I1 + operand + label; }
		public static IEnumerable<Instruction> Yield_Stelem_I2(object operand = null) { yield return Stelem_I2 + operand; }
		public static IEnumerable<Instruction> Yield_Stelem_I2(object operand, Label label) { yield return Stelem_I2 + operand + label; }
		public static IEnumerable<Instruction> Yield_Stelem_I4(object operand = null) { yield return Stelem_I4 + operand; }
		public static IEnumerable<Instruction> Yield_Stelem_I4(object operand, Label label) { yield return Stelem_I4 + operand + label; }
		public static IEnumerable<Instruction> Yield_Stelem_I8(object operand = null) { yield return Stelem_I8 + operand; }
		public static IEnumerable<Instruction> Yield_Stelem_I8(object operand, Label label) { yield return Stelem_I8 + operand + label; }
		public static IEnumerable<Instruction> Yield_Stelem_R4(object operand = null) { yield return Stelem_R4 + operand; }
		public static IEnumerable<Instruction> Yield_Stelem_R4(object operand, Label label) { yield return Stelem_R4 + operand + label; }
		public static IEnumerable<Instruction> Yield_Stelem_R8(object operand = null) { yield return Stelem_R8 + operand; }
		public static IEnumerable<Instruction> Yield_Stelem_R8(object operand, Label label) { yield return Stelem_R8 + operand + label; }
		public static IEnumerable<Instruction> Yield_Stelem_Ref(object operand = null) { yield return Stelem_Ref + operand; }
		public static IEnumerable<Instruction> Yield_Stelem_Ref(object operand, Label label) { yield return Stelem_Ref + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldelem(object operand = null) { yield return Ldelem + operand; }
		public static IEnumerable<Instruction> Yield_Ldelem(object operand, Label label) { yield return Ldelem + operand + label; }
		public static IEnumerable<Instruction> Yield_Stelem(object operand = null) { yield return Stelem + operand; }
		public static IEnumerable<Instruction> Yield_Stelem(object operand, Label label) { yield return Stelem + operand + label; }
		public static IEnumerable<Instruction> Yield_Unbox_Any(object operand = null) { yield return Unbox_Any + operand; }
		public static IEnumerable<Instruction> Yield_Unbox_Any(object operand, Label label) { yield return Unbox_Any + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_I1(object operand = null) { yield return Conv_Ovf_I1 + operand; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_I1(object operand, Label label) { yield return Conv_Ovf_I1 + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_U1(object operand = null) { yield return Conv_Ovf_U1 + operand; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_U1(object operand, Label label) { yield return Conv_Ovf_U1 + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_I2(object operand = null) { yield return Conv_Ovf_I2 + operand; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_I2(object operand, Label label) { yield return Conv_Ovf_I2 + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_U2(object operand = null) { yield return Conv_Ovf_U2 + operand; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_U2(object operand, Label label) { yield return Conv_Ovf_U2 + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_I4(object operand = null) { yield return Conv_Ovf_I4 + operand; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_I4(object operand, Label label) { yield return Conv_Ovf_I4 + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_U4(object operand = null) { yield return Conv_Ovf_U4 + operand; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_U4(object operand, Label label) { yield return Conv_Ovf_U4 + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_I8(object operand = null) { yield return Conv_Ovf_I8 + operand; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_I8(object operand, Label label) { yield return Conv_Ovf_I8 + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_U8(object operand = null) { yield return Conv_Ovf_U8 + operand; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_U8(object operand, Label label) { yield return Conv_Ovf_U8 + operand + label; }
		public static IEnumerable<Instruction> Yield_Refanyval(object operand = null) { yield return Refanyval + operand; }
		public static IEnumerable<Instruction> Yield_Refanyval(object operand, Label label) { yield return Refanyval + operand + label; }
		public static IEnumerable<Instruction> Yield_Ckfinite(object operand = null) { yield return Ckfinite + operand; }
		public static IEnumerable<Instruction> Yield_Ckfinite(object operand, Label label) { yield return Ckfinite + operand + label; }
		public static IEnumerable<Instruction> Yield_Mkrefany(object operand = null) { yield return Mkrefany + operand; }
		public static IEnumerable<Instruction> Yield_Mkrefany(object operand, Label label) { yield return Mkrefany + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldtoken(object operand = null) { yield return Ldtoken + operand; }
		public static IEnumerable<Instruction> Yield_Ldtoken(object operand, Label label) { yield return Ldtoken + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_U2(object operand = null) { yield return Conv_U2 + operand; }
		public static IEnumerable<Instruction> Yield_Conv_U2(object operand, Label label) { yield return Conv_U2 + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_U1(object operand = null) { yield return Conv_U1 + operand; }
		public static IEnumerable<Instruction> Yield_Conv_U1(object operand, Label label) { yield return Conv_U1 + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_I(object operand = null) { yield return Conv_I + operand; }
		public static IEnumerable<Instruction> Yield_Conv_I(object operand, Label label) { yield return Conv_I + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_I(object operand = null) { yield return Conv_Ovf_I + operand; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_I(object operand, Label label) { yield return Conv_Ovf_I + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_U(object operand = null) { yield return Conv_Ovf_U + operand; }
		public static IEnumerable<Instruction> Yield_Conv_Ovf_U(object operand, Label label) { yield return Conv_Ovf_U + operand + label; }
		public static IEnumerable<Instruction> Yield_Add_Ovf(object operand = null) { yield return Add_Ovf + operand; }
		public static IEnumerable<Instruction> Yield_Add_Ovf(object operand, Label label) { yield return Add_Ovf + operand + label; }
		public static IEnumerable<Instruction> Yield_Add_Ovf_Un(object operand = null) { yield return Add_Ovf_Un + operand; }
		public static IEnumerable<Instruction> Yield_Add_Ovf_Un(object operand, Label label) { yield return Add_Ovf_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Mul_Ovf(object operand = null) { yield return Mul_Ovf + operand; }
		public static IEnumerable<Instruction> Yield_Mul_Ovf(object operand, Label label) { yield return Mul_Ovf + operand + label; }
		public static IEnumerable<Instruction> Yield_Mul_Ovf_Un(object operand = null) { yield return Mul_Ovf_Un + operand; }
		public static IEnumerable<Instruction> Yield_Mul_Ovf_Un(object operand, Label label) { yield return Mul_Ovf_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Sub_Ovf(object operand = null) { yield return Sub_Ovf + operand; }
		public static IEnumerable<Instruction> Yield_Sub_Ovf(object operand, Label label) { yield return Sub_Ovf + operand + label; }
		public static IEnumerable<Instruction> Yield_Sub_Ovf_Un(object operand = null) { yield return Sub_Ovf_Un + operand; }
		public static IEnumerable<Instruction> Yield_Sub_Ovf_Un(object operand, Label label) { yield return Sub_Ovf_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Endfinally(object operand = null) { yield return Endfinally + operand; }
		public static IEnumerable<Instruction> Yield_Endfinally(object operand, Label label) { yield return Endfinally + operand + label; }
		public static IEnumerable<Instruction> Yield_Leave(object operand = null) { yield return Leave + operand; }
		public static IEnumerable<Instruction> Yield_Leave(object operand, Label label) { yield return Leave + operand + label; }
		public static IEnumerable<Instruction> Yield_Leave_S(object operand = null) { yield return Leave_S + operand; }
		public static IEnumerable<Instruction> Yield_Leave_S(object operand, Label label) { yield return Leave_S + operand + label; }
		public static IEnumerable<Instruction> Yield_Stind_I(object operand = null) { yield return Stind_I + operand; }
		public static IEnumerable<Instruction> Yield_Stind_I(object operand, Label label) { yield return Stind_I + operand + label; }
		public static IEnumerable<Instruction> Yield_Conv_U(object operand = null) { yield return Conv_U + operand; }
		public static IEnumerable<Instruction> Yield_Conv_U(object operand, Label label) { yield return Conv_U + operand + label; }
		public static IEnumerable<Instruction> Yield_Prefix7(object operand = null) { yield return Prefix7 + operand; }
		public static IEnumerable<Instruction> Yield_Prefix7(object operand, Label label) { yield return Prefix7 + operand + label; }
		public static IEnumerable<Instruction> Yield_Prefix6(object operand = null) { yield return Prefix6 + operand; }
		public static IEnumerable<Instruction> Yield_Prefix6(object operand, Label label) { yield return Prefix6 + operand + label; }
		public static IEnumerable<Instruction> Yield_Prefix5(object operand = null) { yield return Prefix5 + operand; }
		public static IEnumerable<Instruction> Yield_Prefix5(object operand, Label label) { yield return Prefix5 + operand + label; }
		public static IEnumerable<Instruction> Yield_Prefix4(object operand = null) { yield return Prefix4 + operand; }
		public static IEnumerable<Instruction> Yield_Prefix4(object operand, Label label) { yield return Prefix4 + operand + label; }
		public static IEnumerable<Instruction> Yield_Prefix3(object operand = null) { yield return Prefix3 + operand; }
		public static IEnumerable<Instruction> Yield_Prefix3(object operand, Label label) { yield return Prefix3 + operand + label; }
		public static IEnumerable<Instruction> Yield_Prefix2(object operand = null) { yield return Prefix2 + operand; }
		public static IEnumerable<Instruction> Yield_Prefix2(object operand, Label label) { yield return Prefix2 + operand + label; }
		public static IEnumerable<Instruction> Yield_Prefix1(object operand = null) { yield return Prefix1 + operand; }
		public static IEnumerable<Instruction> Yield_Prefix1(object operand, Label label) { yield return Prefix1 + operand + label; }
		public static IEnumerable<Instruction> Yield_Prefixref(object operand = null) { yield return Prefixref + operand; }
		public static IEnumerable<Instruction> Yield_Prefixref(object operand, Label label) { yield return Prefixref + operand + label; }
		public static IEnumerable<Instruction> Yield_Arglist(object operand = null) { yield return Arglist + operand; }
		public static IEnumerable<Instruction> Yield_Arglist(object operand, Label label) { yield return Arglist + operand + label; }
		public static IEnumerable<Instruction> Yield_Ceq(object operand = null) { yield return Ceq + operand; }
		public static IEnumerable<Instruction> Yield_Ceq(object operand, Label label) { yield return Ceq + operand + label; }
		public static IEnumerable<Instruction> Yield_Cgt(object operand = null) { yield return Cgt + operand; }
		public static IEnumerable<Instruction> Yield_Cgt(object operand, Label label) { yield return Cgt + operand + label; }
		public static IEnumerable<Instruction> Yield_Cgt_Un(object operand = null) { yield return Cgt_Un + operand; }
		public static IEnumerable<Instruction> Yield_Cgt_Un(object operand, Label label) { yield return Cgt_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Clt(object operand = null) { yield return Clt + operand; }
		public static IEnumerable<Instruction> Yield_Clt(object operand, Label label) { yield return Clt + operand + label; }
		public static IEnumerable<Instruction> Yield_Clt_Un(object operand = null) { yield return Clt_Un + operand; }
		public static IEnumerable<Instruction> Yield_Clt_Un(object operand, Label label) { yield return Clt_Un + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldftn(object operand = null) { yield return Ldftn + operand; }
		public static IEnumerable<Instruction> Yield_Ldftn(object operand, Label label) { yield return Ldftn + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldvirtftn(object operand = null) { yield return Ldvirtftn + operand; }
		public static IEnumerable<Instruction> Yield_Ldvirtftn(object operand, Label label) { yield return Ldvirtftn + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldarg(object operand = null) { yield return Ldarg + operand; }
		public static IEnumerable<Instruction> Yield_Ldarg(object operand, Label label) { yield return Ldarg + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldarga(object operand = null) { yield return Ldarga + operand; }
		public static IEnumerable<Instruction> Yield_Ldarga(object operand, Label label) { yield return Ldarga + operand + label; }
		public static IEnumerable<Instruction> Yield_Starg(object operand = null) { yield return Starg + operand; }
		public static IEnumerable<Instruction> Yield_Starg(object operand, Label label) { yield return Starg + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldloc(object operand = null) { yield return Ldloc + operand; }
		public static IEnumerable<Instruction> Yield_Ldloc(object operand, Label label) { yield return Ldloc + operand + label; }
		public static IEnumerable<Instruction> Yield_Ldloca(object operand = null) { yield return Ldloca + operand; }
		public static IEnumerable<Instruction> Yield_Ldloca(object operand, Label label) { yield return Ldloca + operand + label; }
		public static IEnumerable<Instruction> Yield_Stloc(object operand = null) { yield return Stloc + operand; }
		public static IEnumerable<Instruction> Yield_Stloc(object operand, Label label) { yield return Stloc + operand + label; }
		public static IEnumerable<Instruction> Yield_Localloc(object operand = null) { yield return Localloc + operand; }
		public static IEnumerable<Instruction> Yield_Localloc(object operand, Label label) { yield return Localloc + operand + label; }
		public static IEnumerable<Instruction> Yield_Endfilter(object operand = null) { yield return Endfilter + operand; }
		public static IEnumerable<Instruction> Yield_Endfilter(object operand, Label label) { yield return Endfilter + operand + label; }
		public static IEnumerable<Instruction> Yield_Unaligned(object operand = null) { yield return Unaligned + operand; }
		public static IEnumerable<Instruction> Yield_Unaligned(object operand, Label label) { yield return Unaligned + operand + label; }
		public static IEnumerable<Instruction> Yield_Volatile(object operand = null) { yield return Volatile + operand; }
		public static IEnumerable<Instruction> Yield_Volatile(object operand, Label label) { yield return Volatile + operand + label; }
		public static IEnumerable<Instruction> Yield_Tailcall(object operand = null) { yield return Tailcall + operand; }
		public static IEnumerable<Instruction> Yield_Tailcall(object operand, Label label) { yield return Tailcall + operand + label; }
		public static IEnumerable<Instruction> Yield_Initobj(object operand = null) { yield return Initobj + operand; }
		public static IEnumerable<Instruction> Yield_Initobj(object operand, Label label) { yield return Initobj + operand + label; }
		public static IEnumerable<Instruction> Yield_Constrained(object operand = null) { yield return Constrained + operand; }
		public static IEnumerable<Instruction> Yield_Constrained(object operand, Label label) { yield return Constrained + operand + label; }
		public static IEnumerable<Instruction> Yield_Cpblk(object operand = null) { yield return Cpblk + operand; }
		public static IEnumerable<Instruction> Yield_Cpblk(object operand, Label label) { yield return Cpblk + operand + label; }
		public static IEnumerable<Instruction> Yield_Initblk(object operand = null) { yield return Initblk + operand; }
		public static IEnumerable<Instruction> Yield_Initblk(object operand, Label label) { yield return Initblk + operand + label; }
		public static IEnumerable<Instruction> Yield_Rethrow(object operand = null) { yield return Rethrow + operand; }
		public static IEnumerable<Instruction> Yield_Rethrow(object operand, Label label) { yield return Rethrow + operand + label; }
		public static IEnumerable<Instruction> Yield_Sizeof(object operand = null) { yield return Sizeof + operand; }
		public static IEnumerable<Instruction> Yield_Sizeof(object operand, Label label) { yield return Sizeof + operand + label; }
		public static IEnumerable<Instruction> Yield_Refanytype(object operand = null) { yield return Refanytype + operand; }
		public static IEnumerable<Instruction> Yield_Refanytype(object operand, Label label) { yield return Refanytype + operand + label; }
		public static IEnumerable<Instruction> Yield_Readonly(object operand = null) { yield return Readonly + operand; }
		public static IEnumerable<Instruction> Yield_Readonly(object operand, Label label) { yield return Readonly + operand + label; }
	}
}