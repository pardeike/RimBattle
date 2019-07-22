using Harmony;
using RimWorld;
using RimWorld.Planet;
using System;
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
			if (__instance.GetType().Assembly == typeof(WorldLayer_MouseTile).Assembly)
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
	// add team marker to colonists
	//
	[HarmonyPatch(typeof(PawnRenderer))]
	[HarmonyPatch("RenderPawnInternal")]
	[HarmonyPatch(new[] { typeof(Vector3), typeof(float), typeof(bool), typeof(Rot4), typeof(Rot4), typeof(RotDrawMode), typeof(bool), typeof(bool) })]
	static class PawnRenderer_RenderPawnInternal_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(Pawn ___pawn, out bool __state)
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
			if (team == null || team.id == Refs.controller.team) return;

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
		static Instructions Transpiler(Instructions instructions, ILGenerator generator)
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
		static Instructions Transpiler(Instructions instructions, ILGenerator generator)
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
			var section = Refs.SectionLayer_section(myBase);
			CopiedMethods.RegenerateZone(myBase, section);
			return false;
		}

		[HarmonyPriority(10000)]
		static Instructions Transpiler(Instructions instructions)
		{
			_ = instructions; // make compiler happy
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

	// size caravan dialog more reasonable
	//
	[HarmonyPatch(typeof(Dialog_FormCaravan))]
	[HarmonyPatch(nameof(Dialog_FormCaravan.InitialSize), MethodType.Getter)]
	static class Dialog_FormCaravan_InitialSize_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(ref Vector2 __result)
		{
			__result -= new Vector2(28, 100);
		}
	}

	// caravan dialog must choose route
	//
	[HarmonyPatch(typeof(Dialog_FormCaravan))]
	[HarmonyPatch("MustChooseRoute", MethodType.Getter)]
	static class Dialog_FormCaravan_MustChooseRoute_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(ref bool __result)
		{
			__result = true;
			return false;
		}
	}

	// preselect all selected pawns in the form caravan dialog
	//
	[HarmonyPatch(typeof(Dialog_FormCaravan))]
	[HarmonyPatch(nameof(Dialog_FormCaravan.PostOpen))]
	static class Dialog_FormCaravan_PostOpen_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(Dialog_FormCaravan __instance, List<TransferableOneWay> ___transferables)
		{
			___transferables.Do(transferable =>
			{
				if (transferable.things.Count != 1) return;
				var pawn = transferable.things.First() as Pawn;
				if (pawn == null) return;
				if (Find.Selector.IsSelected(pawn))
					transferable.AdjustTo(transferable.GetMaximumToTransfer());
			});

			Refs.canChooseRoute(__instance) = true;
			Refs.startingTile(__instance) = Find.CurrentMap.Tile;

			var controller = Refs.controller;
			var reachableTiles = controller.tiles.Where(tile => controller.CanReach(Find.CurrentMap.Tile, tile));
			Refs.destinationTile(__instance) = reachableTiles.Count() == 1 ? reachableTiles.First() : -1;
		}
	}

	// move items into pawn section of caravan dialog
	//
	[HarmonyPatch(typeof(CaravanUIUtility))]
	[HarmonyPatch(nameof(CaravanUIUtility.CreateCaravanTransferableWidgets))]
	static class CaravanUIUtility_CreateCaravanTransferableWidgets_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(List<TransferableOneWay> transferables, out TransferableOneWayWidget pawnsTransfer, out TransferableOneWayWidget itemsTransfer, string thingCountTip, IgnorePawnsInventoryMode ignorePawnInventoryMass, Func<float> availableMassGetter, bool ignoreSpawnedCorpsesGearAndInventoryMass, int tile, bool playerPawnsReadOnly)
		{
			bool IsColonist(Thing thing) => thing is Pawn && ((Pawn)thing).IsFreeColonist && Refs.controller.IsMyColonist((Pawn)thing);
			bool IsPrisoner(Thing thing) => thing is Pawn && ((Pawn)thing).IsPrisoner;
			bool IsCaptured(Thing thing) => thing is Pawn && ((Pawn)thing).Downed && CaravanUtility.ShouldAutoCapture((Pawn)thing, Faction.OfPlayer);
			bool IsAnimal(Thing thing) => thing is Pawn && ((Pawn)thing).RaceProps.Animal; // TODO: add support for team animals
			bool IsItem(Thing thing) => (thing is Pawn) == false && Refs.controller.IsVisible(thing);

			pawnsTransfer = new TransferableOneWayWidget(transferables, null, null, thingCountTip, true, ignorePawnInventoryMass, false, availableMassGetter, 0f, ignoreSpawnedCorpsesGearAndInventoryMass, tile, true, false, false, true, false, true, playerPawnsReadOnly);
			Refs.TransferableOneWayWidget_sections(pawnsTransfer).Clear();
			pawnsTransfer.AddSection("ColonistsSection".Translate(), transferables.Where(x => IsColonist(x.AnyThing)));
			pawnsTransfer.AddSection("PrisonersSection".Translate(), transferables.Where(x => IsPrisoner(x.AnyThing)));
			pawnsTransfer.AddSection("CaptureSection".Translate(), transferables.Where(x => IsCaptured(x.AnyThing)));
			pawnsTransfer.AddSection("AnimalsSection".Translate(), transferables.Where(x => IsAnimal(x.AnyThing)));
			pawnsTransfer.AddSection("ItemsTab".Translate(), transferables.Where(x => IsItem(x.AnyThing)));

			// empty dummy item list
			itemsTransfer = new TransferableOneWayWidget(default, null, null, thingCountTip, true, ignorePawnInventoryMass, false, availableMassGetter, 0f, ignoreSpawnedCorpsesGearAndInventoryMass, tile, true, false, false, true, false, true, false);

			return false;
		}
	}

	// remove info header and tabs from caravan dialog
	//
	[HarmonyPatch(typeof(Dialog_FormCaravan))]
	[HarmonyPatch(nameof(Dialog_FormCaravan.DoWindowContents))]
	static class Dialog_FormCaravan_DoWindowContents_Patch
	{
		public static float miniMapDialogHeight = 200f;

		static readonly MethodInfo m_DrawMenuSection = SymbolExtensions.GetMethodInfo(() => Widgets.DrawMenuSection(default));
		static readonly MethodInfo m_DrawMenuSection_Empty = SymbolExtensions.GetMethodInfo(() => DrawMenuSection(default));

		static readonly MethodInfo m_DrawCaravanInfo = SymbolExtensions.GetMethodInfo(() => CaravanUIUtility.DrawCaravanInfo(default, default, 0, 0, 0f, default, false, "", false));
		static readonly MethodInfo m_DrawCaravanInfo_Empty = SymbolExtensions.GetMethodInfo(() => DrawCaravanInfo(default, default, 0, 0, 0f, default, false, "", false));

		static readonly MethodInfo m_DrawTabs = SymbolExtensions.GetMethodInfo(() => TabDrawer.DrawTabs(default, null, 0f));
		static readonly MethodInfo m_DrawTabs_Empty = SymbolExtensions.GetMethodInfo(() => DrawTabsSingle(default, null, 0f));

		public static void DrawMenuSection(Rect rect)
		{
			_ = rect;
		}

		static void DrawCaravanInfo(CaravanUIUtility.CaravanInfo p1, CaravanUIUtility.CaravanInfo? p2, int p3, int? p4, float p5, Rect p6, bool p7, string p8, bool p9)
		{
			_ = p1; _ = p2; _ = p3; _ = p4; _ = p5; _ = p6; _ = p7; _ = p8; _ = p9; // make compiler happy
		}

		public static TabRecord DrawTabsSingle(Rect baseRect, List<TabRecord> tabs, float maxTabWidth)
		{
			_ = baseRect;
			_ = maxTabWidth;
			return tabs[0]; // always first tab selected
		}

		[HarmonyPriority(10000)]
		static Instructions Transpiler(Instructions instructions)
		{
			var codes = instructions
				.MethodReplacer(m_DrawMenuSection, m_DrawMenuSection_Empty)
				.MethodReplacer(m_DrawCaravanInfo, m_DrawCaravanInfo_Empty)
				.MethodReplacer(m_DrawTabs, m_DrawTabs_Empty)
				.ToList();
			for (var i = 0; i < codes.Count() - 6; i++)
				if (codes[i].opcode == OpCodes.Ldarga_S || codes[i].opcode == OpCodes.Ldloca_S)
					if (codes[i + 1].opcode == OpCodes.Dup)
						if (codes[i + 2].opcode == OpCodes.Call)
							if (codes[i + 3].opcode == OpCodes.Ldc_R4)
							{
								if ((float)codes[i + 3].operand == 119f)
									codes[i + 3].operand = 40f;
								if ((float)codes[i + 3].operand == 76f)
									codes[i + 3].operand = 86f + miniMapDialogHeight;
							}
			return codes.AsEnumerable();
		}
	}

	// no log food warning in caravan dialog
	//
	[HarmonyPatch(typeof(Dialog_FormCaravan))]
	[HarmonyPatch("DaysWorthOfFood", MethodType.Getter)]
	static class Dialog_FormCaravan_DaysWorthOfFood_Patch
	{
		static bool Prefix(ref Pair<float, float> __result)
		{
			__result = new Pair<float, float>(10f, 0f); // more than 5f is enough
			return false;
		}
	}

	// reordering elements and no choose route button in caravan dialog
	//
	[HarmonyPatch(typeof(Dialog_FormCaravan))]
	[HarmonyPatch("DoBottomButtons")]
	static class Dialog_FormCaravan_DoBottomButtons_Patch
	{
		static readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);

		static Rect dialogRect;
		static int buttonCount;

		[HarmonyPriority(10000)]
		static void Prefix(Dialog_FormCaravan __instance, Rect rect, ref bool ___canChooseRoute)
		{
			Color? IsSelected(Map map)
			{
				if (map.Tile == Refs.destinationTile(__instance))
					return Color.green;
				return null;
			}

			bool CanSelect(Map map)
			{
				return Refs.controller.CanReach(Find.CurrentMap, map);
			}

			void SetSelected(Map map)
			{
				Refs.destinationTile(__instance) = map.Tile;
			}

			var mapHeight = Dialog_FormCaravan_DoWindowContents_Patch.miniMapDialogHeight;

			dialogRect = rect;
			___canChooseRoute = false;
			buttonCount = 0;

			var mapRect = rect;
			mapRect.yMin = rect.height - mapHeight - 86f + 14f;
			mapRect.yMax -= 32f;
			mapRect.width -= BottomButtonSize.x + 16f;
			var config = new MiniMap.Configuration()
			{
				isCurrent = map => Find.CurrentMap == map,
				isSelected = IsSelected,
				canSelect = CanSelect,
				setSelected = SetSelected,
				canSelectMarkers = false
			};
			Refs.controller.BattleOverview.DrawMaps(mapRect, false, config);
		}

		static bool ButtonTextReordered(Rect rect, string label, bool drawBackground, bool doMouseoverSound, bool active)
		{
			if (buttonCount > 2)
				return false;

			_ = rect;
			var offsetOrder = new[] { 0, 2, 1 }[buttonCount]; // accept, reset, cancel
			var offset = BottomButtonSize.y + 16f;
			rect = new Rect(dialogRect.xMax - BottomButtonSize.x, dialogRect.yMax - BottomButtonSize.y - offsetOrder * offset - 32f, BottomButtonSize.x, BottomButtonSize.y);
			buttonCount++;

			return Widgets.ButtonText(rect, label, drawBackground, doMouseoverSound, active);
		}

		static readonly MethodInfo m_ButtonText = SymbolExtensions.GetMethodInfo(() => Widgets.ButtonText(default, "", false, false, false));
		static readonly MethodInfo m_ButtonTextReordered = SymbolExtensions.GetMethodInfo(() => ButtonTextReordered(default, "", false, false, false));

		[HarmonyPriority(10000)]
		static Instructions Transpiler(Instructions instructions)
		{
			var codes = instructions.ToList();
			for (var i = 0; i < codes.Count() - 4; i++)
			{
				if (codes[i].opcode == OpCodes.Call && codes[i].operand == m_ButtonText)
					codes[i].operand = m_ButtonTextReordered;
				/*if (codes[i].opcode == OpCodes.Ldloc_S)
					if (codes[i + 1].opcode == OpCodes.Call || codes[i + 1].opcode == OpCodes.Callvirt)
						if (codes[i + 2].opcode == OpCodes.Ldc_I4_0)
							if (codes[i + 3].opcode == OpCodes.Ble)
								for (var j = 0; j <= 3; j++)
									codes[i + j].opcode = OpCodes.Nop;*/
			}
			return codes.AsEnumerable();
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