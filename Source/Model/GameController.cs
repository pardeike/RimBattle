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

		public int mapSize;
		public List<int> tiles;

		public BattleOverview battleOverview;
		public List<MapPart> mapParts;
		public List<string> teamChoices = new List<string> { "", "", "", "", "", "", "" };

		public int team;
		public List<Team> teams = new List<Team>();

		public GameController(Game game) : base()
		{
			_ = game;
			team = 0;
			mapParts = new List<MapPart>();
			battleOverview = new BattleOverview();
		}

		public Team CreateTeam()
		{
			var t = new Team(teams.Count);
			teams.Add(t);
			return t;
		}

		public Team GetTeam(Pawn pawn)
		{
			var teamId = pawn.GetTeamID();
			if (teamId < 0) return null;
			return teams[teamId];
		}

		public void JoinTeam(int team)
		{
			this.team = team;
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

		public bool IsMyTeam(int team)
		{
			return this.team == team;
		}

		public Team CurrentTeam => teams[team];

		public void CreateMapPart(Map map)
		{
			mapSize = map.Size.x;
			mapParts.Add(map.GetComponent<MapPart>());
		}

		public void MultiplayerEstablished()
		{
			Multiplayer.IsUsingAsyncTime = MPTools.IsAsyncTime();
			GameState.ConnectPlayers();
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
			return Ref.adjactedTiles[idx1].Contains(idx2);
		}

		public bool CanReach(Map mapFrom, Map mapTo)
		{
			return CanReach(mapFrom.Tile, mapTo.Tile);
		}

		public bool InMyTeam(Pawn pawn)
		{
			return pawn != null && pawn.GetTeamID() == team;
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
			if (ToggleBattle.BattleMap?.KeyDownEvent ?? false)
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
			Scribe_Collections.Look(ref teamChoices, "teamChoices");

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
				return 1f - (float)minTickets / Ref.startTickets;
			}
		}
	}
}