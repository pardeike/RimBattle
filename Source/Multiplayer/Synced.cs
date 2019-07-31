using Multiplayer.API;
using RimWorld;
using RimWorld.Planet;
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
			for (var i = 0; i < GameState.TeamChoices.Length; i++)
				if (GameState.TeamChoices[i] == player)
					GameState.TeamChoices[i] = "";
			if (joining)
				GameState.TeamChoices[team] = player;
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
	}
}