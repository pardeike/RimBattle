using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimBattle
{
	public class GameController : WorldComponent
	{
		public static int tileCount = 5;
		public static int teamCount = 3;
		public static int totalTickets = 20;
		public static int maxQuadrums = 8; // 2 years

		public List<int> tiles;

		public int myTeamID;
		public BattleOverview battleOverview;
		public Dictionary<Map, MapPart> mapParts;
		public List<Team> teams = new List<Team>();

		private List<Map> tmpMaps;
		private List<MapPart> tmpMapParts;

		public GameController(World world) : base(world)
		{
			myTeamID = 1; // TODO
			mapParts = new Dictionary<Map, MapPart>();
		}

		public static int[] TilePattern => Refs.teamTiles[tileCount - 2][tileCount - 2];

		public void CreateMapPart(Map map)
		{
			mapParts[map] = new MapPart(map);
		}

		public BattleOverview BattleOverview
		{
			get
			{
				if (battleOverview == null)
					battleOverview = new BattleOverview();
				return battleOverview;
			}
		}

		public override void WorldComponentTick()
		{
			BattleOverview.Update();
		}

		public Map MapForTile(int tile)
		{
			return mapParts
				.Select(pair => pair.Value.map)
				.FirstOrDefault(map => map.Tile == tile);
		}

		public Map MapByIndex(int n)
		{
			return MapForTile(tiles[n]);
		}

		public void OnGUI()
		{
			if (Keys.BattleMap.KeyDownEvent)
			{
				Event.current.Use();
				MainButtonWorker_ToggleBattle.Toggle();
			}

			BattleOverview.OnGUI();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref tiles, "tiles");
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