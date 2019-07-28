using Harmony;
using Multiplayer.API;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimBattle
{
	class Multiplayer
	{
		public static readonly MPEvents dispatcher = new MPEvents();
		public static readonly List<PlayerInfo> players = new List<PlayerInfo>();
		public static PlayerInfo CurrentPlayer = null;

		public static void Init()
		{
			if (MP.enabled == false)
			{
				Log.Error("RimBattle needs Multiplayer to be enabled!");
				return;
			}

			MP.RegisterAll();
			MPTools.Checks();

			dispatcher.Subscribe(MPEventType.Connect, (info) =>
			{
				if (info.username == MP.PlayerName)
					CurrentPlayer = info;
			});
			dispatcher.Subscribe(MPEventType.Disconnect, (info) =>
			{
				if (info.username == MP.PlayerName)
					CurrentPlayer = null;
			});
		}

		public static bool IsArbiter()
		{
			var type = AccessTools.TypeByName("Multiplayer.Client.MultiplayerMod");
			return Traverse.Create(type).Field("arbiterInstance").GetValue<bool>();
		}

		public static void SetName(string name)
		{
			var type = AccessTools.TypeByName("Multiplayer.Client.Multiplayer");
			Traverse.Create(type).Field("username").SetValue(name);
		}

		public static Window GetHostWindow()
		{
			var type = AccessTools.TypeByName("Multiplayer.Client.HostWindow");
			return Activator.CreateInstance(type, new object[] { null, false }) as Window;
		}

		public static void SetSpeed(int team, TimeSpeed speed)
		{
			SetSpeed(team, (int)speed);
		}

		[SyncMethod]
		public static void SetSpeed(int team, int speed)
		{
			Ref.controller.teams[team].gameSpeed = speed;

			var teams = Ref.controller.teams;
			var minSpeed = Math.Max(1, teams.Min(t => t.gameSpeed));
			if (teams.All(t => t.gameSpeed == 0))
				minSpeed = 0;

			var timeSpeed = Ref.CachedTimeSpeedValues[minSpeed];
			var currentSpeed = MPTools.CurTimeSpeed;

			if (currentSpeed != timeSpeed)
			{
				MPTools.CurTimeSpeed = timeSpeed;
				Ref.PlaySoundOf(null, new object[] { MPTools.CurTimeSpeed });
			}
		}

		[SyncMethod]
		public static void JoinTeam(int team)
		{
			Ref.controller.team = team;
			if (CurrentPlayer != null)
				CurrentPlayer.teamID = team;
		}
	}
}