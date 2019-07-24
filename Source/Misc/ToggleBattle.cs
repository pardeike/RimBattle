using RimWorld;
using Verse;
using Verse.Sound;

namespace RimBattle
{
	public class ToggleBattle : MainButtonWorker
	{
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