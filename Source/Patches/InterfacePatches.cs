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

	// make speed control use cooperative speed
	//
	[HarmonyPatch(typeof(TimeControls))]
	[HarmonyPatch(nameof(TimeControls.DoTimeControlsGUI))]
	class TimeControls_DoTimeControlsGUI_Patch
	{
		static readonly MethodInfo m_ButtonImage = SymbolExtensions.GetMethodInfo(() => Widgets.ButtonImage(default, default));
		static readonly MethodInfo m_MyButtonImage = SymbolExtensions.GetMethodInfo(() => ButtonImage(default, default, 0));
		static bool ButtonImage(Rect butRect, Texture2D tex, int i)
		{
			_ = butRect; _ = tex;
			if (Widgets.ButtonImage(butRect, tex))
				Multiplayer.SetSpeed(Ref.controller.team, i);
			return false;
		}

		static readonly MethodInfo m_Event_current = AccessTools.Property(typeof(Event), nameof(Event.current)).GetGetMethod(true);
		static readonly MethodInfo m_SpeedKeyboardEvents = SymbolExtensions.GetMethodInfo(() => Tools.SpeedKeyboardEvents());

		static readonly MultiPatches multiPatches = new MultiPatches(
			typeof(Hostility_MultiPatches),
			new MultiPatchInfo(
				SymbolExtensions.GetMethodInfo(() => TimeControls.DoTimeControlsGUI(default)),
				m_ButtonImage, m_MyButtonImage,
				(codes) => codes.Where(code => code.opcode == OpCodes.Ldloc_S).Take(1).Select(code => { code.opcode = OpCodes.Ldloc_S; return code; })
			),
			new MultiPatchInfo(
				SymbolExtensions.GetMethodInfo(() => TimeControls.DoTimeControlsGUI(default)),
				m_Event_current, m_SpeedKeyboardEvents
			)
		);

		static IEnumerable<MethodBase> TargetMethods()
		{
			return multiPatches.TargetMethods();
		}

		[HarmonyPriority(10000)]
		static Instructions Transpiler(MethodBase original, Instructions instructions)
		{
			return multiPatches.Transpile(original, instructions);
		}
	}

	// remove toggle buttons with cooperative behaviour
	//
	[HarmonyPatch(typeof(WidgetRow))]
	[HarmonyPatch(nameof(WidgetRow.ToggleableIcon))]
	class WidgetRow_ToggleableIcon_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(string tooltip)
		{
			return tooltip != "AutoHomeAreaToggleButton".Translate() && tooltip != "AutoRebuildButton".Translate();
		}
	}

	// move all toggle buttons to one row
	//
	[HarmonyPatch(typeof(GlobalControlsUtility))]
	[HarmonyPatch(nameof(GlobalControlsUtility.DoPlaySettings))]
	class GlobalControlsUtility_DoPlaySettings_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(ref float curBaseY)
		{
			curBaseY -= 20f;
		}

		[HarmonyPriority(10000)]
		static Instructions Transpiler(Instructions instructions)
		{
			return instructions.Select(code =>
			{
				if (code.opcode == OpCodes.Ldc_R4 && (float)code.operand >= 141f)
					code.operand = 999f;
				return code;
			});
		}
	}

	// show cooperative speeds
	//
	[HarmonyPatch(typeof(GlobalControlsUtility))]
	[HarmonyPatch(nameof(GlobalControlsUtility.DoTimespeedControls))]
	class GlobalControlsUtility_DoTimespeedControls_Patch
	{
		public static bool Prefix(float leftX, float width, ref float curBaseY)
		{
			leftX += Mathf.Max(0f, width - 150f);
			width = Mathf.Min(width, 150f);

			var teams = Ref.controller.teams;
			var choices = 2f + 6f * teams.Count;

			var y = TimeControls.TimeButSize.y;
			var timerRect = new Rect(leftX + 16f, curBaseY - y, width, y + choices);

			for (var speed = 0; speed < 4; speed++)
			{
				var rect = new Rect(leftX + 16f + speed * TimeControls.TimeButSize.x, curBaseY + 2f, TimeControls.TimeButSize.x, 2f);
				foreach (var team in teams)
					if (team.gameSpeed == speed)
					{
						Widgets.DrawBoxSolid(rect, Ref.TeamColors[team.id]);
						rect.y += 4f;
					}
			}

			TimeControls.DoTimeControlsGUI(timerRect);
			curBaseY -= timerRect.height + choices;
			return false;
		}
	}

	// remove manual prio checkbox
	//
	[HarmonyPatch(typeof(MainTabWindow_Work))]
	[HarmonyPatch("DoManualPrioritiesCheckbox")]
	class MainTabWindow_Work_DoManualPrioritiesCheckbox_Patch
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
	class MainTabWindow_PawnTable_Pawns_Patch
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
	class MainTabWindow_Animals_Pawns_Patch
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
	class TrainableUtility_MasterSelectButton_GenerateMenu_Patch
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
	[HarmonyPatch(typeof(Messages))]
	[HarmonyPatch("AcceptsMessage")]
	class Messages_AcceptsMessage_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(LookTargets lookTargets, ref bool __result)
		{
			if (__result == false)
				return;
			if (lookTargets.targets.Any(target => target.HasThing && target.Thing is Pawn pawn && Ref.controller.InMyTeam(pawn)) == false)
				__result = false;
		}
	}

	// hide alerts that don't contain at least one team member
	//
	[HarmonyPatch(typeof(Alert))]
	[HarmonyPatch(nameof(Alert.DrawAt))]
	class Alert_DrawAt_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(Alert __instance, ref Rect __result)
		{
			if (__instance.Active == false)
				return true;

			var culprits = __instance.GetReport().culprits;
			if (culprits == null)
				return true;

			var result = culprits
				.Select(culprit => culprit.Thing)
				.OfType<Pawn>()
				.Any(pawn => Ref.controller.InMyTeam(pawn));

			if (result == false)
				__result = Rect.zero;
			return result;
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
	class TargetHighlighter_Highlight_Patch
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
	class Pawn_TryGetAttackVerb_Patch
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
	class SickPawnVisitUtility_CanVisit_Patch
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
	class Pawn_IsColonist_Patch
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
	class MapPawn_FreeColonistsSpawned_Patch
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
	class MainTabWindow_Inspect_DoInspectPaneButtons_Patch
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
	class InspectPaneFiller_DrawAreaAllowed_Patch
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
	class HealthCardUtility_DrawOverviewTab_Patch
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
	class ITab_Patches
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
	class VerbTracker_CreateVerbTargetCommand_Patch
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
	class Pawn_GetGizmos_Patch
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
				yield return gizmo;
		}
	}

	// only show context menu for our own team members
	//
	[HarmonyPatch(typeof(FloatMenuMakerMap))]
	[HarmonyPatch("CanTakeOrder")]
	class FloatMenuMakerMap_CanTakeOrder_Patch
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

	// kill feed
	//
	[HarmonyPatch(typeof(Pawn))]
	[HarmonyPatch(nameof(Pawn.Kill))]
	class Pawn_Kill_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(Pawn __instance, DamageInfo? dinfo)
		{
			if (dinfo == null) return;
			var killer = dinfo.Value.Instigator as Pawn;
			if (killer == null) return;

			//var teamKiller = Ref.controller.GetTeam(killer);
			//var teamKilled = Ref.controller.GetTeam(__instance);
			var weapon = "";
			if (dinfo.Value.Weapon != null)
				weapon = "with " + Find.ActiveLanguageWorker.WithIndefiniteArticle(dinfo.Value.Weapon.label, killer.gender);

			//var message = $"{killer.Name.ToStringShort} of {teamKiller.name} killed {__instance.Name.ToStringShort} of {teamKilled.name} {weapon}";
			var message = $"{killer.Name.ToStringShort} killed {__instance.Name.ToStringShort} {weapon}";
			Messages.Message(message, killer, MessageTypeDefOf.PositiveEvent, true);
		}
	}

	// suppress other teams colonist died alerts
	//
	[HarmonyPatch(typeof(LetterStack))]
	[HarmonyPatch(nameof(LetterStack.ReceiveLetter))]
	[HarmonyPatch(new[] { typeof(string), typeof(string), typeof(LetterDef), typeof(LookTargets), typeof(Faction), typeof(string) })]
	class Pawn_HealthTracker_NotifyPlayerOfKilled_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(LookTargets lookTargets)
		{
			var target = lookTargets.PrimaryTarget;
			if (target.HasThing == false) return true;
			var pawn = target.Thing as Pawn;
			if (pawn == null) return true;
			return Ref.controller.InMyTeam(pawn);
		}
	}

	/* rescue message only for our team
	//
	[HarmonyPatch(typeof(Alert_ColonistNeedsRescuing))]
	[HarmonyPatch("ColonistsNeedingRescue")]
	class Alert_ColonistNeedsRescuing_ColonistsNeedingRescue_Patch
	{
		[HarmonyPriority(10000)]
		static IEnumerable<Pawn> Postfix(IEnumerable<Pawn> pawns)
		{
			return pawns.Where(pawn => Ref.controller.InMyTeam(pawn));
		}
	}*/
}