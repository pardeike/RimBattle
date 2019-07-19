using RimWorld;
using Verse;
using Verse.Sound;

namespace RimBattle
{
	[StaticConstructorOnStartup]
	public static class RimBattleButtonDefOf
	{
		static RimBattleButtonDefOf()
		{
			DefDatabase<MainButtonDef>.Add(Refs.Battle);
		}
	}

	public class MainButtonWorker_ToggleBattle : MainButtonWorker
	{
		public static void Toggle()
		{
			if (Find.MainTabsRoot.OpenTab != null)
				Find.MainTabsRoot.EscapeCurrentTab(false);

			var battleOverview = Refs.controller.BattleOverview;
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

		public override float ButtonBarPercent => Refs.controller.ProgressPercent;
	}
}