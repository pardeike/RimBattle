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
	class Thing_Position_Patch
	{
		[HarmonyPriority(10000)]
		static void Prefix(Thing __instance, IntVec3 value, IntVec3 ___positionInt)
		{
			if (___positionInt == value) return;
			Tools.UpdateVisibility(__instance, value);
		}
	}

	// uncover map when colonist is placed
	//
	[HarmonyPatch(typeof(Thing))]
	[HarmonyPatch(nameof(Thing.SetPositionDirect))]
	class Thing_SetPositionDirect_Patch
	{
		[HarmonyPriority(10000)]
		static void Prefix(Thing __instance, IntVec3 newPos, IntVec3 ___positionInt)
		{
			if (___positionInt == newPos) return;
			Tools.UpdateVisibility(__instance, newPos);
		}
	}

	// uncover map when colonist is placed
	//
	[HarmonyPatch(typeof(Pawn_DraftController))]
	[HarmonyPatch("Notify_PrimaryWeaponChanged")]
	class MapPawns_RegisterPawn_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(Pawn ___pawn)
		{
			Tools.UpdateVisibility(___pawn, ___pawn.Position);
		}
	}

	// show other colonists only if they are close by
	// add team marker to colonists
	//
	[HarmonyPatch(typeof(PawnRenderer))]
	[HarmonyPatch("RenderPawnInternal")]
	[HarmonyPatch(new[] { typeof(Vector3), typeof(float), typeof(bool), typeof(Rot4), typeof(Rot4), typeof(RotDrawMode), typeof(bool), typeof(bool) })]
	class PawnRenderer_RenderPawnInternal_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(Pawn ___pawn, out bool __state)
		{
			__state = true;
			if (___pawn.Faction != Faction.OfPlayer) return true;
			var map = ___pawn.Map;
			if (map == null) return true;
			__state = Ref.controller.IsInVisibleRange(___pawn);
			return __state;
		}

		[HarmonyPriority(10000)]
		static void Postfix(Pawn ___pawn, bool __state)
		{
			if (__state == false)
				return;

			var team = ___pawn.GetTeamID();
			if (team < 0 || Ref.controller.IsMyTeam(team)) return;

			var pos = ___pawn.DrawPos + new Vector3(0.3f, 0.2f, -0.3f);
			var matrix = default(Matrix4x4);
			matrix.SetTRS(pos, Quaternion.identity, new Vector3(0.5f, 1f, 0.5f));
			Graphics.DrawMesh(MeshPool.plane10, matrix, Statics.BadgeShadow, 0);
			Graphics.DrawMesh(MeshPool.plane10, matrix, Statics.Badges[team], 0);
		}
	}

	// skip to draw progressbar if not in visible range
	//
	[HarmonyPatch(typeof(ToilEffects))]
	[HarmonyPatch(nameof(ToilEffects.WithProgressBar))]
	class ToilEffects_WithProgressBar_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(Toil toil)
		{
			var last2 = toil.preTickActions.Count - 1;
			var action2 = toil.preTickActions[last2];
			toil.preTickActions[last2] = delegate
			{
				if (toil.actor.Faction != Faction.OfPlayer || Ref.controller.IsInVisibleRange(toil.actor))
					action2();
			};
		}
	}

	// skip to draw effecter if not in visible range
	//
	[HarmonyPatch(typeof(ToilEffects))]
	[HarmonyPatch(nameof(ToilEffects.WithEffect))]
	[HarmonyPatch(new[] { typeof(Toil), typeof(Func<EffecterDef>), typeof(Func<LocalTargetInfo>) })]
	class ToilEffects_WithEffect_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(Toil toil)
		{
			var i = toil.preTickActions.Count - 1;
			var action = toil.preTickActions[i];
			toil.preTickActions[i] = delegate
			{
				if (toil.actor.Faction != Faction.OfPlayer || Ref.controller.IsInVisibleRange(toil.actor))
					action();
			};
		}
	}

	// draw pawn shadows only if close by
	// 
	[HarmonyPatch(typeof(Graphic))]
	[HarmonyPatch("Draw")]
	class Graphic_Draw_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(Thing thing)
		{
			var pawn = thing as Pawn;
			if (pawn == null) return true;
			if (pawn.Faction != Faction.OfPlayer) return true;
			var map = pawn.Map;
			if (map == null) return true;
			return Ref.controller.IsInVisibleRange(pawn);
		}
	}

	// only visible and in range objects are selectable
	//
	[HarmonyPatch(typeof(Selector))]
	[HarmonyPatch(nameof(Selector.Select))]
	class Selector_Select_Patch
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
	class Designator_Cell_Patch
	{
		static bool IsVisible(Map map, IntVec3 loc)
		{
			return Tools.IsVisible(map, loc);
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
			var m_get_Map = AccessTools.Property(typeof(Designator), nameof(Designator.Map)).GetGetMethod(true);
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
	class Designator_Thing_Patch
	{
		static bool IsVisible(Thing thing)
		{
			return Tools.IsVisible(thing);
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
			var m_get_Map = AccessTools.Property(typeof(Designator), nameof(Designator.Map)).GetGetMethod(true);
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
	class PlayerPawnsDisplayOrderUtility_Sort_Patch
	{
		[HarmonyPriority(10000)]
		static void Prefix(List<Pawn> pawns)
		{
			var controller = Ref.controller;
			var myColonists = pawns.Where(pawn => controller.InMyTeam(pawn)).ToList();
			pawns.Clear();
			pawns.AddRange(myColonists);
		}
	}

	// skip pawn-overlay if not discovered
	//
	[HarmonyPatch(typeof(PawnUIOverlay))]
	[HarmonyPatch(nameof(PawnUIOverlay.DrawPawnGUIOverlay))]
	class PawnUIOverlay_DrawPawnGUIOverlay_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(Pawn ___pawn)
		{
			if (Ref.controller.battleOverview.showing) return false;
			var controller = Ref.controller;
			if (Tools.IsVisible(___pawn) == false)
				return false;
			return controller.IsInVisibleRange(___pawn);
		}
	}

	// skip thing-overlay if not discovered (1)
	//
	[HarmonyPatch(typeof(Thing))]
	[HarmonyPatch(nameof(Thing.DrawGUIOverlay))]
	class Thing_DrawGUIOverlay_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(Thing __instance)
		{
			if (Ref.controller.battleOverview.showing) return false;
			return Tools.IsVisible(__instance);
		}
	}

	// skip thing-overlay if not discovered (2)
	//
	[HarmonyPatch(typeof(ThingOverlays))]
	[HarmonyPatch(nameof(ThingOverlays.ThingOverlaysOnGUI))]
	class ThingOverlays_ThingOverlaysOnGUI_Patch
	{
		static bool IsFogged(FogGrid grid, IntVec3 c)
		{
			if (Ref.controller.battleOverview.showing) return false;
			if (Tools.IsVisible(Ref.map(grid), c) == false)
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
	class GenSpawn_Spawn_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(Thing newThing, IntVec3 loc, Map map, ref Thing __result)
		{
			if (newThing is Mote && loc.InBounds(map))
				if (Tools.IsVisible(map, loc) == false)
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
	class SectionLayer_FogOfWar_Regenerate__Patch
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
	class OverlayDrawer_DrawAllOverlays_Patch
	{
		static bool IsVisible(Thing key)
		{
			return Tools.IsVisible(key.Map, key.Position);
		}

		[HarmonyPriority(10000)]
		static Instructions Transpiler(Instructions instructions, ILGenerator generator)
		{
			var m_IsVisible = SymbolExtensions.GetMethodInfo(() => IsVisible(null));
			var label = generator.DefineLabel();

			var codes = instructions.ToList();
			var idx1 = codes.FirstIndexOf(code => code.opcode == OpCodes.Stloc_2);
			codes.InsertRange(idx1 + 1, new[]
			{
				new CodeInstruction(OpCodes.Ldloc_2),
				new CodeInstruction(OpCodes.Call, m_IsVisible),
				new CodeInstruction(OpCodes.Brfalse, label)
			});
			var idx2 = codes.FindLastIndex(code => code.opcode == OpCodes.Brtrue);
			codes[idx2 - 2].labels.Add(label);
			return codes.AsEnumerable();
		}
	}

	// hide zones if not discovered
	//
	[HarmonyPatch]
	class SectionLayer_Zones_Regenerate_Patch
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
			var section = Ref.SectionLayer_section(myBase);
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