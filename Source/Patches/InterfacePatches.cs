using Harmony;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace RimBattle
{
	using Instructions = IEnumerable<CodeInstruction>;

	// getting our own OnGUI
	//
	[HarmonyPatch(typeof(MainTabWindow_Work))]
	[HarmonyPatch("DoManualPrioritiesCheckbox")]
	static class MainTabWindow_Work_DoManualPrioritiesCheckbox_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix()
		{
			Text.Font = GameFont.Small;
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
			var rect = new Rect(5f, 5f, 140f, 30f);
			Widgets.Label(rect, "ManualPriorities".Translate() + ": " + "On".Translate());
			return false;
		}
	}

	// show all my team member from all maps in all work tabs
	//
	[HarmonyPatch(typeof(MainTabWindow_PawnTable))]
	[HarmonyPatch("Pawns", MethodType.Getter)]
	static class MainTabWindow_PawnTable_Pawns_Patch
	{
		[HarmonyPriority(10000)]
		static IEnumerable<Pawn> Postfix(IEnumerable<Pawn> pawns)
		{
			_ = pawns;
			foreach (var pawn in Ref.controller.teams[Ref.controller.team].members)
				yield return pawn;
		}
	}

	// show all my animals from all maps in all work tabs
	//
	[HarmonyPatch(typeof(MainTabWindow_Animals))]
	[HarmonyPatch("Pawns", MethodType.Getter)]
	static class MainTabWindow_Animals_Pawns_Patch
	{
		[HarmonyPriority(10000)]
		static IEnumerable<Pawn> Postfix(IEnumerable<Pawn> pawns)
		{
			_ = pawns;
			return Current.Game.Maps
				.SelectMany(map => map.mapPawns.PawnsInFaction(Faction.OfPlayer))
				.Where(pawn => pawn.RaceProps.Animal)
				.Where(Ref.controller.InMyTeam);
		}
	}

	// skip "(none)" as a choice in the animal menu
	//
	[HarmonyPatch(typeof(TrainableUtility))]
	[HarmonyPatch("MasterSelectButton_GenerateMenu")]
	static class TrainableUtility_MasterSelectButton_GenerateMenu_Patch
	{
		[HarmonyPriority(10000)]
		static IEnumerable<Widgets.DropdownMenuElement<Pawn>> Postfix(IEnumerable<Widgets.DropdownMenuElement<Pawn>> menuItems)
		{
			return menuItems.Skip(1)
				.Where(menuItem => Ref.controller.InMyTeam(menuItem.payload));
		}
	}

	// hide alerts that don't contain at least one team member
	//
	[HarmonyPatch(typeof(Alert))]
	[HarmonyPatch(nameof(Alert.DrawAt))]
	static class Alert_DrawAt_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(Alert __instance)
		{
			if (__instance.Active == false)
				return true;

			var culprits = __instance.GetReport().culprits;
			if (culprits == null)
				return true;

			return culprits
				.Select(culprit => culprit.Thing)
				.OfType<Pawn>()
				.Any(pawn => Ref.controller.InMyTeam(pawn));
		}

		static IEnumerable<GlobalTargetInfo> OnlyMyTeamCulprits(IEnumerable<GlobalTargetInfo> culprits)
		{
			if (culprits == null) yield break;
			foreach (var culprit in culprits)
			{
				if (Ref.controller.InMyTeam(culprit.Thing as Pawn))
					yield return culprit;
			}
		}

		[HarmonyPriority(10000)]
		static Instructions Transpiler(Instructions instructions)
		{
			var codes = instructions.ToList();
			var m_OnlyMyTeamCulprits = SymbolExtensions.GetMethodInfo(() => OnlyMyTeamCulprits(null));
			var f_culprits = AccessTools.Field(typeof(AlertReport), nameof(AlertReport.culprits));

			var idx = codes.FindIndex(code => code.opcode == OpCodes.Ldfld && code.operand == f_culprits);
			if (idx == -1) Log.Error("Cannot find Ldfld AlertReport.culprits in Alert.DrawAt");
			codes.Insert(idx + 1, new CodeInstruction(OpCodes.Call, m_OnlyMyTeamCulprits));
			return codes.AsEnumerable();
		}
	}

	// only highlight our team targets
	//
	[HarmonyPatch(typeof(TargetHighlighter))]
	[HarmonyPatch(nameof(TargetHighlighter.Highlight))]
	static class TargetHighlighter_Highlight_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(GlobalTargetInfo target)
		{
			if (target.IsValid == false)
				return true;

			var pawn = target.Thing as Pawn;
			if (pawn == null)
				return true;

			return Ref.controller.InMyTeam(pawn);
		}
	}

	// allow using weapons to attack colonist to colonist
	//
	[HarmonyPatch(typeof(Pawn))]
	[HarmonyPatch(nameof(Pawn.TryGetAttackVerb))]
	static class Pawn_TryGetAttackVerb_Patch
	{
		[HarmonyPriority(10000)]
		static void Prefix(ref bool allowManualCastWeapons)
		{
			allowManualCastWeapons = true;
		}
	}

	// only visit sick colonists when they are in the same team
	//
	[HarmonyPatch(typeof(SickPawnVisitUtility))]
	[HarmonyPatch(nameof(SickPawnVisitUtility.CanVisit))]
	static class SickPawnVisitUtility_CanVisit_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(Pawn pawn, Pawn sick, ref bool __result)
		{
			if (__result == false) return;
			var t1 = pawn.GetTeamID();
			var t2 = sick.GetTeamID();
			if (t1 >= 0 && t2 >= 0 && t1 != t2)
				__result = false;
		}
	}

	// make other team members show the same UX like non-colonists (1)
	//
	[HarmonyPatch]
	static class Pawn_IsColonist_Patch
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return SymbolExtensions.GetMethodInfo(() => CharacterCardUtility.DrawCharacterCard(default, null, null, default));
			yield return AccessTools.Method(typeof(HealthCardUtility), "DrawOverviewTab");
			yield return AccessTools.Method(typeof(PawnDiedOrDownedThoughtsUtility), "AppendThoughts_ForHumanlike");
		}

		static bool IsMyColonist(Pawn pawn)
		{
			return pawn.IsColonist && Ref.controller.InMyTeam(pawn);
		}

		static Faction OnlyMyFaction(Thing thing)
		{
			var faction = thing.Faction;
			if (faction == Faction.OfPlayer)
			{
				var pawn = Find.Selector.SingleSelectedThing as Pawn;
				if (pawn != null && Ref.controller.InMyTeam(pawn) == false)
					faction = new Faction();
			}
			return faction;
		}

		static Instructions Transpiler(Instructions instructions)
		{
			var m_get_IsColonist = AccessTools.Property(typeof(Pawn), nameof(Pawn.IsColonist)).GetGetMethod(true);
			var m_get_Faction = AccessTools.Property(typeof(Thing), nameof(Thing.Faction)).GetGetMethod(true);
			return instructions
				.MethodReplacer(m_get_IsColonist, SymbolExtensions.GetMethodInfo(() => IsMyColonist(null)))
				.MethodReplacer(m_get_Faction, SymbolExtensions.GetMethodInfo(() => OnlyMyFaction(null)));
		}
	}

	// make other team members show the same UX like non-colonists (2)
	//
	[HarmonyPatch]
	static class MapPawn_FreeColonistsSpawned_Patch
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Property(typeof(MapPawns), nameof(MapPawns.FreeColonistsSpawned)).GetGetMethod(true);
			yield return AccessTools.Method(typeof(Command_SetPlantToGrow), "WarnAsAppropriate");
		}

		static IEnumerable<Pawn> FreeMyColonistsSpawned(MapPawns mapPawns)
		{
			return mapPawns.FreeColonistsSpawned.Where(Ref.controller.InMyTeam);
		}

		static Instructions Transpiler(Instructions instructions)
		{
			var m_get_FreeColonistsSpawned = AccessTools.Property(typeof(MapPawns), nameof(MapPawns.FreeColonistsSpawned)).GetGetMethod(true);
			return instructions.MethodReplacer(m_get_FreeColonistsSpawned, SymbolExtensions.GetMethodInfo(() => FreeMyColonistsSpawned(null)));
		}
	}

	// remove react button from non team members
	//
	[HarmonyPatch(typeof(MainTabWindow_Inspect))]
	[HarmonyPatch("DoInspectPaneButtons")]
	static class MainTabWindow_Inspect_DoInspectPaneButtons_Patch
	{
		static bool UsesMyConfigurableHostilityResponse(Pawn_PlayerSettings playerSettings)
		{
			var response = playerSettings.UsesConfigurableHostilityResponse;
			if (response)
			{
				var pawn = Find.Selector.SingleSelectedThing as Pawn;
				if (pawn != null && Ref.controller.InMyTeam(pawn) == false)
					response = false;
			}
			return response;
		}

		static Instructions Transpiler(Instructions instructions)
		{
			var m_get_UsesConfigurableHostilityResponse = AccessTools.Property(typeof(Pawn_PlayerSettings), nameof(Pawn_PlayerSettings.UsesConfigurableHostilityResponse)).GetGetMethod(true);
			return instructions.MethodReplacer(m_get_UsesConfigurableHostilityResponse, SymbolExtensions.GetMethodInfo(() => UsesMyConfigurableHostilityResponse(null)));
		}
	}

	// remove schedule and restrict buttons from non team members 
	//
	[HarmonyPatch]
	static class InspectPaneFiller_DrawAreaAllowed_Patch
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(InspectPaneFiller), "DrawAreaAllowed");
			yield return AccessTools.Method(typeof(InspectPaneFiller), "DrawTimetableSetting");
		}

		[HarmonyPriority(10000)]
		static bool Prefix(Pawn pawn)
		{
			if (pawn.IsColonist && Ref.controller.InMyTeam(pawn) == false)
				return false;
			return true;
		}
	}

	// disable health control of enemy team members
	//
	[HarmonyPatch(typeof(HealthCardUtility))]
	[HarmonyPatch("DrawOverviewTab")]
	static class HealthCardUtility_DrawOverviewTab_Patch
	{
		static bool ConfigurableIfOurTeam(Pawn_FoodRestrictionTracker tracker)
		{
			var configurable = tracker.Configurable;
			if (configurable)
			{
				var pawn = Find.Selector.SingleSelectedThing as Pawn;
				if (pawn != null && Ref.controller.InMyTeam(pawn) == false)
					configurable = false;
			}
			return configurable;
		}

		static void Prefix(Pawn pawn, out Pawn_PlayerSettings __state)
		{
			__state = pawn.playerSettings;
			if (Ref.controller.InMyTeam(pawn) == false)
				pawn.playerSettings = null;
		}

		static void Postfix(Pawn pawn, Pawn_PlayerSettings __state)
		{
			pawn.playerSettings = __state;
		}

		static Instructions Transpiler(Instructions instructions)
		{
			var m_get_Configurable = AccessTools.Property(typeof(Pawn_FoodRestrictionTracker), nameof(Pawn_FoodRestrictionTracker.Configurable)).GetGetMethod(true);
			return instructions.MethodReplacer(m_get_Configurable, SymbolExtensions.GetMethodInfo(() => ConfigurableIfOurTeam(null)));
		}
	}

	// disable gear control of enemy team members
	// disable operations of enemy team members
	// disable storage control of enemy team members
	//
	[HarmonyPatch]
	static class ITab_Patches
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Property(typeof(ITab_Pawn_Gear), "CanControl").GetGetMethod(true);
			yield return AccessTools.Method(typeof(ITab_Pawn_Health), "ShouldAllowOperations");
			yield return AccessTools.Property(typeof(ITab_Storage), "IsVisible").GetGetMethod(true);
		}

		[HarmonyPriority(10000)]
		static void Postfix(ref bool __result)
		{
			if (__result == false) return;
			var pawn = Find.Selector.SingleSelectedThing as Pawn;
			if (pawn != null && Ref.controller.InMyTeam(pawn) == false)
				__result = false;
		}
	}

	// disable target command gizmos for other team members
	//
	[HarmonyPatch(typeof(VerbTracker))]
	[HarmonyPatch("CreateVerbTargetCommand")]
	static class VerbTracker_CreateVerbTargetCommand_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(Verb verb, Command_VerbTarget __result)
		{
			if (verb.caster is Pawn pawn && pawn.IsColonist && Ref.controller.InMyTeam(pawn) == false)
				__result.Disable("CannotOrderNonControlled".Translate());
		}
	}

	// only show full range of gizmos for our own team members
	//
	[HarmonyPatch(typeof(Pawn))]
	[HarmonyPatch(nameof(Pawn.GetGizmos))]
	static class Pawn_GetGizmos_Patch
	{
		[HarmonyPriority(10000)]
		static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Pawn __instance)
		{
			if (__instance.IsColonist && Ref.controller.InMyTeam(__instance) == false)
			{
				if (__instance.equipment != null)
					foreach (var gizmo in __instance.equipment.GetGizmos())
						yield return gizmo;
				foreach (var gizmo in __instance.mindState.GetGizmos())
					yield return gizmo;
				yield break;
			}

			foreach (var gizmo in gizmos)
			{
				Log.Warning($"{__instance.Name.ToStringShort} {gizmo} visible={gizmo.Visible} disabled-reason={gizmo.disabledReason}");
				yield return gizmo;
			}
		}
	}

	// only show context menu for our own team members
	//
	[HarmonyPatch(typeof(FloatMenuMakerMap))]
	[HarmonyPatch("CanTakeOrder")]
	static class FloatMenuMakerMap_CanTakeOrder_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(Pawn pawn, ref bool __result)
		{
			if (__result == false)
				return;

			if (Ref.controller.InMyTeam(pawn) == false)
				__result = false;
		}
	}
}