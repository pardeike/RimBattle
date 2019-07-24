using Verse;

namespace RimBattle
{
	public class MapPart : MapComponent
	{
		public Visibility visibility;

		public MapPart(Map map) : base(map)
		{
			visibility = new Visibility(map);
		}

		public string Name => Find.WorldObjects.SettlementAt(map.Tile).Name;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Deep.Look(ref visibility, "visibility", new object[] { map });
		}
	}
}