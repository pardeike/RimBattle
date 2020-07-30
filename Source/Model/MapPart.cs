using Verse;

namespace RimBattle
{
	public class MapPart : MapComponent
	{
		// more here later

		public MapPart(Map map) : base(map)
		{
		}

		public string Name => Find.WorldObjects.SettlementAt(map.Tile).Name;

		public override void ExposeData()
		{
			base.ExposeData();
		}
	}
}