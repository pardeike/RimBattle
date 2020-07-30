using Multiplayer.API;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;

namespace RimBattle
{
	class Synced
	{
		[SyncMethod]
		public static void SetSpeed(int team, int tile, int speed)
		{
			var tIndex = Ref.controller.TileIndex(tile);
			if (Multiplayer.IsUsingAsyncTime)
				Ref.controller.teams[team].mapSpeeds[tIndex] = speed;
			else
				Ref.controller.teams[team].worldSpeed = speed;

			var teams = Ref.controller.teams;
			var minSpeed = 4;
			var allPause = true;
			for (var i = 0; i < teams.Count; i++)
			{
				var n = Multiplayer.IsUsingAsyncTime ? teams[i].mapSpeeds[tIndex] : teams[i].worldSpeed;
				if (n < minSpeed)
					minSpeed = n == 0 ? 1 : n;
				if (n > 0)
					allPause = false;
			}
			if (allPause)
				minSpeed = 0;

			var timeSpeed = Ref.CachedTimeSpeedValues[minSpeed];
			MPTools.SetCurrentSpeed(tile, timeSpeed);
		}

		[SyncMethod]
		public static void SetPlayerTeam(string player, int team, bool joining)
		{
			for (var i = 0; i < Ref.controller.teamChoices.Count; i++)
				if (Ref.controller.teamChoices[i] == player)
					Ref.controller.teamChoices[i] = "";
			if (joining)
				Ref.controller.teamChoices[team] = player;
		}

		[SyncMethod]
		public static void StartGame()
		{
			PlayerConnectDialog.startGame = true;
		}

		[SyncMethod]
		public static void StartFormingCaravan(List<Pawn> pawns, List<Pawn> downedPawns, List<TransferableOneWay> transferables, IntVec3 meetingPoint, IntVec3 exitSpot, int startingTile, int destinationTile)
		{
			CaravanFormingUtility.StartFormingCaravan(pawns, downedPawns, Faction.OfPlayer, transferables, meetingPoint, exitSpot, startingTile, destinationTile);
		}

		[SyncMethod]
		public static void UpdateVisibility(int team, Map map, int x, int z, float weaponRange)
		{
			var visibility = map.GetComponent<Visibility>();
			map.DoInCircle(new IntVec3(x, 0, z), weaponRange, (px, pz) => visibility.MakeVisible(team, px, pz));
			var radius = (int)Math.Ceiling(weaponRange);
			map.MapMeshDirtyRect(x - radius, z - radius, x + radius, z + radius);
		}
	}
}