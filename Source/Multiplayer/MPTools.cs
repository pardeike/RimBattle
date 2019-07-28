using Harmony;
using Multiplayer.API;
using System;
using System.Reflection;
using Verse;

namespace RimBattle
{
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

		public static MethodInfo Method(string typeName, string methodName)
		{
			var result = AccessTools.Method($"Multiplayer.{typeName}:{methodName}");
			if (result == null)
				Log.Error($"Cannot find {methodName} in type Multiplayer.{typeName}");
			return result;
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
					return;
				}
				Find.TickManager.CurTimeSpeed = value;
			}
		}
	}
}