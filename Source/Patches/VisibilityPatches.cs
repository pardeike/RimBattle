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

// ================== TODO =====================
/*
 * Designator_Forbid - patch away forbidding on unowned things
 * Uninstall/reinstall - patch away for unowned things
 */
// ================== TODO =====================

namespace RimBattle
{
	using Instructions = IEnumerable<CodeInstruction>;

	// fake our CompOwnedBy into the init phase when loading from save file
	//
	[HarmonyPatch(typeof(ThingWithComps))]
	[HarmonyPatch(nameof(ThingWithComps.InitializeComps))]
	class ThingWithComps_InitializeComps_Patch
	{
		static void Postfix(ThingWithComps __instance)
		{
			if (Ref.comps(__instance) == null)
				Ref.comps(__instance) = new List<ThingComp>();
			var ownedBy = new CompOwnedBy() { parent = __instance };
			Ref.comps(__instance).Add(ownedBy);
		}
	}

	// things build are owned by the team of the builder
	//
	[HarmonyPatch]
	class OwnedByTeam_MultiPatches
	{
		static readonly MethodInfo m_SetFactionDirect = SymbolExtensions.GetMethodInfo(() => new Thing().SetFactionDirect(default));

		static readonly MethodInfo m_MySetFactionDirect1 = SymbolExtensions.GetMethodInfo(() => MySetFactionDirect(default, default, default(Pawn)));
		static void MySetFactionDirect(Thing thing, Faction newFaction, Pawn owner)
		{
			Log.Warning($"Add {thing} with owner {owner} ID={owner.GetTeamID()}");
			thing.SetFactionDirect(newFaction);
			if (newFaction == Faction.OfPlayer)
				CompOwnedBy.SetTeam(thing as ThingWithComps, owner);
		}

		static readonly MethodInfo m_MySetFactionDirect2 = SymbolExtensions.GetMethodInfo(() => MySetFactionDirect(default, default, 0));
		static void MySetFactionDirect(Thing thing, Faction newFaction, int team)
		{
			Log.Warning($"Add {thing} with ID={team}");
			thing.SetFactionDirect(newFaction);
			if (newFaction == Faction.OfPlayer)
				CompOwnedBy.SetTeam(thing as ThingWithComps, team);
		}

		static readonly MethodInfo m_PlaceBlueprintForBuild = SymbolExtensions.GetMethodInfo(() => GenConstruct.PlaceBlueprintForBuild(default, default, null, default, null, null));
		static readonly MethodInfo m_MyPlaceBlueprintForBuild = SymbolExtensions.GetMethodInfo(() => MyPlaceBlueprintForBuild(default, default, null, default, null, null, 0));
		static Blueprint_Build MyPlaceBlueprintForBuild(BuildableDef sourceDef, IntVec3 center, Map map, Rot4 rotation, Faction faction, ThingDef stuff, int team)
		{
			var blueprint = GenConstruct.PlaceBlueprintForBuild(sourceDef, center, map, rotation, faction, stuff);
			CompOwnedBy.SetTeam(blueprint, team);
			return blueprint;
		}

		static int CurrentTeam()
		{
			return Ref.controller.team;
		}

		static readonly MultiPatches multiPatches = new MultiPatches(
			typeof(OwnedByTeam_MultiPatches),
			new MultiPatchInfo(
				SymbolExtensions.GetMethodInfo(() => new Frame().CompleteConstruction(null)),
				m_SetFactionDirect, m_MySetFactionDirect1,
				new CodeInstruction(OpCodes.Ldarg_1)
			),
			new MultiPatchInfo(
				AccessTools.Method(typeof(Blueprint), nameof(Blueprint.TryReplaceWithSolidThing)),
				m_SetFactionDirect, m_MySetFactionDirect1,
				new CodeInstruction(OpCodes.Ldarg_1)
			),
			new MultiPatchInfo(
				AccessTools.Method(typeof(Designator_Build), nameof(Designator_Build.DesignateSingleCell)),
				m_SetFactionDirect, m_MySetFactionDirect2,
				new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => CurrentTeam()))
			),
			new MultiPatchInfo(
				AccessTools.Method(typeof(Designator_Build), nameof(Designator_Build.DesignateSingleCell)),
				m_PlaceBlueprintForBuild, m_MyPlaceBlueprintForBuild,
				new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => CurrentTeam()))
			)
		);

		static IEnumerable<MethodBase> TargetMethods()
		{
			return multiPatches.TargetMethods();
		}

		[HarmonyPriority(10000)]
		static Instructions Transpiler(MethodBase original, Instructions codes)
		{
			return multiPatches.Transpile(original, codes);
		}
	}

	// patch all workgivers to disallow work on not-owned things
	//
	[HarmonyPatch]
	class OwnedByTeam_WorkGiver_Patches
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			return Tools.GetMethodsFromSubclasses(typeof(WorkGiver_Scanner), nameof(WorkGiver_Scanner.HasJobOnThing));
		}

		static bool Prefix(Pawn pawn, Thing t)
		{
			if (pawn == null || t == null)
				return true;
			var thingTeam = t.OwnedByTeam();
			var workerTeam = pawn.GetTeamID();
			return thingTeam < 0 || workerTeam < 0 || thingTeam == workerTeam;
		}
	}

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
			return Tools.GetMethodsFromSubclasses(typeof(Designator), nameof(Designator.CanDesignateCell));
		}

		[HarmonyPriority(10000)]
		static Instructions Transpiler(Instructions codes, ILGenerator generator)
		{
			if (codes.Count() > 2)
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
			}
			foreach (var code in codes)
				yield return code;
		}
	}

	// disallow designating things that are not visible or not ours
	//
	[HarmonyPatch]
	class Designator_Thing_Patch
	{
		static bool IsVisible(Thing thing)
		{
			var thingTeam = thing.OwnedByTeam();
			if (thingTeam >= 0 && thingTeam != Ref.controller.team)
				return false;
			return Tools.IsVisible(thing);
		}

		static IEnumerable<MethodBase> TargetMethods()
		{
			return Tools.GetMethodsFromSubclasses(typeof(Designator), nameof(Designator.CanDesignateThing));
		}

		[HarmonyPriority(10000)]
		static Instructions Transpiler(Instructions codes, ILGenerator generator)
		{
			if (codes.Count() > 2)
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
			}
			foreach (var code in codes)
				yield return code;
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
		static Instructions Transpiler(Instructions codes)
		{
			return Transpilers.MethodReplacer(codes,
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
		static Instructions Transpiler(Instructions codes)
		{
			codes.GetHashCode(); // make compiler happy
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
		static Instructions Transpiler(Instructions codes)
		{
			_ = codes; // make compiler happy
			var replacement = SymbolExtensions.GetMethodInfo(() => CopiedMethods.RegenerateFog(null));
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Call, replacement);
			yield return new CodeInstruction(OpCodes.Ret);
		}
	}
}