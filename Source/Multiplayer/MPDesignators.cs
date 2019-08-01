using Harmony;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace RimBattle
{
	using Instructions = IEnumerable<CodeInstruction>;

	// send extra pawn sync object when sending DesignateSingleCell, DesignateMultiCell
	//
	[HarmonyPatch]
	class MapAsyncTimeComp_DesignateSingleCell_Patch
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method("Multiplayer.Client.DesignatorPatches:DesignateSingleCell");
			yield return AccessTools.Method("Multiplayer.Client.DesignatorPatches:DesignateMultiCell");
			yield return AccessTools.Method("Multiplayer.Client.DesignatorPatches:DesignateThing");
		}

		static void SyncWriteCurrentTeam(object byteWriter)
		{
			MPTools.SyncWrite(byteWriter, Ref.controller.team);
		}

		static Instructions Transpiler(Instructions codes)
		{
			foreach (var code in codes)
			{
				yield return code;
				if (code.operand is MethodInfo method && method.Name == "WriteSync")
				{
					yield return new CodeInstruction(OpCodes.Ldloc_2);
					yield return new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => SyncWriteCurrentTeam(null)));
				}
			}
		}
	}

	// read extra pawn sync object when receiving DesignateSingleCell
	//
	[HarmonyPatch]
	class MapAsyncTimeComp_HandleDesignator_Patch
	{
		static readonly MethodInfo m_DesignateSingleCell = AccessTools.Method(typeof(Designator), "DesignateSingleCell");
		static readonly MethodInfo m_MyDesignateSingleCell = SymbolExtensions.GetMethodInfo(() => MyDesignateSingleCell(null, default, null));
		static void MyDesignateSingleCell(Designator designator, IntVec3 c, object data)
		{
			if (designator is Designator_Build buildDesignator)
			{
				var team = MPTools.SyncRead<int>(data);
				CopiedMethods.DesignateSingleCell_WithTeam(buildDesignator, c, team);
				return;
			}
			designator.DesignateSingleCell(c);
		}

		static readonly MethodInfo m_DesignateMultiCell = AccessTools.Method(typeof(Designator), "DesignateMultiCell");
		static readonly MethodInfo m_MyDesignateMultiCell = SymbolExtensions.GetMethodInfo(() => MyDesignateMultiCell(null, default, null));
		static void MyDesignateMultiCell(Designator designator, IEnumerable<IntVec3> cells, object data)
		{
			if (designator is Designator_Build buildDesignator)
			{
				var team = MPTools.SyncRead<int>(data);
				CopiedMethods.DesignateMultiCell_WithTeam(buildDesignator, cells, team);
				return;
			}
			designator.DesignateMultiCell(cells);
		}

		static readonly MethodInfo m_DesignateThing = AccessTools.Method(typeof(Designator), "DesignateThing");
		static readonly MethodInfo m_MyDesignateThing = SymbolExtensions.GetMethodInfo(() => MyDesignateThing(null, default, null));
		static void MyDesignateThing(Designator designator, Thing thing, object data)
		{
			var team = MPTools.SyncRead<int>(data);
			designator.DesignateThing(thing);

			if (designator is Designator_Claim || designator is Designator_SmoothSurface)
				if (thing is ThingWithComps compThing)
					CompOwnedBy.SetTeam(compThing, team);
		}

		static readonly MethodInfo m_HandleDesignator = AccessTools.Method("Multiplayer.Client.MapAsyncTimeComp:HandleDesignator");
		static readonly CodeInstruction ldarg2 = new CodeInstruction(OpCodes.Ldarg_2);
		static readonly MultiPatches multiPatches = new MultiPatches(
			typeof(OwnedByTeam_MultiPatches),
			new MultiPatchInfo(m_HandleDesignator, m_DesignateSingleCell, m_MyDesignateSingleCell, ldarg2),
			new MultiPatchInfo(m_HandleDesignator, m_DesignateMultiCell, m_MyDesignateMultiCell, ldarg2),
			new MultiPatchInfo(m_HandleDesignator, m_DesignateThing, m_MyDesignateThing, ldarg2)
		);

		static IEnumerable<MethodBase> TargetMethods()
		{
			return multiPatches.TargetMethods();
		}

		static Instructions Transpiler(MethodBase original, Instructions codes)
		{
			return multiPatches.Transpile(original, codes);
		}
	}
}