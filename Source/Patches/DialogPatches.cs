using Harmony;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimBattle
{
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
}