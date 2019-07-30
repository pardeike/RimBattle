using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace RimBattle
{
	using Instructions = IEnumerable<CodeInstruction>;

	public class MultiPatchInfo
	{
		public MethodBase original;
		public MethodInfo replaceFrom;
		public MethodInfo replaceTo;
		public Func<Instructions, Instructions> argumentCodes;

		public MultiPatchInfo(MethodBase original, MethodInfo replaceFrom, MethodInfo replaceTo, params CodeInstruction[] codes)
		{
			this.original = original;
			this.replaceFrom = replaceFrom;
			this.replaceTo = replaceTo;
			argumentCodes = (_) => codes.AsEnumerable();
		}

		public MultiPatchInfo(MethodBase original, MethodInfo replaceFrom, MethodInfo replaceTo, Func<Instructions, Instructions> argumentCodes)
		{
			this.original = original;
			this.replaceFrom = replaceFrom;
			this.replaceTo = replaceTo;
			this.argumentCodes = argumentCodes;
		}
	}

	public class MultiPatches
	{
		readonly Type patchClass;
		readonly List<MultiPatchInfo> patchInfos;

		public MultiPatches(Type patchClass, params MultiPatchInfo[] patchInfos)
		{
			this.patchClass = patchClass;
			this.patchInfos = patchInfos.ToList();
		}

		public void Add(MultiPatchInfo patchInfo)
		{
			patchInfos.Add(patchInfo);
		}

		public IEnumerable<MethodBase> TargetMethods()
		{
			var i = 1;
			var originals = new HashSet<MethodBase>();
			foreach (var patchInfo in patchInfos)
			{
				if (patchInfo.original == null)
					Log.Error($"In {patchClass.FullName} original #{i} was not defined");
				if (patchInfo.replaceFrom == null)
					Log.Error($"In {patchClass.FullName} replaceFrom #{i} was not defined");
				if (patchInfo.replaceTo == null)
					Log.Error($"In {patchClass.FullName} replaceTo #{i} was not defined");
				if (patchInfo.original != null && originals.Contains(patchInfo.original) == false)
				{
					_ = originals.Add(patchInfo.original);
					yield return patchInfo.original;
				}
				i++;
			}
		}

		public Instructions Transpile(MethodBase original, Instructions instructions)
		{
			var codes = instructions.ToList();
			var multiPatches = patchInfos.Where(info => info.original == original);
			foreach (var multiPatch in multiPatches)
			{
				if (multiPatch.replaceFrom == multiPatch.replaceTo)
					Log.Error($"Replacement methods are the same in {original.FullDescription()}");
				if (multiPatch.replaceTo.IsStatic == false)
					Log.Error($"Replacement method must be static");

				var fromTypes = multiPatch.replaceFrom.GetParameters().Types().ToList();
				if (multiPatch.replaceFrom.IsStatic == false)
					fromTypes.Insert(0, multiPatch.replaceFrom.DeclaringType);
				var toTypes = multiPatch.replaceTo.GetParameters().Types();
				if (toTypes.Length < fromTypes.Count)
				{
					var info = $"{multiPatch.replaceFrom.FullDescription()}/{multiPatch.replaceTo.FullDescription()}";
					Log.Error($"Replacement methods have mismatching arguments ({info}) in {original.FullDescription()}");
				}
				for (var i = 0; i < fromTypes.Count; i++)
					if (fromTypes[i] != toTypes[i])
						Log.Error($"Replacement methods have mismatching argument #{i + 1} (should be {fromTypes[i]}) in {original.FullDescription()}");
				var found = false;
				for (var i = 0; i < codes.Count; i++)
				{
					var code = codes[i];
					if (code.opcode == OpCodes.Call || code.opcode == OpCodes.Callvirt)
						if (code.operand == multiPatch.replaceFrom)
						{
							var argumentCodes = multiPatch.argumentCodes(instructions).ToList();
							codes.InsertRange(i, argumentCodes);
							i += argumentCodes.Count;
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