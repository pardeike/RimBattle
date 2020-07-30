using RimWorld;
using RimWorld.Planet;
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
		public List<int> tiles;
		public List<Team> teams = new List<Team>();

		// transient properties
		public int Team { get; set; } = 0;
		public BattleOverview battleOverview = new BattleOverview();
		public List<MapPart> mapPartRefs = new List<MapPart>();
		public List<string> teamChoices = new List<string> { "", "", "", "", "", "", "" };

		public GameController(Game game) : base()
		{
			_ = game;
		}

		public override void GameComponentTick()
		{
			battleOverview.Update();
		}

		public void OnGUI()
		{
			if (ToggleBattle.BattleMap?.KeyDownEvent ?? false)
			{
				Event.current.Use();
				ToggleBattle.Toggle();
			}
			battleOverview.OnGUI();
		}

		public Team CreateTeam()
		{
			var t = new Team(teams.Count);
			teams.Add(t);
			return t;
		}

		public void JoinTeam(int team)
		{
			Team = team;
			Multiplayer.CurrentPlayer.teamID = team;

			// TODO: find a way to remove this ridiculous loop
			//
			var allTileIndices = Tools.TeamTiles(tileCount, tileCount);
			var teamTileIndices = Tools.TeamTiles(tileCount, teamCount);
			var j = 0;
			for (var i = 0; i < tiles.Count; i++)
			{
				var tile = tiles[i];
				var tileIndex = allTileIndices[i];
				var hasTeam = teamTileIndices.Contains(tileIndex);
				if (hasTeam)
				{
					if (team == j)
					{
						var map = Tools.MapForTile(tile);
						CameraJumper.TryJump(new GlobalTargetInfo(map.Center, map, false));
						break;
					}
					j++;
				}
			}
		}

		public Team CurrentTeam => teams[Team];

		public bool IsCurrentTeam(int team)
		{
			return Team == team;
		}

		public bool InMyTeam(Pawn pawn)
		{
			return pawn != null && pawn.GetTeamID() == Team;
		}

		public void CreateMapPart(Map map)
		{
			mapPartRefs.Add(map.GetComponent<MapPart>());
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
			return Ref.adjactedTiles[idx1].Contains(idx2);
		}

		public bool CanReach(Map mapFrom, Map mapTo)
		{
			return CanReach(mapFrom.Tile, mapTo.Tile);
		}

		public IEnumerable<Pawn> MyColonistsOn(Map map, bool includeTameAnimals = true)
		{
			foreach (var pawn in teams[Team].members.Where(pawn => pawn.Map == map))
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
			if (Ref.controller.battleOverview.showing) return false;
			if (InMyTeam(pawn)) return true;
			var pos = pawn.Position;
			return MyColonistsOn(pawn.Map)
				.Any(myColonist => myColonist.Position.DistanceToSquared(pos) <= myColonist.WeaponRange(true));
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
			Scribe_Collections.Look(ref teamChoices, "teamChoices");

			if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
			{
				Ref.controller = Current.Game.GetComponent<GameController>();
				mapPartRefs = Current.Game.Maps.Select(map => map.GetComponent<MapPart>()).ToList();
			}
		}

		public float ProgressPercent
		{
			get
			{
				if (teams.Count == 0) return 0f;
				var minTickets = teams.Select(team => team.ticketsLeft).Min();
				return 1f - (float)minTickets / Ref.startTickets;
			}
		}
	}
}