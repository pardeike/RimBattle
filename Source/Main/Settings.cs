using UnityEngine;
using Verse;

namespace RimBattle
{
	public class RimBattleModSettings : ModSettings
	{
		static string buffer;

		public override void ExposeData()
		{
			base.ExposeData();

			// for testing switching teams
			var team = Ref.controller?.team ?? 0;
			Scribe_Values.Look(ref team, "team", 0);
			if (Ref.controller != null)
				Ref.controller.team = team;
		}

		public void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard { ColumnWidth = (inRect.width - 34f) / 2f };
			list.Begin(inRect);
			list.Gap(12f);

			// for testing switching teams
			var oldValue = Ref.controller.team;
			list.TextFieldNumericLabeled("Team", ref Ref.controller.team, ref buffer);
			if (Ref.controller.team != oldValue)
				Find.ColonistBar.MarkColonistsDirty();

			list.End();
		}
	}
}