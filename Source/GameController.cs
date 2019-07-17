using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimBattle
{
	public class GameController : WorldComponent
	{
		public int[] startingTiles; // temporary

		public int myTeamID;
		public Dictionary<Map, MapPart> mapParts;
		public List<Team> teams = new List<Team>();

		private List<Map> tmpMaps;
		private List<MapPart> tmpMapParts;

		public GameController(World world) : base(world)
		{
			myTeamID = 1; // TODO
			mapParts = new Dictionary<Map, MapPart>();
		}

		public void CreateMapPart(Map map)
		{
			mapParts[map] = new MapPart(map);
		}

		public Map MapForTile(int tile)
		{
			return mapParts
				.Select(pair => pair.Value.map)
				.FirstOrDefault(map => map.Tile == tile);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref myTeamID, "myTeamID");
			Scribe_Collections.Look(ref mapParts, "mapParts", LookMode.Reference, LookMode.Deep, ref tmpMaps, ref tmpMapParts);
			Scribe_Collections.Look(ref teams, "teams", LookMode.Deep);
		}

		public float ProgressPercent
		{
			get
			{
				if (teams.Count == 0) return 0f;
				var minTickets = teams.Select(team => team.ticketsLeft).Min();
				return 1f - (float)minTickets / Refs.startTickets;
			}
		}
	}
}