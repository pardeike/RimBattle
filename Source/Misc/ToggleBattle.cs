using Harmony;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimBattle
{
	public class ToggleBattle : MainButtonWorker
	{
		public static KeyBindingDef BattleMap;
		public static MainButtonDef Battle;

		public static void Patch()
		{
			var harmony = HarmonyInstance.Create("net.pardeike.rimworld.mod.rimbattle.defs");
			var original = SymbolExtensions.GetMethodInfo(() => DefGenerator.GenerateImpliedDefs_PostResolve());
			var postfix = SymbolExtensions.GetMethodInfo(() => PostResolve());
			harmony.Patch(original, null, new HarmonyMethod(postfix));
		}

		[HarmonyPriority(10000)]
		static void PostResolve()
		{
			BattleMap = new KeyBindingDef()
			{
				label = "Toggle battle map tab",
				defName = "MainTab_Battle",
				category = KeyBindingCategoryDefOf.MainTabs,
				defaultKeyCodeA = KeyCode.Tab,
				defaultKeyCodeB = KeyCode.BackQuote,
				modContentPack = MainButtonDefOf.Architect.modContentPack
			};
			DefDatabase<KeyBindingDef>.Add(BattleMap);

			Battle = new MainButtonDef()
			{
				defName = "Battle",
				label = "battle",
				description = "Shows the main battle overview with its 7 maps and possible spawns.",
				workerClass = typeof(ToggleBattle),
				order = 100,
				defaultHotKey = KeyCode.F12,
				validWithoutMap = true
			};
			DefDatabase<MainButtonDef>.Add(Battle);
		}

		public static void Toggle()
		{
			if (Find.MainTabsRoot.OpenTab != null)
				Find.MainTabsRoot.EscapeCurrentTab(false);

			var battleOverview = Ref.controller.battleOverview;
			battleOverview.showing = !battleOverview.showing;

			if (battleOverview.showing)
				SoundDefOf.TabOpen.PlayOneShotOnCamera(null);
			else
				SoundDefOf.TabClose.PlayOneShotOnCamera(null);
		}

		public override void Activate()
		{
			Toggle();
		}

		public override float ButtonBarPercent => Ref.controller.ProgressPercent;
	}
}