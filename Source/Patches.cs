using Harmony;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimBattle
{
	using Instructions = IEnumerable<CodeInstruction>;

	// use 5% world coverage
	//
	[HarmonyPatch(typeof(Page_CreateWorldParams))]
	[HarmonyPatch(nameof(Page_CreateWorldParams.PreOpen))]
	static class Page_CreateWorldParams_PreOpen_Patch
	{
		[HarmonyPriority(10000)]
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
		[HarmonyPriority(10000)]
		static void Postfix(World __instance)
		{
			Refs.controller = __instance.GetComponent<GameController>();
		}
	}

	// getting our own OnGUI
	//
	[HarmonyPatch(typeof(UIRoot))]
	[HarmonyPatch(nameof(UIRoot.UIRootOnGUI))]
	static class UIRoot_UIRootOnGUI_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix()
		{
			Refs.controller?.OnGUI();
		}
	}

	// hide colonist bar if battle map is showing
	//
	[HarmonyPatch(typeof(ColonistBar))]
	[HarmonyPatch(nameof(ColonistBar.ColonistBarOnGUI))]
	static class ColonistBar_ColonistBarOnGUI_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix()
		{
			return (Refs.controller?.battleOverview?.showing ?? true) == false;
		}
	}

	// show only our teams colonists
	//
	[HarmonyPatch(typeof(ColonistBar))]
	[HarmonyPatch("CheckRecacheEntries")]
	static class ColonistBar_CheckRecacheEntries_Patch
	{
		public static IEnumerable<Pawn> OurFreeColonists(MapPawns mapPawns)
		{
			return mapPawns.FreeColonists
				.Where(c => c.Map == Find.CurrentMap);
		}

		public static List<Pawn> OurAllPawnsSpawned(MapPawns mapPawns)
		{
			return mapPawns.AllPawnsSpawned
				.Where(c => c.Map == Find.CurrentMap)
				.ToList();
		}

		[HarmonyPriority(10000)]
		static Instructions Transpiler(Instructions instructions)
		{
			var m_get_FreeColonists = AccessTools.Property(typeof(MapPawns), "FreeColonists").GetGetMethod();
			var m_OurFreeColonists = SymbolExtensions.GetMethodInfo(() => OurFreeColonists(null));
			var m_get_AllPawnsSpawned = AccessTools.Property(typeof(MapPawns), "AllPawnsSpawned").GetGetMethod();
			var m_OurAllPawnsSpawned = SymbolExtensions.GetMethodInfo(() => OurAllPawnsSpawned(null));
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt)
				{
					if (instruction.operand == m_get_FreeColonists)
					{
						instruction.opcode = OpCodes.Call;
						instruction.operand = m_OurFreeColonists;
					}
					if (instruction.operand == m_get_AllPawnsSpawned)
					{
						instruction.opcode = OpCodes.Call;
						instruction.operand = m_OurFreeColonists;
					}
				}
				yield return instruction;
			}
		}
	}

	[HarmonyPatch(typeof(MapInterface))]
	[HarmonyPatch(nameof(MapInterface.Notify_SwitchedMap))]
	static class MapInterface_Notify_SwitchedMap_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix()
		{
			Find.ColonistBar.MarkColonistsDirty();
		}
	}

	// hide pawn labels if battle map is showing
	//
	[HarmonyPatch(typeof(GenMapUI))]
	[HarmonyPatch("DrawPawnLabel")]
	[HarmonyPatch(new[] { typeof(Pawn), typeof(Rect), typeof(float), typeof(float), typeof(Dictionary<string, string>), typeof(GameFont), typeof(bool), typeof(bool) })]
	static class GenMapUI_DrawPawnLabel_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix()
		{
			return Refs.controller.BattleOverview.showing == false;
		}
	}

	// adding vanilla keybindings
	//
	[HarmonyPatch(typeof(DefGenerator))]
	[HarmonyPatch(nameof(DefGenerator.GenerateImpliedDefs_PostResolve))]
	static class DefGenerator_GenerateImpliedDefs_PostResolve_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix()
		{
			AccessTools.GetDeclaredFields(typeof(Keys))
				.Do(field =>
				{
					var keyDef = field.GetValue(null) as KeyBindingDef;
					DefDatabase<KeyBindingDef>.Add(keyDef);
				});
		}
	}

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
		[HarmonyPriority(10000)]
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

	//
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

	// remove World tab
	//
	[HarmonyPatch(typeof(MainButtonsRoot))]
	[HarmonyPatch(MethodType.Constructor)]
	static class MainButtonsRoot_Constructor_Patch
	{
		[HarmonyPriority(10000)]
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

		[HarmonyPriority(10000)]
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
		[HarmonyPriority(10000)]
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

		[HarmonyPriority(10000)]
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
		[HarmonyPriority(10000)]
		static void Prefix(Thing __instance, IntVec3 value)
		{
			var pawn = __instance as Pawn;
			if (pawn == null) return;
			var map = pawn.Map;
			if (map == null) return;
			if (pawn.Position == value) return;
			if (pawn.Faction != Faction.OfPlayer) return;

			// TODO: if this is too slow, make a co-routine and queue work to it
			var controller = Refs.controller;
			if (controller.IsMyColonist(pawn))
				if (controller.mapParts.TryGetValue(map, out var mapPart))
					map.DoInCircle(value, pawn.WeaponRange(), mapPart.visibility.MakeVisible);
		}
	}

	// show other colonists only if they are close by
	//
	[HarmonyPatch(typeof(PawnRenderer))]
	[HarmonyPatch("RenderPawnInternal")]
	[HarmonyPatch(new[] { typeof(Vector3), typeof(float), typeof(bool), typeof(Rot4), typeof(Rot4), typeof(RotDrawMode), typeof(bool), typeof(bool) })]
	static class PawnRenderer_RenderPawnInternal_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(Vector3 rootLoc, Pawn ___pawn, out bool __state)
		{
			__state = true;
			if (___pawn.Faction != Faction.OfPlayer) return true;
			var map = ___pawn.Map;
			if (map == null) return true;
			__state = Refs.controller.IsInWeaponRange(___pawn);
			return __state;
		}

		[HarmonyPriority(10000)]
		static void Postfix(Pawn ___pawn, bool __state)
		{
			if (__state == false)
				return;

			var team = Refs.controller.TeamForPawn(___pawn);
			if (team == null) return;

			var pos = ___pawn.DrawPos + new Vector3(0.3f, 0.2f, -0.3f);
			var matrix = default(Matrix4x4);
			matrix.SetTRS(pos, Quaternion.identity, new Vector3(0.5f, 1f, 0.5f));
			Graphics.DrawMesh(MeshPool.plane10, matrix, Refs.BadgeShadow, 0);
			Graphics.DrawMesh(MeshPool.plane10, matrix, Refs.Badges[team.id], 0);
		}
	}

	// skip to draw progressbar if not in visible range
	//
	[HarmonyPatch(typeof(ToilEffects))]
	[HarmonyPatch(nameof(ToilEffects.WithProgressBar))]
	static class ToilEffects_WithProgressBar_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(Toil toil)
		{
			var last2 = toil.preTickActions.Count - 1;
			var action2 = toil.preTickActions[last2];
			toil.preTickActions[last2] = delegate
			{
				if (toil.actor.Faction != Faction.OfPlayer || Refs.controller.IsInWeaponRange(toil.actor))
					action2();
			};
		}
	}

	// skip to draw effecter if not in visible range
	//
	[HarmonyPatch(typeof(ToilEffects))]
	[HarmonyPatch(nameof(ToilEffects.WithEffect))]
	[HarmonyPatch(new[] { typeof(Toil), typeof(Func<EffecterDef>), typeof(Func<LocalTargetInfo>) })]
	static class ToilEffects_WithEffect_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(Toil toil)
		{
			var i = toil.preTickActions.Count - 1;
			var action = toil.preTickActions[i];
			toil.preTickActions[i] = delegate
			{
				if (toil.actor.Faction != Faction.OfPlayer || Refs.controller.IsInWeaponRange(toil.actor))
					action();
			};
		}
	}

	// draw pawn shadows only if close by
	// 
	[HarmonyPatch(typeof(Graphic))]
	[HarmonyPatch("Draw")]
	static class Graphic_Draw_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(Thing thing)
		{
			var pawn = thing as Pawn;
			if (pawn == null) return true;
			if (pawn.Faction != Faction.OfPlayer) return true;
			var map = pawn.Map;
			if (map == null) return true;
			return Refs.controller.IsInWeaponRange(pawn);
		}
	}

	// only visible and in range objects are selectable
	//
	[HarmonyPatch(typeof(Selector))]
	[HarmonyPatch(nameof(Selector.Select))]
	static class Selector_Select_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(object obj, bool forceDesignatorDeselect)
		{
			// Designator_ZoneAdd.set_SelectedZone does some funky stuff 
			if (forceDesignatorDeselect == false) return true;

			return Tools.CanSelect(obj);
		}
	}

	// disallow designation in cells that are not visible
	//
	[HarmonyPatch]
	static class Designator_Cell_Patch
	{
		static bool IsVisible(Map map, IntVec3 loc)
		{
			return Refs.controller.IsVisible(map, loc);
		}

		static IEnumerable<MethodBase> TargetMethods()
		{
			var myAssembly = typeof(Designator_Cell_Patch).Assembly;
			return GenTypes.AllTypes
				.Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(Designator)))
				.Where(type => type.Assembly != myAssembly && type != typeof(Designator_EmptySpace))
				.Select(type => type.GetMethod(nameof(Designator.CanDesignateCell)) as MethodBase);
		}

		[HarmonyPriority(10000)]
		static Instructions Transpiler(MethodBase method, Instructions instructions, ILGenerator generator)
		{
			var m_get_Map = AccessTools.Property(typeof(Designator), nameof(Designator.Map)).GetGetMethod();
			var m_IsVisible = SymbolExtensions.GetMethodInfo(() => IsVisible(null, IntVec3.Zero));
			var m_get_WasRejected = AccessTools.Method(typeof(AcceptanceReport), "get_WasRejected");
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Call, m_get_Map);
			yield return new CodeInstruction(OpCodes.Ldarg_1);
			yield return new CodeInstruction(OpCodes.Call, m_IsVisible);
			var label = generator.DefineLabel();
			yield return new CodeInstruction(OpCodes.Brtrue, label);
			yield return new CodeInstruction(OpCodes.Call, m_get_WasRejected);
			yield return new CodeInstruction(OpCodes.Ret);
			yield return new CodeInstruction(OpCodes.Nop) { labels = new List<Label>() { label } };
			foreach (var instruction in instructions)
				yield return instruction;
		}
	}

	// disallow designating things that are not visible
	//
	[HarmonyPatch]
	static class Designator_Thing_Patch
	{
		static bool IsVisible(Thing thing)
		{
			return Refs.controller.IsVisible(thing);
		}

		static IEnumerable<MethodBase> TargetMethods()
		{
			var myAssembly = typeof(Designator_Thing_Patch).Assembly;
			return GenTypes.AllTypes
				.Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(Designator)))
				.Where(type => type.Assembly != myAssembly && type != typeof(Designator_EmptySpace))
				.Select(type => type.GetMethod(nameof(Designator.CanDesignateThing)) as MethodBase);
		}

		[HarmonyPriority(10000)]
		static Instructions Transpiler(MethodBase method, Instructions instructions, ILGenerator generator)
		{
			var m_get_Map = AccessTools.Property(typeof(Designator), nameof(Designator.Map)).GetGetMethod();
			var m_IsVisible = SymbolExtensions.GetMethodInfo(() => IsVisible(null));
			var m_get_WasRejected = AccessTools.Method(typeof(AcceptanceReport), "get_WasRejected");
			yield return new CodeInstruction(OpCodes.Ldarg_1);
			yield return new CodeInstruction(OpCodes.Call, m_IsVisible);
			var label = generator.DefineLabel();
			yield return new CodeInstruction(OpCodes.Brtrue, label);
			yield return new CodeInstruction(OpCodes.Call, m_get_WasRejected);
			yield return new CodeInstruction(OpCodes.Ret);
			yield return new CodeInstruction(OpCodes.Nop) { labels = new List<Label>() { label } };
			foreach (var instruction in instructions)
				yield return instruction;
		}
	}

	// show only our colonists in colonistbar
	//
	[HarmonyPatch(typeof(PlayerPawnsDisplayOrderUtility))]
	[HarmonyPatch(nameof(PlayerPawnsDisplayOrderUtility.Sort))]
	static class PlayerPawnsDisplayOrderUtility_Sort_Patch
	{
		[HarmonyPriority(10000)]
		static void Prefix(List<Pawn> pawns)
		{
			var controller = Refs.controller;
			var myColonists = pawns.Where(pawn => controller.IsMyColonist(pawn)).ToList();
			pawns.Clear();
			pawns.AddRange(myColonists);
		}
	}

	// skip pawn-overlay if not discovered
	//
	[HarmonyPatch(typeof(PawnUIOverlay))]
	[HarmonyPatch(nameof(PawnUIOverlay.DrawPawnGUIOverlay))]
	static class PawnUIOverlay_DrawPawnGUIOverlay_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(Pawn ___pawn)
		{
			if (Refs.controller.BattleOverview.showing) return false;
			var controller = Refs.controller;
			if (controller.IsVisible(___pawn) == false)
				return false;
			return controller.IsInWeaponRange(___pawn);
		}
	}

	// skip thing-overlay if not discovered (1)
	//
	[HarmonyPatch(typeof(Thing))]
	[HarmonyPatch(nameof(Thing.DrawGUIOverlay))]
	static class Thing_DrawGUIOverlay_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(Thing __instance)
		{
			if (Refs.controller.BattleOverview.showing) return false;
			return Refs.controller.IsVisible(__instance);
		}
	}

	// skip thing-overlay if not discovered (2)
	//
	[HarmonyPatch(typeof(ThingOverlays))]
	[HarmonyPatch(nameof(ThingOverlays.ThingOverlaysOnGUI))]
	static class ThingOverlays_ThingOverlaysOnGUI_Patch
	{
		static bool IsFogged(FogGrid grid, IntVec3 c)
		{
			if (Refs.controller.BattleOverview.showing) return false;
			if (Refs.controller.IsVisible(Refs.map(grid), c) == false)
				return false;
			return grid.IsFogged(c);
		}

		[HarmonyPriority(10000)]
		static Instructions Transpiler(Instructions instructions)
		{
			return Transpilers.MethodReplacer(instructions,
				SymbolExtensions.GetMethodInfo(() => new FogGrid(null).IsFogged(IntVec3.Zero)),
				SymbolExtensions.GetMethodInfo(() => IsFogged(null, IntVec3.Zero))
			);
		}
	}

	// skip mote spawns if not discovered
	//
	[HarmonyPatch(typeof(GenSpawn))]
	[HarmonyPatch(nameof(GenSpawn.Spawn))]
	[HarmonyPatch(new[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool) })]
	static class GenSpawn_Spawn_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(Thing newThing, IntVec3 loc, Map map, ref Thing __result)
		{
			if (newThing is Mote && loc.InBounds(map))
				if (Refs.controller.IsVisible(map, loc) == false)
				{
					__result = newThing;
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
		[HarmonyPriority(10000)]
		static Instructions Transpiler(Instructions instructions)
		{
			instructions.GetHashCode(); // make compiler happy
			var replacement = SymbolExtensions.GetMethodInfo(() => CopiedMethods.RegenerateFog(null));
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
		[HarmonyPriority(10000)]
		static Instructions Transpiler(Instructions instructions, ILGenerator generator)
		{
			Func<Thing, bool> IsVisible = (thing) => Refs.controller.IsVisible(thing.Map, thing.Position);

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

	// hide zones if not discovered
	//
	[HarmonyPatch]
	static class SectionLayer_Zones_Regenerate_Patch
	{
		// jeez, why is this class internal
		static MethodBase TargetMethod()
		{
			var type = AccessTools.TypeByName("SectionLayer_Zones");
			return AccessTools.Method(type, "Regenerate");
		}

		static bool Prefix(object __instance)
		{
			var myBase = __instance as SectionLayer;
			if (myBase == null) return true;
			var section = Refs.section(myBase);
			CopiedMethods.RegenerateZone(myBase, section);
			return false;
		}

		[HarmonyPriority(10000)]
		static Instructions Transpiler(Instructions instructions)
		{
			instructions.GetHashCode(); // make compiler happy
			var replacement = SymbolExtensions.GetMethodInfo(() => CopiedMethods.RegenerateFog(null));
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Call, replacement);
			yield return new CodeInstruction(OpCodes.Ret);
		}
	}

	// add form caravan button if colonists or colony animals are selected
	//
	[HarmonyPatch(typeof(InspectGizmoGrid))]
	[HarmonyPatch(nameof(InspectGizmoGrid.DrawInspectGizmoGridFor))]
	static class InspectGizmoGrid_DrawInspectGizmoGridFor_Patch
	{
		static void ClearAndAddOurGizmo(List<Gizmo> list)
		{
			list.Clear();
			Tools.AddFormCaravanGizmo(list);
		}

		[HarmonyPriority(10000)]
		static Instructions Transpiler(Instructions instructions)
		{
			var m_List_Gizmo_Clear = SymbolExtensions.GetMethodInfo(() => new List<Gizmo>().Clear());
			var m_ClearAndAddOurGizmo = SymbolExtensions.GetMethodInfo(() => ClearAndAddOurGizmo(null));
			foreach (var instruction in instructions)
			{
				var first = true;
				if (first)
					if (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt)
						if (instruction.operand == m_List_Gizmo_Clear)
						{
							instruction.opcode = OpCodes.Call;
							instruction.operand = m_ClearAndAddOurGizmo;
							first = false;
						}
				yield return instruction;
			}
		}
	}

	// preselect all selected pawns in the form caravan dialog
	//
	[HarmonyPatch(typeof(Dialog_FormCaravan))]
	[HarmonyPatch(nameof(Dialog_FormCaravan.PostOpen))]
	static class Dialog_FormCaravan_PostOpen_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(List<TransferableOneWay> ___transferables)
		{
			___transferables.Do(transferable =>
			{
				if (transferable.things.Count != 1) return;
				var pawn = transferable.things.First() as Pawn;
				if (pawn == null) return;
				if (Find.Selector.IsSelected(pawn))
					transferable.AdjustTo(transferable.GetMaximumToTransfer());
			});
		}
	}

	// -- maybe used later ----------------------------------------------------------

	/*[HarmonyPatch(typeof(DynamicDrawManager))]
	[HarmonyPatch(nameof(DynamicDrawManager.DrawDynamicThings))]
	static class DynamicDrawManager_DrawDynamicThings_Patch
	{
		[HarmonyPriority(10000)]
		static void Prefix(HashSet<Thing> ___drawThings, out List<Thing> __state)
		{
			__state = ___drawThings.ToList();
			___drawThings.RemoveWhere(thing => Refs.controller.IsVisible(thing) == false);
		}

		[HarmonyPriority(10000)]
		static void Postfix(HashSet<Thing> ___drawThings, List<Thing> __state)
		{
			___drawThings.Clear();
			___drawThings.AddRange(__state);
		}
	}*/

	/*[HarmonyPatch(typeof(Pawn))]
	[HarmonyPatch(nameof(Pawn.GetGizmos))]
	static class Pawn_GetGizmos_Patch
	{
		[HarmonyPriority(10000)]
		static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Pawn __instance)
		{
			foreach (var gizmo in gizmos)
				yield return gizmo;

			if (__instance.IsColonistPlayerControlled)
				yield return new Command_Action
				{
					defaultLabel = "CommandFormCaravan".Translate(),
					defaultDesc = "CommandFormCaravanDesc".Translate(),
					icon = FormCaravanComp.FormCaravanCommand,
					hotKey = KeyBindingDefOf.Misc2,
					tutorTag = "FormCaravan",
					action = delegate ()
					{
						Find.WindowStack.Add(new Dialog_FormCaravan(__instance.Map, false, null, false));
					}
				};
		}
	}*/

	/*[HarmonyPatch(typeof(PawnNameColorUtility))]
	[HarmonyPatch(nameof(PawnNameColorUtility.PawnNameColorOf))]
	static class PawnNameColorUtility_PawnNameColorOf_Patch
	{
		static bool Prefix(Pawn pawn, ref Color __result)
		{
			var team = Refs.controller.TeamForPawn(pawn);
			if (team == null) return true;
			__result = team.color;
			return false;
		}
	}*/

	// we cannot change IsFogged because the fog is local to each player
	// if we do more than cosmetic stuff it will desync
	//
	// fake IsFogged 1
	//
	//[HarmonyPatch(typeof(FogGrid))]
	//[HarmonyPatch(nameof(FogGrid.IsFogged), new[] { typeof(IntVec3) })]
	//static class FogGrid_IsFogged1_Patch
	//{
	//	static bool Prefix(Map ___map, IntVec3 c, ref bool __result)
	//	{
	//		if (c.InBounds(___map) == false)
	//		{
	//			__result = false;
	//			return false;
	//		}

	//		if (Refs.controller.mapParts.TryGetValue(___map, out var mapPart))
	//			if (mapPart.visibility.IsVisible(c) == false)
	//			{
	//				__result = true;
	//				return false;
	//			}
	//		return true;
	//	}
	//}

	// fake IsFogged 2
	//
	//[HarmonyPatch(typeof(FogGrid))]
	//[HarmonyPatch(nameof(FogGrid.IsFogged), new[] { typeof(int) })]
	//static class FogGrid_IsFogged2_Patch
	//{
	//	static bool Prefix(Map ___map, int index, ref bool __result)
	//	{
	//		if (Refs.controller.mapParts.TryGetValue(___map, out var mapPart))
	//			if (mapPart.visibility.IsVisible(index) == false)
	//			{
	//				__result = true;
	//				return false;
	//			}
	//		return true;
	//	}
	//}
}