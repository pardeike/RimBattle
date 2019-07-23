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
using Verse.AI.Group;

namespace RimBattle
{
	using Instructions = IEnumerable<CodeInstruction>;

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

	// make caravan exit map at the correct spot
	//
	[HarmonyPatch]
	static class Dialog_FormCaravan_TryFindExitSpot_Patch
	{
		static MethodBase TargetMethod()
		{
			return AccessTools.Method(typeof(Dialog_FormCaravan), "TryFindExitSpot", new[] { typeof(List<Pawn>), typeof(bool), typeof(IntVec3).MakeByRefType() });
		}

		[HarmonyPriority(10000)]
		static bool Prefix(Dialog_FormCaravan __instance, ref bool __result, List<Pawn> pawns, bool reachableForEveryColonist, out IntVec3 spot)
		{
			// second try: use original
			if (reachableForEveryColonist == false)
			{
				spot = default;
				return true;
			}

			// first try: use our method
			var fromTile = Refs.Dialog_FormCaravan_startingTile(__instance);
			var toTile = Refs.Dialog_FormCaravan_destinationTile(__instance);
			var movingPawns = pawns.Where(pawn => pawn.IsColonist && pawn.Downed == false);
			spot = Tools.FindEdgeSpot(fromTile, toTile, movingPawns, IntVec3.Invalid);
			__result = spot.IsValid;
			return false;
		}
	}

	// delay removing the form caravan lord so we can keep the info 
	// around for a bit longer. we remove it later ourselves after entering
	//
	[HarmonyPatch(typeof(LordManager))]
	[HarmonyPatch("RemoveLord")]
	static class LordManager_RemoveLord_Patch
	{
		static bool Prefix(Lord oldLord)
		{
			if (oldLord.loadID == int.MaxValue) return true;
			return (oldLord.LordJob is LordJob_FormAndSendBattleCaravan) == false;
		}
	}

	// make caravan enter map at the correct spot
	//
	[HarmonyPatch(typeof(CaravanEnterMapUtility))]
	[HarmonyPatch("GetEnterCell")]
	static class CaravanEnterMapUtility_GetEnterCell_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(Caravan caravan, CaravanEnterMode enterMode, ref IntVec3 __result)
		{
			if (enterMode != CaravanEnterMode.Edge)
				return true;

			var firstColonist = caravan.pawns.InnerListForReading.Where(pawn => pawn.IsColonist).FirstOrDefault();
			if (firstColonist == null)
				return true;

			var caravaningLord = Find.Maps
				.SelectMany(map => map.lordManager.lords)
				.FirstOrDefault(lord => (lord.LordJob as LordJob_FormAndSendBattleCaravan)?.pawns?.Contains(firstColonist) ?? false);
			if (caravaningLord == null)
				return true;

			var caravanJob = caravaningLord.LordJob as LordJob_FormAndSendBattleCaravan;
			var enterSpot = Tools.GetEnterSpot(caravanJob.startingTile, caravanJob.destinationTile, caravanJob.exitSpot);

			caravaningLord.loadID = int.MaxValue; // remove it by bypassing the prefix on RemoveLord
			caravaningLord.Map.lordManager.RemoveLord(caravaningLord);

			var spot = Tools.FindEdgeSpot(caravanJob.destinationTile, caravanJob.startingTile, new List<Pawn>(), enterSpot);
			if (spot.IsValid == false)
				return true;

			__result = spot;
			return false;
		}
	}

	// 
	//
	[HarmonyPatch(typeof(CaravanFormingUtility))]
	[HarmonyPatch(nameof(CaravanFormingUtility.StartFormingCaravan))]
	static class CaravanFormingUtility_StartFormingCaravan_Patch
	{
		[HarmonyPriority(10000)]
		static Instructions Transpiler(Instructions instructions)
		{
			var parameters = new[] { typeof(List<TransferableOneWay>), typeof(List<Pawn>), typeof(IntVec3), typeof(IntVec3), typeof(int), typeof(int) };
			var c_LordJob_FormAndSendCaravan = AccessTools.Constructor(typeof(LordJob_FormAndSendCaravan), parameters);
			if (c_LordJob_FormAndSendCaravan == null)
				Log.Error("Cannot find constructor for LordJob_FormAndSendCaravan()");

			var c_LordJob_FormAndSendBattleCaravan = AccessTools.Constructor(typeof(LordJob_FormAndSendBattleCaravan), parameters);
			if (c_LordJob_FormAndSendBattleCaravan == null)
				Log.Error("Cannot find constructor for c_LordJob_FormAndSendBattleCaravan()");

			var codes = instructions.ToList();
			var idx = codes.FirstIndexOf(code => code.opcode == OpCodes.Newobj && code.operand == c_LordJob_FormAndSendCaravan);
			if (idx < 0)
				Log.Error("Cannot find constructor LordJob_FormAndSendCaravan() in CaravanFormingUtility.StartFormingCaravan");

			codes[idx].operand = c_LordJob_FormAndSendBattleCaravan;
			codes.InsertRange(idx + 2, new[]
			{
				new CodeInstruction(OpCodes.Ldloc_S, codes[idx + 1].operand),
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => new LordJob_FormAndSendBattleCaravan().SetPawns(null)))
			});
			return codes.AsEnumerable();
		}
	}

	// make caravan travel instantly
	//
	[HarmonyPatch(typeof(Caravan_PathFollower))]
	[HarmonyPatch("SetupMoveIntoNextTile")]
	static class Caravan_PathFollower_AtDestinationPosition_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(ref Caravan_PathFollower __instance)
		{
			var trv = Traverse.Create(__instance);
			trv.Field("nextTileCostTotal").SetValue(0f);
			trv.Field("nextTileCostLeft").SetValue(0f);
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
			Refs.Dialog_FormCaravan_startingTile(__instance) = Find.CurrentMap.Tile;

			var controller = Refs.controller;
			var reachableTiles = controller.tiles.Where(tile => controller.CanReach(Find.CurrentMap.Tile, tile));
			Refs.Dialog_FormCaravan_destinationTile(__instance) = reachableTiles.Count() == 1 ? reachableTiles.First() : -1;
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
				if (map.Tile == Refs.Dialog_FormCaravan_destinationTile(__instance))
					return Color.green;
				return null;
			}

			bool CanSelect(Map map)
			{
				return Refs.controller.CanReach(Find.CurrentMap, map);
			}

			void SetSelected(Map map)
			{
				Refs.Dialog_FormCaravan_destinationTile(__instance) = map.Tile;
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
				if (codes[i].opcode == OpCodes.Call && codes[i].operand == m_ButtonText)
					codes[i].operand = m_ButtonTextReordered;
			return codes.AsEnumerable();
		}
	}
}