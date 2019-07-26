using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace RimBattle
{
	public class MultiPatchInfo
	{
		public MethodBase original;
		public MethodInfo replaceFrom;
		public MethodInfo replaceTo;
		public CodeInstruction[] additionalInstructions;

		public MultiPatchInfo(MethodBase original, MethodInfo replaceFrom, MethodInfo replaceTo, params CodeInstruction[] additionalInstructions)
		{
			this.original = original;
			this.replaceFrom = replaceFrom;
			this.replaceTo = replaceTo;
			this.additionalInstructions = additionalInstructions;
		}
	}

	public class MultiPatches
	{
		readonly Type patchClass;
		readonly MultiPatchInfo[] patchInfos;

		public MultiPatches(Type patchClass, params MultiPatchInfo[] patchInfos)
		{
			this.patchClass = patchClass;
			this.patchInfos = patchInfos;
		}

		public IEnumerable<MethodBase> TargetMethods()
		{
			var i = 1;
			var originals = new HashSet<MethodBase>();
			foreach (var patchInfo in patchInfos)
			{
				if (patchInfo.original == null)
					Log.Error($"In {patchClass.FullName} original #{i} was not defined");
				if (patchInfo.replaceFrom == null || patchInfo.replaceFrom.IsStatic == false)
					Log.Error($"In {patchClass.FullName} replaceFrom #{i} was not defined");
				if (patchInfo.replaceTo == null || patchInfo.replaceTo.IsStatic == false)
					Log.Error($"In {patchClass.FullName} replaceTo #{i} was not defined");
				if (patchInfo.original != null && originals.Contains(patchInfo.original) == false)
				{
					originals.Add(patchInfo.original);
					yield return patchInfo.original;
				}
				i++;
			}
		}

		public IEnumerable<CodeInstruction> Transpile(MethodBase original, IEnumerable<CodeInstruction> instructions)
		{
			var codes = instructions.ToList();
			var multiPatches = patchInfos.Where(info => info.original == original);
			foreach (var multiPatch in multiPatches)
			{
				if (multiPatch.replaceFrom == multiPatch.replaceTo)
					Log.Error($"Replacement methods are the same in {original.FullDescription()}");

				var fromTypes = multiPatch.replaceFrom.GetParameters().Types();
				var toTypes = multiPatch.replaceTo.GetParameters().Types();
				if (toTypes.Length < fromTypes.Length)
				{
					var info = $"{multiPatch.replaceFrom.FullDescription()}/{multiPatch.replaceTo.FullDescription()}";
					Log.Error($"Replacement methods have mismatching arguments ({info}) in {original.FullDescription()}");
				}
				for (var i = 0; i < fromTypes.Length; i++)
					if (fromTypes[i] != toTypes[i])
						Log.Error($"Replacement methods have mismatching argument #{i + 1} (should be {fromTypes[i]}) in {original.FullDescription()}");
				var found = false;
				for (var i = 0; i < codes.Count; i++)
				{
					var code = codes[i];
					if (code.opcode == OpCodes.Call || code.opcode == OpCodes.Callvirt)
						if (code.operand == multiPatch.replaceFrom)
						{
							codes.InsertRange(i, multiPatch.additionalInstructions);
							i += multiPatch.additionalInstructions.Length;
							code.operand = multiPatch.replaceTo;
							found = true;
						}
				}
				if (found == false)
					Log.Error($"Cannot find instruction CALL {multiPatch.replaceFrom.FullDescription()} in {original.FullDescription()}");
			}
			return codes.AsEnumerable();
		}
	}
}