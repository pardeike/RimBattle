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

		static readonly MethodInfo m_MySetFactionDirect1 = SymbolExtensions.GetMethodInfo(() => MySetFactionDirect(default, default, default));
		static void MySetFactionDirect(Thing thing, Faction newFaction, Pawn owner)
		{
			thing.SetFactionDirect(newFaction);
			if (newFaction == Faction.OfPlayer)
				CompOwnedBy.SetTeam(thing as ThingWithComps, owner);
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
			)
		);

		static IEnumerable<MethodBase> TargetMethods()
		{
			return multiPatches.TargetMethods();
		}

		static Instructions Transpiler(MethodBase original, Instructions codes)
		{
			return multiPatches.Transpile(original, codes);
		}
	}

	// patch lots of methods to disallow work on not-owned things
	//
	[HarmonyPatch]
	class OwnedByTeam_WorkGiver_Patches
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(WorkGiver_ConstructDeliverResources), "IsNewValidNearbyNeeder");
			yield return SymbolExtensions.GetMethodInfo(() => new ReservationManager(null).CanReserve(default, default, 0, 0, null, false));
			yield return SymbolExtensions.GetMethodInfo(() => new ReservationManager(null).CanReserveStack(default, default, 0, null, false));
			yield return SymbolExtensions.GetMethodInfo(() => new ReservationManager(null).IsReservedAndRespected(default, default));
			yield return SymbolExtensions.GetMethodInfo(() => new ReservationManager(null).ReservedBy(default, default, default));
			yield return SymbolExtensions.GetMethodInfo(() => ForbidUtility.IsForbidden(default(Thing), default(Pawn)));
			yield return SymbolExtensions.GetMethodInfo(() => ForbidUtility.IsForbiddenToPass(default, default));
			yield return SymbolExtensions.GetMethodInfo(() => new PhysicalInteractionReservationManager().IsReservedBy(default, default));
			yield return SymbolExtensions.GetMethodInfo(() => RestUtility.IsValidBedFor(default, default, default, false, false, false, false));
		}

		static bool CanHandle(string method, Pawn pawn, Thing t)
		{
			if (pawn == null || t == null)
				return true;
			if (t is Building_Door door && door.FreePassage && method == "IsForbiddenToPass")
				return true;
			var thingTeam = t.GetTeamID();
			var workerTeam = pawn.GetTeamID();
			return thingTeam < 0 || workerTeam < 0 || thingTeam == workerTeam;
		}

		static Instructions Transpiler(MethodBase original, Instructions codes, ILGenerator generator)
		{
			var label = generator.DefineLabel();
			var parameterTypes = original.GetParameters().Types().ToList();
			var returnFalseIfCannotHandle = new[] { "IsNewValidNearbyNeeder", "CanReserve", "CanReserveStack", "IsValidBedFor", }
				.Contains(original.Name);

			var skipOneArg = original.IsStatic ? 0 : 1;
			var m_OwnedBy = SymbolExtensions.GetMethodInfo(() => CanHandle("", null, null));
			var m_get_Thing = AccessTools.Property(typeof(LocalTargetInfo), nameof(LocalTargetInfo.Thing)).GetGetMethod(true);

			yield return new CodeInstruction(OpCodes.Ldstr, original.Name);

			yield return parameterTypes
				.Where(type => typeof(Pawn).IsAssignableFrom(type))
				.Select(type => new CodeInstruction(OpCodes.Ldarg, skipOneArg + parameterTypes.IndexOf(type)))
				.FirstOrDefault();

			var idx = original.GetParameters().IndexOf(p => p.ParameterType == typeof(LocalTargetInfo));
			if (idx >= 0)
			{
				yield return new CodeInstruction(OpCodes.Ldarga_S, skipOneArg + idx);
				yield return new CodeInstruction(OpCodes.Call, m_get_Thing);
			}
			else
			{
				yield return parameterTypes
				.Where(type => typeof(ThingWithComps).IsAssignableFrom(type))
				.Select(type => new CodeInstruction(OpCodes.Ldarg, skipOneArg + parameterTypes.IndexOf(type)))
				.FirstOrDefault();
			}

			yield return new CodeInstruction(OpCodes.Call, m_OwnedBy);
			yield return new CodeInstruction(OpCodes.Brtrue, label);
			yield return new CodeInstruction(returnFalseIfCannotHandle ? OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1);
			yield return new CodeInstruction(OpCodes.Ret);

			codes.First().labels.Add(label);
			foreach (var code in codes)
				yield return code;
		}
	}

	// uncover map when moving
	//
	[HarmonyPatch(typeof(Thing))]
	[HarmonyPatch(nameof(Thing.Position), MethodType.Setter)]
	class Thing_Position_Patch
	{
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
		static bool Prefix(Pawn ___pawn, out bool __state)
		{
			__state = true;
			if (___pawn.Faction != Faction.OfPlayer) return true;
			var map = ___pawn.Map;
			if (map == null) return true;
			__state = Ref.controller.IsInVisibleRange(___pawn);
			return __state;
		}

		static void Postfix(Pawn ___pawn, bool __state)
		{
			if (__state == false)
				return;

			var team = ___pawn.GetTeamID();
			if (team < 0 || Ref.controller.IsCurrentTeam(team)) return;

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
		static bool Prefix(object obj, bool forceDesignatorDeselect)
		{
			// Designator_ZoneAdd.set_SelectedZone does some funky stuff 
			if (forceDesignatorDeselect == false) return true;

			return Tools.CanSelect(Ref.controller.Team, obj);
		}
	}

	// disallow designation in cells that are not visible
	//
	[HarmonyPatch]
	class Designator_Cell_Patch
	{
		static bool Accessible(Designator designator, IntVec3 loc)
		{
			var map = designator.Map;
			/*if (designator is Designator_Zone designatorZone)
			{
				var zone = map.zoneManager.ZoneAt(loc);
				return zone.CanAccess();
			}*/
			return Tools.IsVisible(Ref.controller.Team, map, loc);
		}

		static IEnumerable<MethodBase> TargetMethods()
		{
			return Tools.GetMethodsFromSubclasses(typeof(Designator), nameof(Designator.CanDesignateCell));
		}

		static Instructions Transpiler(Instructions codes, ILGenerator generator)
		{
			if (codes.Count() > 2)
			{
				var m_Accesssible = SymbolExtensions.GetMethodInfo(() => Accessible(null, IntVec3.Zero));
				var m_get_WasRejected = AccessTools.Method(typeof(AcceptanceReport), "get_WasRejected");
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Call, m_Accesssible);
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
			/*var thingTeam = thing.OwnedByTeam();
			if (thingTeam >= 0 && thingTeam != Ref.controller.team)
				return false;*/
			return Tools.IsVisible(Ref.controller.Team, thing);
		}

		static IEnumerable<MethodBase> TargetMethods()
		{
			return Tools.GetMethodsFromSubclasses(typeof(Designator), nameof(Designator.CanDesignateThing));
		}

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
		static bool Prefix(Pawn ___pawn)
		{
			var controller = Ref.controller;
			if (Tools.IsVisible(Ref.controller.Team, ___pawn) == false)
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
		static bool Prefix(Thing __instance)
		{
			return Tools.IsVisible(Ref.controller.Team, __instance);
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
			if (Tools.IsVisible(Ref.controller.Team, Ref.map(grid), c) == false)
				return false;
			return grid.IsFogged(c);
		}

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
		static bool Prefix(Thing newThing, IntVec3 loc, Map map, ref Thing __result)
		{
			if (newThing is Mote && loc.InBounds(map))
				if (Tools.IsVisible(Ref.controller.Team, map, loc) == false)
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
		static Instructions Transpiler(Instructions codes)
		{
			_ = codes;
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
			return Tools.IsVisible(Ref.controller.Team, key.Map, key.Position);
		}

		static Instructions Transpiler(Instructions instructions, ILGenerator generator)
		{
			var m_IsVisible = SymbolExtensions.GetMethodInfo(() => IsVisible(null));
			var label = generator.DefineLabel();

			var codes = instructions.ToList();
			var idx1 = codes.IndexOf(code => code.opcode == OpCodes.Stloc_2);
			if (idx1 < 1)
				Log.Error("Cannot find Stloc.2 in OverlayDrawer.DrawAllOverlays");
			codes.InsertRange(idx1 + 1, new[]
			{
				new CodeInstruction(OpCodes.Ldloc_2),
				new CodeInstruction(OpCodes.Call, m_IsVisible),
				new CodeInstruction(OpCodes.Brfalse, label)
			});
			var idx2 = codes.FindLastIndex(code => code.opcode == OpCodes.Brtrue);
			if (idx2 < 2)
				Log.Error("Cannot find Brtrue in OverlayDrawer.DrawAllOverlays");
			codes[idx2 - 2].labels.Add(label);
			return codes.AsEnumerable();
		}
	}

	// hide zones if not discovered
	//
	/*[HarmonyPatch]
	class SectionLayer_Zones_Regenerate_Patch
	{
		static MethodBase TargetMethod()
		{
			var method = AccessTools.Method("Verse.SectionLayer_Zones:Regenerate");
			"Method Verse.SectionLayer_Zones:Regenerate".NullCheck(method);
			return method;
		}

		static void RegenerateZone(object __instance)
		{
			var myBase = __instance as SectionLayer;
			if (myBase == null) return;
			var section = Ref.SectionLayer_section(myBase);
			CopiedMethods.RegenerateZone(myBase, section);
		}

		static Instructions Transpiler(Instructions codes)
		{
			_ = codes;
			var m_RegenerateZone = SymbolExtensions.GetMethodInfo(() => RegenerateZone(null));
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Call, m_RegenerateZone);
			yield return new CodeInstruction(OpCodes.Ret);
		}
	}*/

	/*// only deal with owned zones (1)
	//
	[HarmonyPatch(typeof(GridsUtility))]
	[HarmonyPatch(nameof(GridsUtility.GetZone))]
	class GridsUtility_GetZone_Patch
	{
		static void Postfix(ref Zone __result)
		{
			if (__result == null)
				return;
			if (__result.CanAccess() == false)
				__result = null;
		}
	}

	// only deal with owned zones (2)
	//
	[HarmonyPatch(typeof(HaulAIUtility))]
	[HarmonyPatch("HaulablePlaceValidator")]
	class HaulAIUtility_HaulablePlaceValidator_Patch
	{
		static void Postfix(Thing haulable, Pawn worker, IntVec3 c, ref bool __result)
		{
			if (__result == false)
				return;

			var pawnTeam = worker.OwnedByTeam();
			if (pawnTeam < 0)
				return;

			if (haulable != null && haulable.def.BlockPlanting)
			{
				var zone = worker.Map.zoneManager.ZoneAt(c);
				if (zone.OwnedByTeam() != pawnTeam)
					__result = false;
			}
		}
	}

	// only deal with owned zones (3)
	//
	[HarmonyPatch]
	class Selector_SelectableObjects_Patches
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(Selector), "SelectableObjectsUnderMouse");
			yield return AccessTools.Method(typeof(Selector), nameof(Selector.SelectableObjectsAt));
		}

		static IEnumerable<object> Postfix(IEnumerable<object> objects)
		{
			foreach (var obj in objects)
			{
				if (obj is Zone zone)
					if (zone.CanAccess() == false)
						continue;
				yield return obj;
			}
		}
	}

	// only deal with owned zones (4)
	//
	[HarmonyPatch(typeof(WorkGiver_Grower))]
	[HarmonyPatch(nameof(WorkGiver_Grower.PotentialWorkCellsGlobal))]
	class WorkGiver_Grower_PotentialWorkCellsGlobal_Patch
	{
		static List<Zone> AllZones(ZoneManager zoneManager)
		{
			return zoneManager.AllZones.Where(Tools.CanAccess).ToList();
		}

		static Instructions Transpiler(Instructions codes)
		{
			return Transpilers.MethodReplacer(codes,
				AccessTools.Property(typeof(ZoneManager), nameof(ZoneManager.AllZones)).GetGetMethod(true),
				SymbolExtensions.GetMethodInfo(() => AllZones(null))
			);
		}
	}*/
}