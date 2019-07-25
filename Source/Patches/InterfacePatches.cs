using Harmony;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace RimBattle
{
	using Instructions = IEnumerable<CodeInstruction>;

	// getting our own OnGUI
	//
	[HarmonyPatch(typeof(MainTabWindow_Work))]
	[HarmonyPatch("DoManualPrioritiesCheckbox")]
	static class MainTabWindow_Work_DoManualPrioritiesCheckbox_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix()
		{
			Text.Font = GameFont.Small;
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
			var rect = new Rect(5f, 5f, 140f, 30f);
			Widgets.Label(rect, "ManualPriorities".Translate() + ": " + "On".Translate());
			return false;
		}
	}

	// show all my team member from all maps in all work tabs
	//
	[HarmonyPatch(typeof(MainTabWindow_PawnTable))]
	[HarmonyPatch("Pawns", MethodType.Getter)]
	static class MainTabWindow_PawnTable_Pawns_Patch
	{
		[HarmonyPriority(10000)]
		static IEnumerable<Pawn> Postfix(IEnumerable<Pawn> pawns)
		{
			_ = pawns;
			foreach (var pawn in Ref.controller.teams[Ref.controller.team].members)
				yield return pawn;
		}
	}

	// show all my animals from all maps in all work tabs
	//
	[HarmonyPatch(typeof(MainTabWindow_Animals))]
	[HarmonyPatch("Pawns", MethodType.Getter)]
	static class MainTabWindow_Animals_Pawns_Patch
	{
		[HarmonyPriority(10000)]
		static IEnumerable<Pawn> Postfix(IEnumerable<Pawn> pawns)
		{
			_ = pawns;
			return Current.Game.Maps
				.SelectMany(map => map.mapPawns.PawnsInFaction(Faction.OfPlayer))
				.Where(pawn => pawn.RaceProps.Animal)
				.Where(Ref.controller.InMyTeam);
		}
	}

	// skip "(none)" as a choice in the animal menu
	//
	[HarmonyPatch(typeof(TrainableUtility))]
	[HarmonyPatch("MasterSelectButton_GenerateMenu")]
	static class TrainableUtility_MasterSelectButton_GenerateMenu_Patch
	{
		[HarmonyPriority(10000)]
		static IEnumerable<Widgets.DropdownMenuElement<Pawn>> Postfix(IEnumerable<Widgets.DropdownMenuElement<Pawn>> menuItems)
		{
			return menuItems.Skip(1)
				.Where(menuItem => Ref.controller.InMyTeam(menuItem.payload));
		}
	}

	// hide alerts that don't contain at least one team member
	//
	[HarmonyPatch(typeof(Alert))]
	[HarmonyPatch(nameof(Alert.DrawAt))]
	static class Alert_DrawAt_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(Alert __instance)
		{
			if (__instance.Active == false)
				return true;

			var culprits = __instance.GetReport().culprits;
			if (culprits == null)
				return true;

			return culprits
				.Select(culprit => culprit.Thing)
				.OfType<Pawn>()
				.Any(pawn => Ref.controller.InMyTeam(pawn));
		}

		static IEnumerable<GlobalTargetInfo> OnlyMyTeamCulprits(IEnumerable<GlobalTargetInfo> culprits)
		{
			if (culprits == null) yield break;
			foreach (var culprit in culprits)
			{
				if (Ref.controller.InMyTeam(culprit.Thing as Pawn))
					yield return culprit;
			}
		}

		[HarmonyPriority(10000)]
		static Instructions Transpiler(Instructions instructions, ILGenerator generator)
		{
			var codes = instructions.ToList();
			var m_OnlyMyTeamCulprits = SymbolExtensions.GetMethodInfo(() => OnlyMyTeamCulprits(null));
			var f_culprits = AccessTools.Field(typeof(AlertReport), nameof(AlertReport.culprits));

			var idx = codes.FindIndex(code => code.opcode == OpCodes.Ldfld && code.operand == f_culprits);
			if (idx == -1) Log.Error("Cannot find Ldfld AlertReport.culprits in Alert.DrawAt");
			codes.Insert(idx + 1, new CodeInstruction(OpCodes.Call, m_OnlyMyTeamCulprits));
			return codes.AsEnumerable();
		}
	}

	// only highlight our team targets
	//
	[HarmonyPatch(typeof(TargetHighlighter))]
	[HarmonyPatch(nameof(TargetHighlighter.Highlight))]
	static class TargetHighlighter_Highlight_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(GlobalTargetInfo target)
		{
			if (target.IsValid == false)
				return true;

			var pawn = target.Thing as Pawn;
			if (pawn == null)
				return true;

			return Ref.controller.InMyTeam(pawn);
		}
	}
}