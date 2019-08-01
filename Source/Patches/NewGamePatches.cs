using Harmony;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimBattle
{
	// intercept the next button on world tile select page and store
	// the resulting cells (if all are ok)
	//
	[HarmonyPatch(typeof(Page_SelectStartingSite))]
	[HarmonyPatch("CanDoNext")]
	class Page_SelectStartingSite_CanDoNext_Patch
	{
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
	class WorldLayer_MouseTile_Tile_Patch
	{
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
	class Prefs_MaxNumberOfPlayerSettlements_Patch
	{
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
	class ScenPart_PlayerPawnsArriveMethod_GenerateIntoMap_Patch
	{
		static bool Prefix()
		{
			return Find.GameInitData.startingAndOptionalPawns.Any();
		}
	}

	// init gamecontroller early
	//
	[HarmonyPatch(typeof(Page_CreateWorldParams))]
	[HarmonyPatch(nameof(Page_CreateWorldParams.PreOpen))]
	class Page_CreateWorldParams_PreOpen_Patch1
	{
		static void Prefix()
		{
			Ref.controller = Current.Game.GetComponent<GameController>();
		}
	}

	// init use 5% world coverage
	//
	[HarmonyPatch(typeof(Page_CreateWorldParams))]
	[HarmonyPatch(nameof(Page_CreateWorldParams.PreOpen))]
	class Page_CreateWorldParams_PreOpen_Patch2
	{
		static void Postfix(ref float ___planetCoverage)
		{
			___planetCoverage = 0.05f;
		}
	}

	// add our start configuration interface
	//
	[HarmonyPatch(typeof(Page_CreateWorldParams))]
	[HarmonyPatch(nameof(Page_CreateWorldParams.DoWindowContents))]
	class Page_CreateWorldParams_DoWindowContents_Patch
	{
		static void Postfix(Rect rect)
		{
			ConfigGUI.DoWindowContents(rect);
		}
	}

	// skip pawn selection screen
	//
	[HarmonyPatch(typeof(ScenPart_ConfigPage))]
	[HarmonyPatch(nameof(ScenPart_ConfigPage.GetConfigPages))]
	class ScenPart_ConfigPage_GetConfigPages_Patch
	{
		static IEnumerable<Page> Postfix(IEnumerable<Page> pages)
		{
			return pages.Where(page => page.GetType() != typeof(Page_ConfigureStartingPawns));
		}
	}

	// use our multi map init
	//
	[HarmonyPatch(typeof(Game))]
	[HarmonyPatch(nameof(Game.InitNewGame))]
	class Game_InitNewGame_Patch
	{
		static bool Prefix()
		{
			GameState.InitNewGame();
			GameState.StartMultiplayer();
			return false;
		}
	}
}