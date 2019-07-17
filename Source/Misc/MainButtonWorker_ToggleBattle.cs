using RimWorld;
using Verse;
using Verse.Sound;

namespace RimBattle
{
	/* new Dialog_FormCaravan(Find.CurrentMap, false, null, false) */

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
		static BattleOverview window;

		public override void Activate()
		{
			if (Find.MainTabsRoot.OpenTab != null)
				Find.MainTabsRoot.EscapeCurrentTab(false);
			if (Find.WindowStack.IsOpen<BattleOverview>())
			{
				Find.WindowStack.TryRemove(window);
				SoundDefOf.TabClose.PlayOneShotOnCamera(null);
				return;
			}
			window = new BattleOverview();
			Find.WindowStack.Add(window);
			SoundDefOf.TabOpen.PlayOneShotOnCamera(null);
		}

		public override float ButtonBarPercent => Refs.controller.ProgressPercent;
	}
}