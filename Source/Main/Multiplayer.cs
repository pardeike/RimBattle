using Harmony;
using Multiplayer.API;
using System;
using System.Linq;
using System.Reflection;
using Verse;

namespace RimBattle
{
	class Multiplayer
	{
		public static void Init()
		{
			if (MP.enabled == false)
			{
				Log.Error("RimBattle needs Multiplayer to be enabled!");
				return;
			}

			MP.RegisterAll();
			MPTools.Checks();
		}

		[SyncMethod]
		public static void SetSpeed(int team, int speed)
		{
			Ref.controller.teams[team].gameSpeed = speed;
			Log.Warning($"Team {team} wants speed {speed}");

			var teams = Ref.controller.teams;
			var minSpeed = Math.Max(1, teams.Min(t => t.gameSpeed));
			Log.Warning($"Min speed {minSpeed}");
			if (teams.All(t => t.gameSpeed == 0))
			{
				Log.Warning($"All are paused => speed 0");
				minSpeed = 0;
			}

			var timeSpeed = Ref.CachedTimeSpeedValues[minSpeed];
			var currentSpeed = MPTools.CurTimeSpeed;

			Log.Warning($"Speed change from {currentSpeed} to {timeSpeed}");
			if (currentSpeed != timeSpeed)
			{
				MPTools.CurTimeSpeed = timeSpeed;
				Log.Warning($"Speed now {MPTools.CurTimeSpeed}");
				Ref.PlaySoundOf(null, new object[] { MPTools.CurTimeSpeed });
			}
		}
	}

	class MPTools
	{
		// API support and wrappers

		static readonly Type MP_Extensions = AccessTools.TypeByName("Multiplayer.Client.Extensions");
		static readonly MethodInfo m_AsyncTime = AccessTools.Method(MP_Extensions, "AsyncTime");

		static readonly Type MP_MapAsyncTimeComp = AccessTools.TypeByName("Multiplayer.Client.MapAsyncTimeComp");
		static readonly MethodInfo m_get_TimeSpeed = AccessTools.Method(MP_MapAsyncTimeComp, "get_TimeSpeed");
		static readonly MethodInfo m_set_TimeSpeed = AccessTools.Method(MP_MapAsyncTimeComp, "set_TimeSpeed");

		public static void Checks()
		{
			"Type Multiplayer.Client.Extensions".NullCheck(MP_Extensions);
			"Method Multiplayer.Client.Extensions.AsyncTime".NullCheck(m_AsyncTime);

			"Type Multiplayer.Client.MapAsyncTimeComp".NullCheck(MP_MapAsyncTimeComp);
			"Method Multiplayer.Client.MapAsyncTimeComp.get_TimeSpeed".NullCheck(m_get_TimeSpeed);
			"Method Multiplayer.Client.MapAsyncTimeComp.set_TimeSpeed".NullCheck(m_set_TimeSpeed);
		}

		public static TimeSpeed CurTimeSpeed
		{
			get
			{
				if (MP.IsInMultiplayer)
				{
					var mapAsyncTimeComp = m_AsyncTime.Invoke(null, new object[] { Find.CurrentMap });
					return (TimeSpeed)m_get_TimeSpeed.Invoke(mapAsyncTimeComp, new object[0]);
				}
				return Find.TickManager.CurTimeSpeed;
			}
			set
			{
				if (MP.IsInMultiplayer)
				{
					var mapAsyncTimeComp = m_AsyncTime.Invoke(null, new object[] { Find.CurrentMap });
					m_set_TimeSpeed.Invoke(mapAsyncTimeComp, new object[] { value });
				}
				Find.TickManager.CurTimeSpeed = value;
			}
		}
	}
}