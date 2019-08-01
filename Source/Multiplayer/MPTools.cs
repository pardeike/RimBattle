using Harmony;
using Multiplayer.API;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace RimBattle
{
	class MPTools
	{
		// API support and wrappers

		static readonly Type MP_Multiplayer = AccessTools.TypeByName("Multiplayer.Client.Multiplayer");
		static readonly Type MP_MultiplayerWorldComp = AccessTools.TypeByName("Multiplayer.Client.MultiplayerWorldComp");
		static readonly PropertyInfo p_WorldComp = AccessTools.Property(MP_Multiplayer, "WorldComp");
		static readonly FieldInfo f_asyncTime = AccessTools.Field(MP_MultiplayerWorldComp, "asyncTime");
		static readonly MethodInfo m_worldComp_get_TimeSpeed = AccessTools.Method(MP_MultiplayerWorldComp, "get_TimeSpeed");

		static readonly Type MP_Extensions = AccessTools.TypeByName("Multiplayer.Client.Extensions");
		static readonly MethodInfo m_AsyncTime = AccessTools.Method(MP_Extensions, "AsyncTime");

		static readonly Type MP_MapAsyncTimeComp = AccessTools.TypeByName("Multiplayer.Client.MapAsyncTimeComp");
		static readonly MethodInfo m_mapAsyncTimeComp_get_TimeSpeed = AccessTools.Method(MP_MapAsyncTimeComp, "get_TimeSpeed");
		static readonly MethodInfo m_mapAsyncTimeComp_set_TimeSpeed = AccessTools.Method(MP_MapAsyncTimeComp, "set_TimeSpeed");

		static readonly Type MP_TimeControl = AccessTools.TypeByName("Multiplayer.Client.TimeControl");
		static readonly MethodInfo m_SendTimeChange = AccessTools.Method(MP_TimeControl, "SendTimeChange");

		static readonly Type MP_OnMainThread = AccessTools.TypeByName("Multiplayer.Client.OnMainThread");
		static readonly MethodInfo m_StopMultiplayer = AccessTools.Method(MP_OnMainThread, "StopMultiplayer");

		static readonly Type MP_Sync = AccessTools.TypeByName("Multiplayer.Client.Sync");
		static readonly Dictionary<Type, MethodInfo> m_ReadSync = new Dictionary<Type, MethodInfo>()
		{
			{ typeof(int), AccessTools.Method(MP_Sync, "ReadSync").MakeGenericMethod(typeof(int)) }
		};
		static readonly Dictionary<Type, MethodInfo> m_WriteSync = new Dictionary<Type, MethodInfo>()
		{
			{ typeof(int), AccessTools.Method(MP_Sync, "WriteSync").MakeGenericMethod(typeof(int)) }
		};

		public static void Checks()
		{
			"Type Multiplayer.Client.Multiplayer".NullCheck(MP_Multiplayer);
			"Type Multiplayer.Client.MultiplayerWorldComp".NullCheck(MP_MultiplayerWorldComp);
			"Property Multiplayer.Client.Multiplayer.WorldComp".NullCheck(p_WorldComp);
			"Field Multiplayer.Client.MultiplayerWorldComp.asyncTime".NullCheck(f_asyncTime);
			"Method Multiplayer.Client.MultiplayerWorldComp.get_TimeSpeed".NullCheck(m_worldComp_get_TimeSpeed);

			"Type Multiplayer.Client.Extensions".NullCheck(MP_Extensions);
			"Method Multiplayer.Client.Extensions.AsyncTime".NullCheck(m_AsyncTime);

			"Type Multiplayer.Client.MapAsyncTimeComp".NullCheck(MP_MapAsyncTimeComp);
			"Method Multiplayer.Client.MapAsyncTimeComp.get_TimeSpeed".NullCheck(m_mapAsyncTimeComp_get_TimeSpeed);
			"Method Multiplayer.Client.MapAsyncTimeComp.set_TimeSpeed".NullCheck(m_mapAsyncTimeComp_set_TimeSpeed);

			"Type Multiplayer.Client.TimeControl".NullCheck(MP_TimeControl);
			"Method Multiplayer.Client.TimeControl.SendTimeChange".NullCheck(m_SendTimeChange);

			"Type Multiplayer.Client.OnMainThread".NullCheck(MP_OnMainThread);
			"Method Multiplayer.Client.OnMainThread.StopMultiplayer".NullCheck(m_StopMultiplayer);

			"Type Multiplayer.Client.Sync".NullCheck(MP_Sync);
			foreach (var type in m_ReadSync.Keys)
				$"Method Multiplayer.Client.Sync.ReadSync<{type}>()".NullCheck(m_ReadSync[type]);
			foreach (var type in m_WriteSync.Keys)
				$"Method Multiplayer.Client.Sync.WriteSync<{type}>()".NullCheck(m_WriteSync[type]);
		}

		public static MethodInfo Method(string typeName, string methodName)
		{
			var result = AccessTools.Method($"Multiplayer.{typeName}:{methodName}");
			if (result == null)
				Log.Error($"Cannot find {methodName} in type Multiplayer.{typeName}");
			return result;
		}

		public static void SetName(string name)
		{
			_ = Traverse.Create(MP_Multiplayer).Field("username").SetValue(name);
		}

		public static bool IsAsyncTime()
		{
			var worldComp = p_WorldComp.GetValue(null, new object[0]);
			return (bool)f_asyncTime.GetValue(worldComp);
		}

		public static TimeSpeed GetCurrentSpeed()
		{
			return Find.TickManager.CurTimeSpeed;
		}

		static void SetTileSpeed(int tile, TimeSpeed newSpeed)
		{
			var map = Tools.MapForTile(tile);
			var mapAsyncTimeComp = m_AsyncTime.Invoke(null, new object[] { map });
			var currentSpeed = (TimeSpeed)m_mapAsyncTimeComp_get_TimeSpeed.Invoke(mapAsyncTimeComp, new object[0]);
			if (newSpeed != currentSpeed)
			{
				_ = m_SendTimeChange.Invoke(null, new object[] { mapAsyncTimeComp, newSpeed });
				_ = Ref.PlaySoundOf(null, new object[] { newSpeed });
			}
		}

		static void SetWorldSpeed(TimeSpeed newSpeed)
		{
			var worldComp = p_WorldComp.GetValue(null, new object[0]);
			var currentSpeed = (TimeSpeed)m_worldComp_get_TimeSpeed.Invoke(worldComp, new object[0]);
			if (newSpeed != currentSpeed)
			{
				_ = m_SendTimeChange.Invoke(null, new object[] { worldComp, newSpeed });
				_ = Ref.PlaySoundOf(null, new object[] { newSpeed });
			}
		}

		public static void SetCurrentSpeed(int tile, TimeSpeed newSpeed)
		{
			if (MP.IsInMultiplayer)
			{
				if (Multiplayer.IsUsingAsyncTime)
					SetTileSpeed(tile, newSpeed);
				else
					SetWorldSpeed(newSpeed);
				return;
			}
			Find.TickManager.CurTimeSpeed = newSpeed;
		}

		public static void Stop()
		{
			_ = m_StopMultiplayer.Invoke(null, new object[0]);
		}

		public static void StartFormingCaravan(List<Pawn> pawns, List<Pawn> downedPawns, Faction faction, List<TransferableOneWay> transferables, IntVec3 meetingPoint, IntVec3 exitSpot, int startingTile, int destinationTile)
		{
			_ = faction;
			pawns = pawns ?? new List<Pawn>();
			downedPawns = downedPawns ?? new List<Pawn>();
			transferables = transferables ?? new List<TransferableOneWay>();
			Synced.StartFormingCaravan(pawns, downedPawns, transferables, meetingPoint, exitSpot, startingTile, destinationTile);
		}

		public static T SyncRead<T>(object byteReader)
		{
			Log.Warning($"SyncRead byteWrite: {byteReader.GetType().FullName}, value: {typeof(T).FullName}");
			return (T)m_ReadSync[typeof(T)].Invoke(null, new object[] { byteReader });
		}

		public static void SyncWrite<T>(object byteWriter, T value)
		{
			Log.Warning($"SyncWrite byteWrite: {byteWriter.GetType().FullName}, T: {typeof(T).FullName}, value: {value.GetType().FullName}");
			_ = m_WriteSync[typeof(T)].Invoke(null, new object[] { byteWriter, value });
		}
	}
}