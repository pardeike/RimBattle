using Harmony;
using RimWorld;
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
}