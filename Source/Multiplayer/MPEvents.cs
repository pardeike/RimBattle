using System;
using System.Collections.Generic;

namespace RimBattle
{
	class MPEvents
	{
		readonly Dictionary<MPEventType, List<Action<PlayerInfo>>> events = new Dictionary<MPEventType, List<Action<PlayerInfo>>>();

		public MPEvents()
		{
			events[MPEventType.Connect] = new List<Action<PlayerInfo>>();
			events[MPEventType.Disconnect] = new List<Action<PlayerInfo>>();
		}

		public void Subscribe(MPEventType eventType, Action<PlayerInfo> callback)
		{
			events[eventType].Add(callback);
		}

		public void Unsubscribe(Action<PlayerInfo> callback)
		{
			events[MPEventType.Connect].Remove(callback);
			events[MPEventType.Disconnect].Remove(callback);
		}

		public void Send(MPEventType eventType, PlayerInfo info = null)
		{
			if (events.TryGetValue(eventType, out var subs))
				foreach (var sub in subs)
					sub(info);
		}
	}
}