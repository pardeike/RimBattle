namespace RimBattle
{
	// maybe used later

	/*[HarmonyPatch(typeof(MapInterface))]
	[HarmonyPatch(nameof(MapInterface.Notify_SwitchedMap))]
	static class MapInterface_Notify_SwitchedMap_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix()
		{
			Find.ColonistBar.MarkColonistsDirty();
		}
	}*/

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
