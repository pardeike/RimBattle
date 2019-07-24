using Harmony;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimBattle
{
	// prepare teams before map starts filling with things
	/*
	[HarmonyPatch(typeof(Map))]
	[HarmonyPatch(nameof(Map.ConstructComponents))]
	static class Map_ConstructComponents_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(Map __instance)
		{
			if (Tools.HasTeam == false)
				return;
			if (Scribe.mode != LoadSaveMode.LoadingVars)
				return;

			Team.CreateWithColonists(__instance);
			Ref.controller.CreateMapPart(__instance);
		}
	}*/

	// intercept the next button on world tile select page and store
	// the resulting cells (if all are ok)
	//
	[HarmonyPatch(typeof(Page_SelectStartingSite))]
	[HarmonyPatch("CanDoNext")]
	static class Page_SelectStartingSite_CanDoNext_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(ref bool __result)
		{
			if (__result)
			{
				var center = Find.WorldInterface.SelectedTile;
				var tiles = Tools.CalculateTiles(center);
				if (Tools.CheckTiles(tiles))
				{
					Ref.controller.tiles = tiles.ToList();
					__result = true;
					return;
				}
			}
			__result = false;
		}
	}

	// remove normal world mouse tile in favour for our own
	//
	[HarmonyPatch(typeof(WorldLayer_MouseTile))]
	[HarmonyPatch("Tile", MethodType.Getter)]
	static class WorldLayer_MouseTile_Tile_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(WorldLayer_MouseTile __instance, ref int __result)
		{
			if (__instance.GetType().Assembly == typeof(WorldLayer_MouseTile_Tile_Patch).Assembly)
				return true;

			__result = -1;
			return false;
		}
	}

	// allow for 7 settlements
	//
	[HarmonyPatch(typeof(Prefs))]
	[HarmonyPatch(nameof(Prefs.MaxNumberOfPlayerSettlements), MethodType.Getter)]
	static class Prefs_MaxNumberOfPlayerSettlements_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(ref int __result)
		{
			__result = 7; // maximum
			return false;
		}
	}

	// allow empty settlements
	//
	[HarmonyPatch(typeof(ScenPart_PlayerPawnsArriveMethod))]
	[HarmonyPatch(nameof(ScenPart_PlayerPawnsArriveMethod.GenerateIntoMap))]
	static class ScenPart_PlayerPawnsArriveMethod_GenerateIntoMap_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix()
		{
			return Find.GameInitData.startingAndOptionalPawns.Any();
		}
	}

	// init gamecontroller early
	//
	[HarmonyPatch(typeof(Page_CreateWorldParams))]
	[HarmonyPatch(nameof(Page_CreateWorldParams.PreOpen))]
	static class Page_CreateWorldParams_PreOpen_Patch1
	{
		[HarmonyPriority(10000)]
		static void Prefix()
		{
			Ref.controller = Current.Game.GetComponent<GameController>();
		}
	}

	// init use 5% world coverage
	//
	[HarmonyPatch(typeof(Page_CreateWorldParams))]
	[HarmonyPatch(nameof(Page_CreateWorldParams.PreOpen))]
	static class Page_CreateWorldParams_PreOpen_Patch2
	{
		[HarmonyPriority(10000)]
		static void Postfix(ref float ___planetCoverage)
		{
			___planetCoverage = 0.05f;
		}
	}

	// add our start configuration interface
	//
	[HarmonyPatch(typeof(Page_CreateWorldParams))]
	[HarmonyPatch(nameof(Page_CreateWorldParams.DoWindowContents))]
	static class Page_CreateWorldParams_DoWindowContents_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(Rect rect)
		{
			ConfigGUI.DoWindowContents(rect);
		}
	}

	// skip pawn selection screen
	//
	[HarmonyPatch(typeof(ScenPart_ConfigPage))]
	[HarmonyPatch(nameof(ScenPart_ConfigPage.GetConfigPages))]
	static class ScenPart_ConfigPage_GetConfigPages_Patch
	{
		[HarmonyPriority(10000)]
		static IEnumerable<Page> Postfix(IEnumerable<Page> pages)
		{
			return pages.Where(page => page.GetType() != typeof(Page_ConfigureStartingPawns));
		}
	}

	// use our multi map init
	//
	[HarmonyPatch(typeof(Game))]
	[HarmonyPatch(nameof(Game.InitNewGame))]
	static class Game_InitNewGame_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix()
		{
			Tools.InitNewGame();
			return false;
		}
	}
}