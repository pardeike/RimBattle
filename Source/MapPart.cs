using Verse;

namespace RimBattle
{
	public class MapPart : IExposable
	{
		public Map map;
		public Visibility visibility;

		public MapPart()
		{
		}

		public MapPart(Map map)
		{
			this.map = map;
			visibility = new Visibility(map);
		}

		public string Name => Find.WorldObjects.SettlementAt(map.Tile).Name;

		public void ExposeData()
		{
			Scribe_References.Look(ref map, "map");
			Scribe_Deep.Look(ref visibility, "visibility");
		}
	}
}