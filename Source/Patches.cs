using Harmony;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace RimBattle
{
	using CodeInstructions = IEnumerable<CodeInstruction>;

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
					Refs.controller.startingTiles = tiles;
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
			// much faster
			if (stat == StatDefOf.ConstructionSpeed) { __result *= 10f; return; }
			if (stat == StatDefOf.ConstructionSpeedFactor) { __result *= 10f; return; }
			if (stat == StatDefOf.ResearchSpeed) { __result *= 10f; return; }
			if (stat == StatDefOf.ResearchSpeedFactor) { __result *= 10f; return; }
			if (stat == StatDefOf.PlantWorkSpeed) { __result *= 10f; return; }
			if (stat == StatDefOf.SmoothingSpeed) { __result *= 10f; return; }
			if (stat == StatDefOf.UnskilledLaborSpeed) { __result *= 10f; return; }
			if (stat == StatDefOf.AnimalGatherSpeed) { __result *= 10f; return; }
			if (stat == StatDefOf.ImmunityGainSpeed) { __result *= 10f; return; }
			if (stat == StatDefOf.ImmunityGainSpeedFactor) { __result *= 10f; return; }
			if (stat == StatDefOf.WorkTableWorkSpeedFactor) { __result *= 10f; return; }

			// chances
			if (stat == StatDefOf.ConstructSuccessChance) { __result *= 2f; return; }
			if (stat == StatDefOf.TameAnimalChance) { __result *= 2f; return; }

			// delays
			if (stat == StatDefOf.EquipDelay) { __result /= 2f; return; }

			// faster
			if (stat == StatDefOf.EatingSpeed) { __result *= 2f; return; }
			if (stat == StatDefOf.MiningSpeed) { __result *= 2f; return; }
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

	// fake fog
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
	[HarmonyPatch(typeof(SectionLayer_FogOfWar))]
	[HarmonyPatch(nameof(SectionLayer_FogOfWar.Regenerate))]
	static class SectionLayer_FogOfWar_Regenerate__Patch
	{
#pragma warning disable IDE0060
		static CodeInstructions Transpiler(CodeInstructions instructions)
#pragma warning restore IDE0060
		{
			var replacement = SymbolExtensions.GetMethodInfo(() => Tools.Regenerate(null));
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Call, replacement);
			yield return new CodeInstruction(OpCodes.Ret);
		}
	}

	/*[HarmonyPatch(typeof(Frame))]
	[HarmonyPatch(nameof(Frame.CompleteConstruction))]
	static class Frame_CompleteConstruction_Patch
	{
		static Thing Spawn(Thing newThing, IntVec3 loc, Map map, Rot4 rot, WipeMode wipeMode, bool respawningAfterLoad, Pawn creator)
		{
			var result = GenSpawn.Spawn(newThing, loc, map, rot, wipeMode, respawningAfterLoad);
			// TODO: creator build something
			return result;
		}

		static CodeInstructions Transpiler(CodeInstructions instructions)
		{
			var m_Spawn = SymbolExtensions.GetMethodInfo(() => GenSpawn.Spawn(null, IntVec3.Zero, null, Rot4.Invalid, WipeMode.FullRefund, false));
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Call)
					if (instruction.operand == m_Spawn)
					{
						yield return new CodeInstruction(OpCodes.Ldarg_1);
						instruction.operand = SymbolExtensions.GetMethodInfo(() => Spawn(default, default, default, default, default, false, default));
					}

				yield return instruction;
			}
		}
	}*/
}