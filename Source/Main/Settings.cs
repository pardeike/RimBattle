using UnityEngine;
using Verse;

namespace RimBattle
{
	public class RimBattleModSettings : ModSettings
	{
		// static string buffer;

		public override void ExposeData()
		{
			base.ExposeData();
		}

#pragma warning disable CA1822 // Mark members as static
		public void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard { ColumnWidth = (inRect.width - 34f) / 2f };
			list.Begin(inRect);
			list.Gap(12f);

			list.End();
		}
#pragma warning restore CA1822 // Mark members as static
	}
}