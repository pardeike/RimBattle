namespace RimBattle
{
	// maybe used later

	/*[HarmonyPatch(typeof(VerbTracker))]
	[HarmonyPatch("CreateVerbTargetCommand")]
	class VerbTracker_CreateVerbTargetCommand_Patch
	{
		static void Postfix(Verb verb, Command_VerbTarget __result)
		{
			if (verb.caster is Pawn pawn && pawn.IsColonist && Ref.controller.InMyTeam(pawn) == false)
				__result.Disable("CannotOrderNonControlled".Translate());
		}
	}*/

	/*[HarmonyPatch(typeof(World))]
	[HarmonyPatch(nameof(World.FinalizeInit))]
	class World_FinalizeInit_Patch
	{
		static void Postfix()
		{
		}
	}*/

	/*[HarmonyPatch(typeof(MapInterface))]
	[HarmonyPatch(nameof(MapInterface.Notify_SwitchedMap))]
	class MapInterface_Notify_SwitchedMap_Patch
	{
		static void Postfix()
		{
			Find.ColonistBar.MarkColonistsDirty();
		}
	}*/

	/*[HarmonyPatch(typeof(DynamicDrawManager))]
	[HarmonyPatch(nameof(DynamicDrawManager.DrawDynamicThings))]
	class DynamicDrawManager_DrawDynamicThings_Patch
	{
		static void Prefix(HashSet<Thing> ___drawThings, out List<Thing> __state)
		{
			__state = ___drawThings.ToList();
			___drawThings.RemoveWhere(thing => Ref.controller.IsVisible(thing) == false);
		}

		
		static void Postfix(HashSet<Thing> ___drawThings, List<Thing> __state)
		{
			___drawThings.Clear();
			___drawThings.AddRange(__state);
		}
	}*/

	/*[HarmonyPatch(typeof(PawnNameColorUtility))]
	[HarmonyPatch(nameof(PawnNameColorUtility.PawnNameColorOf))]
	class PawnNameColorUtility_PawnNameColorOf_Patch
	{
		static bool Prefix(Pawn pawn, ref Color __result)
		{
			var team = Ref.controller.TeamForPawn(pawn);
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
	/*[HarmonyPatch(typeof(FogGrid))]
	[HarmonyPatch(nameof(FogGrid.IsFogged), new[] { typeof(IntVec3) })]
	class FogGrid_IsFogged1_Patch
	{
		static bool Prefix(Map ___map, IntVec3 c, ref bool __result)
		{
			if (c.InBounds(___map) == false)
			{
				__result = false;
				return false;
			}

			if (Ref.controller.mapParts.TryGetValue(___map, out var mapPart))
				if (mapPart.visibility.IsVisible(c) == false)
				{
					__result = true;
					return false;
				}
			return true;
		}
	}*/

	// fake IsFogged 2
	//
	/*[HarmonyPatch(typeof(FogGrid))]
	[HarmonyPatch(nameof(FogGrid.IsFogged), new[] { typeof(int) })]
	class FogGrid_IsFogged2_Patch
	{
		static bool Prefix(Map ___map, int index, ref bool __result)
		{
			if (Ref.controller.mapParts.TryGetValue(___map, out var mapPart))
				if (mapPart.visibility.IsVisible(index) == false)
				{
					__result = true;
					return false;
				}
			return true;
		}
	}*/

	/* rescue message only for our team
	//
	[HarmonyPatch(typeof(Alert_ColonistNeedsRescuing))]
	[HarmonyPatch("ColonistsNeedingRescue")]
	class Alert_ColonistNeedsRescuing_ColonistsNeedingRescue_Patch
	{
		static IEnumerable<Pawn> Postfix(IEnumerable<Pawn> pawns)
		{
			return pawns.Where(pawn => Ref.controller.InMyTeam(pawn));
		}
	}*/
}
