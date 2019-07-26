using Multiplayer.API;
using System;
using System.Linq;
using Verse;

namespace RimBattle
{
	class Multiplayer
	{
		public static void Init()
		{
			if (MP.enabled)
				MP.RegisterAll();
			else
				Log.Error("RimBattle needs Multiplayer to be enabled!");
		}

		[SyncMethod]
		public static void SetSpeed(int team, int speed)
		{
			Ref.controller.teams[team].gameSpeed = speed;
			Log.Warning($"Team {team} wants speed {speed}");

			var tickManager = Find.TickManager;
			var teams = Ref.controller.teams;

			var minSpeed = Math.Max(1, teams.Min(t => t.gameSpeed));
			Log.Warning($"Min speed {minSpeed}");
			if (teams.All(t => t.gameSpeed == 0))
			{
				Log.Warning($"All are paused => speed 0");
				minSpeed = 0;
			}

			var currentSpeed = (int)tickManager.CurTimeSpeed;
			Log.Warning($"Speed change from {currentSpeed} to {minSpeed}");
			if (currentSpeed != minSpeed)
			{
				tickManager.CurTimeSpeed = (TimeSpeed)minSpeed;
				Log.Warning($"Speed now {tickManager.CurTimeSpeed}");
				Ref.PlaySoundOf(null, new object[] { tickManager.CurTimeSpeed });
			}
		}
	}
}