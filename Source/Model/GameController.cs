using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimBattle
{
	class GameController : GameComponent
	{
		public int tileCount = 3;
		public int teamCount = 2;
		public int totalTickets = 20;
		public int maxQuadrums = 8; // 2 years

		public int mapSize;
		public List<int> tiles;

		public BattleOverview battleOverview;
		public List<MapPart> mapParts;

		public int team;
		public List<Team> teams = new List<Team>();

		public GameController(Game game) : base()
		{
			_ = game;
			team = 0; // TODO
			mapParts = new List<MapPart>();
			battleOverview = new BattleOverview();
		}

		public Team CreateTeam()
		{
			var t = new Team(teams.Count);
			teams.Add(t);
			return t;
		}

		public bool IsMyTeam(int team)
		{
			return this.team == team;
		}

		public void CreateMapPart(Map map)
		{
			mapSize = map.Size.x;
			mapParts.Add(map.GetComponent<MapPart>());
		}

		public override void GameComponentTick()
		{
			battleOverview.Update();
		}

		public int TileIndex(int tile)
		{
			var idx = tiles.IndexOf(tile);
			return Tools.TilePattern()[idx];
		}

		public Map MapByIndex(int n)
		{
			return Tools.MapForTile(tiles[n]);
		}

		public bool CanReach(int tileFrom, int tileTo)
		{
			var idx1 = TileIndex(tileFrom);
			var idx2 = TileIndex(tileTo);
			return Statics.adjactedTiles[idx1].Contains(idx2);
		}

		public bool CanReach(Map mapFrom, Map mapTo)
		{
			return CanReach(mapFrom.Tile, mapTo.Tile);
		}

		public bool InMyTeam(Pawn pawn)
		{
			return pawn.GetTeamID() == team;
		}

		public IEnumerable<Pawn> MyColonistsOn(Map map, bool includeTameAnimals = true)
		{
			foreach (var pawn in teams[team].members.Where(pawn => pawn.Map == map))
				yield return pawn;
			if (includeTameAnimals == false)
				yield break;

			foreach (var pawn in map.mapPawns.PawnsInFaction(Faction.OfPlayer)
				.Where(pawn => pawn.RaceProps.Animal)
				.Where(Ref.controller.InMyTeam))
			{
				yield return pawn;
			}
		}

		public bool IsInVisibleRange(Pawn pawn)
		{
			if (InMyTeam(pawn)) return true;
			var pos = pawn.Position;
			return MyColonistsOn(pawn.Map)
				.Any(myColonist => myColonist.Position.DistanceToSquared(pos) <= myColonist.WeaponRange(true));
		}

		public void OnGUI()
		{
			if (Defs.BattleMap.KeyDownEvent)
			{
				Event.current.Use();
				ToggleBattle.Toggle();
			}
			battleOverview.OnGUI();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref tileCount, "tileCount");
			Scribe_Values.Look(ref teamCount, "teamCount");
			Scribe_Values.Look(ref totalTickets, "totalTickets");
			Scribe_Values.Look(ref maxQuadrums, "maxQuadrums");
			Scribe_Collections.Look(ref tiles, "tiles");
			Scribe_Collections.Look(ref teams, "teams", LookMode.Deep);

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				Ref.controller = Current.Game.GetComponent<GameController>();
				foreach (var map in Current.Game.Maps)
					CreateMapPart(map);
			}
		}

		public float ProgressPercent
		{
			get
			{
				if (teams.Count == 0) return 0f;
				var minTickets = teams.Select(team => team.ticketsLeft).Min();
				return 1f - (float)minTickets / Statics.startTickets;
			}
		}
	}
}