using Harmony;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace RimBattle
{
	using Instructions = IEnumerable<CodeInstruction>;

	// use 5% world coverage
	//
	[HarmonyPatch(typeof(Page_CreateWorldParams))]
	[HarmonyPatch(nameof(Page_CreateWorldParams.PreOpen))]
	static class Page_CreateWorldParams_PreOpen_Patch
	{
		static void Postfix(ref float ___planetCoverage)
		{
			___planetCoverage = 0.05f;
		}
	}

	// initialize our world component early and save a ref to it
	//
	[HarmonyPatch(typeof(World))]
	[HarmonyPatch(nameof(World.FinalizeInit))]
	static class World_FinalizeInit_Patch
	{
		static void Postfix(World __instance)
		{
			Refs.controller = __instance.GetComponent<GameController>();
		}
	}

	// battle maps rendering
	[HarmonyPatch(typeof(ColonistBar))]
	[HarmonyPatch(nameof(ColonistBar.ColonistBarOnGUI))]
	static class ColonistBar_ColonistBarOnGUI_Patch
	{
		static void Postfix()
		{
			Refs.controller.BattleOverview.OnGUI();
		}
	}

	// intercept the next button on world tile select page and store
	// the 7 result cells (if all are ok)
	//
	[HarmonyPatch(typeof(Page_SelectStartingSite))]
	[HarmonyPatch("CanDoNext")]
	static class Page_SelectStartingSite_CanDoNext_Patch
	{
		static void Postfix(ref bool __result)
		{
			if (__result)
			{
				var center = Find.WorldInterface.SelectedTile;
				var tiles = Tools.CalculateTiles(center);
				if (Tools.CheckTiles(tiles))
				{
					Refs.controller.tiles = tiles.ToList();
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
		static bool Prefix(WorldLayer_MouseTile __instance, ref int __result)
		{
			if (__instance.GetType().Namespace == "RimBattle")
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
		static bool Prefix(ref int __result)
		{
			__result = 7;
			return false;
		}
	}

	// skip pawn selection screen
	//
	[HarmonyPatch(typeof(ScenPart_ConfigPage))]
	[HarmonyPatch(nameof(ScenPart_ConfigPage.GetConfigPages))]
	static class ScenPart_ConfigPage_GetConfigPages_Patch
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
	static class Game_InitNewGame_Patch
	{
		static bool Prefix()
		{
			Tools.InitNewGame();
			return false;
		}
	}

	// remove World tab
	//
	[HarmonyPatch(typeof(MainButtonsRoot))]
	[HarmonyPatch(MethodType.Constructor)]
	static class MainButtonsRoot_Constructor_Patch
	{
		static void Postfix(List<MainButtonDef> ___allButtonsInOrder)
		{
			MainButtonDefOf.World.hotKey = null;
			___allButtonsInOrder.Remove(MainButtonDefOf.World);
		}
	}

	// tweak stats
	//
	[HarmonyPatch]
	static class StatExtension_GetStatValue_Patch
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return SymbolExtensions.GetMethodInfo(() => StatExtension.GetStatValue(null, StatDefOf.Mass, false));
			yield return SymbolExtensions.GetMethodInfo(() => StatExtension.GetStatValueAbstract(null, StatDefOf.Mass, null));
		}

		static void Postfix(ref float __result, StatDef stat)
		{
			Tools.TweakStat(stat, ref __result);
		}
	}

	// allow anyone to build
	//
	[HarmonyPatch(typeof(GenConstruct))]
	[HarmonyPatch(nameof(GenConstruct.CanConstruct))]
	static class GenConstruct_CanConstruct_Patch
	{
		static void Prefix(ref bool checkConstructionSkill)
		{
			checkConstructionSkill = false;
		}
	}

	// allow anyone to craft
	//
	[HarmonyPatch]
	static class RecipeDef_PawnSatisfiesSkillRequirements_Patch
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return SymbolExtensions.GetMethodInfo(() => new RecipeDef().PawnSatisfiesSkillRequirements(null));
			yield return SymbolExtensions.GetMethodInfo(() => new SkillRequirement().PawnSatisfies(null));
		}

		static bool Prefix(ref bool __result)
		{
			__result = true;
			return false;
		}
	}

	// uncover map when moving
	//
	[HarmonyPatch(typeof(Thing))]
	[HarmonyPatch(nameof(Thing.Position), MethodType.Setter)]
	static class Thing_Position_Patch
	{
		static void Prefix(Thing __instance, IntVec3 value)
		{
			var pawn = __instance as Pawn;
			if (pawn == null) return;
			var map = pawn.Map;
			if (map == null) return;
			if (pawn.Position == value) return;
			if (pawn.Faction != Faction.OfPlayer) return;

			if (Refs.controller.mapParts.TryGetValue(map, out var mapPart))
				map.DoInCircle(value, pawn.WeaponRange(), mapPart.visibility.MakeVisible);
		}
	}

	// fake IsFogged 1
	//
	[HarmonyPatch(typeof(FogGrid))]
	[HarmonyPatch(nameof(FogGrid.IsFogged), new[] { typeof(IntVec3) })]
	static class FogGrid_IsFogged1_Patch
	{
		static bool Prefix(Map ___map, IntVec3 c, ref bool __result)
		{
			if (c.InBounds(___map) == false)
			{
				__result = false;
				return false;
			}

			if (Refs.controller.mapParts.TryGetValue(___map, out var mapPart))
				if (mapPart.visibility.IsVisible(c) == false)
				{
					__result = true;
					return false;
				}
			return true;
		}
	}

	// fake IsFogged 2
	//
	[HarmonyPatch(typeof(FogGrid))]
	[HarmonyPatch(nameof(FogGrid.IsFogged), new[] { typeof(int) })]
	static class FogGrid_IsFogged2_Patch
	{
		static bool Prefix(Map ___map, int index, ref bool __result)
		{
			if (Refs.controller.mapParts.TryGetValue(___map, out var mapPart))
				if (mapPart.visibility.IsVisible(index) == false)
				{
					__result = true;
					return false;
				}
			return true;
		}
	}

	// fake fog graphics
	//
	[HarmonyPatch(typeof(SectionLayer_FogOfWar))]
	[HarmonyPatch(nameof(SectionLayer_FogOfWar.Regenerate))]
	static class SectionLayer_FogOfWar_Regenerate__Patch
	{
		static Instructions Transpiler(Instructions instructions)
		{
			instructions.GetHashCode(); // make compiler happy
			var replacement = SymbolExtensions.GetMethodInfo(() => Tools.Regenerate(null));
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Call, replacement);
			yield return new CodeInstruction(OpCodes.Ret);
		}
	}

	// hide overlays if not discovered yet
	//
	[HarmonyPatch(typeof(OverlayDrawer))]
	[HarmonyPatch(nameof(OverlayDrawer.DrawAllOverlays))]
	static class OverlayDrawer_DrawAllOverlays_Patch
	{
		static Instructions Transpiler(Instructions instructions, ILGenerator generator)
		{
			Func<Thing, bool> IsVisible = (thing) =>
			{
				var map = thing.Map;
				if (map == null) return false;
				if (Refs.controller.mapParts == null) return false;
				if (Refs.controller.mapParts.TryGetValue(map, out var part) == false) return false;
				if (part == null) return false;
				return part.visibility.IsVisible(thing.Position);
			};

			foreach (var instruction in instructions)
			{
				yield return instruction;

				if (instruction.opcode == OpCodes.Stloc_2 && IsVisible != null)
				{
					yield return new CodeInstruction(OpCodes.Ldnull);
					yield return new CodeInstruction(OpCodes.Ldloc_2);
					yield return new CodeInstruction(OpCodes.Call, IsVisible.Method);
					var label = generator.DefineLabel();
					yield return new CodeInstruction(OpCodes.Brtrue, label);
					yield return new CodeInstruction(OpCodes.Ret);
					yield return new CodeInstruction(OpCodes.Nop) { labels = new List<Label>() { label } };
					IsVisible = null;
				}
			}
		}
	}
}