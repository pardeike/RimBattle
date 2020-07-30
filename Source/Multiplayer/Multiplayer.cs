using Harmony;
using Multiplayer.API;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace RimBattle
{
	class Multiplayer
	{
		public static readonly MPEvents dispatcher = new MPEvents();
		public static readonly List<PlayerInfo> players = new List<PlayerInfo>();
		public static PlayerInfo CurrentPlayer = null;
		public static bool IsUsingAsyncTime;

		public static void Init()
		{
			if (MP.enabled == false)
			{
				Log.Error("RimBattle needs Multiplayer to be enabled!");
				return;
			}

			MP.RegisterAll();
			MPTools.Checks();
			MP.RegisterSyncWorker<TransferableOneWay>(SyncWorkers.TransferableOneWaySupport);

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

		public static bool IsReplay()
		{
			var type = AccessTools.TypeByName("Multiplayer.Client.Multiplayer");
			return Traverse.Create(type).Field("session").Field("replay").GetValue<bool>();
		}

		public static Window GetHostWindow()
		{
			var type = AccessTools.TypeByName("Multiplayer.Client.HostWindow");
			return Activator.CreateInstance(type, new object[] { null, false }) as Window;
		}

		public static void MultiplayerEstablished()
		{
			Multiplayer.IsUsingAsyncTime = MPTools.IsAsyncTime();
			GameState.ConnectPlayers();
		}
	}
}