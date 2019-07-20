using RimWorld.Planet;
using Steamworks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimBattle
{
	public class GameController : WorldComponent
	{
		public static int tileCount = 3;
		public static int teamCount = 2;
		public static int totalTickets = 20;
		public static int maxQuadrums = 8; // 2 years

		public List<int> tiles;

		public BattleOverview battleOverview;
		public Dictionary<Map, MapPart> mapParts;

		public int team;
		public List<Team> teams = new List<Team>();

		private List<Map> tmpMaps;
		private List<MapPart> tmpMapParts;

		public GameController(World world) : base(world)
		{
			team = 0; // TODO
			mapParts = new Dictionary<Map, MapPart>();
		}

		public static int[] TilePattern => Refs.teamTiles[tileCount - 1][tileCount - 2];

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

		public Team TeamForPawn(Pawn pawn)
		{
			return teams.FirstOrDefault(team => team.members.Contains(pawn));
		}

		public bool IsMyColonist(Pawn pawn)
		{
			return teams[team].members.Contains(pawn);
		}

		public IEnumerable<Pawn> MyColonistsOn(Map map)
		{
			return teams[team].members.Where(pawn => pawn.Map == map);
		}

		public bool IsVisible(Map map, IntVec3 loc)
		{
			if (map == null) return false;
			if (Refs.controller.mapParts.TryGetValue(map, out var mapPart))
				return mapPart.visibility.IsVisible(loc);
			return false;
		}

		public bool IsVisible(Thing pawn)
		{
			return IsVisible(pawn.Map, pawn.Position);
		}

		public bool IsInWeaponRange(Pawn pawn)
		{
			if (IsMyColonist(pawn)) return true;
			return MyColonistsOn(pawn.Map)
				.Any(myColonist => myColonist.Position.DistanceToSquared(pawn.Position) <= myColonist.WeaponRange(true));
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
			// TODO: Scribe_Values.Look(ref team, "team");
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