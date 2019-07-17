using UnityEngine;
using Verse;

namespace RimBattle
{
	public class RimBattleModSettings : ModSettings
	{
		public bool foo = true;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref foo, "foo", false);
		}

		public void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard { ColumnWidth = (inRect.width - 34f) / 2f };
			list.Begin(inRect);
			list.Gap(12f);
			list.CheckboxLabeled("Foo", ref foo);
			list.End();
		}
	}
}