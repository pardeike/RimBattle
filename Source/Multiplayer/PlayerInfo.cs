using Harmony;
using System;
using System.Linq;

namespace RimBattle
{
	public class PlayerInfo
	{
		// their stuff
		public int id;
		public string username;
		public MPPlayerType type;
		public MPPlayerStatus status;
		public ulong steamId;
		public string steamPersonaName;

		// our stuff
		public int teamID = -1;

		public PlayerInfo(object obj)
		{
			var myFields = AccessTools.GetDeclaredFields(typeof(PlayerInfo)).Select(f => f.Name).ToHashSet();
			Traverse.IterateFields(obj, this, (name, from, to) =>
			{
				if (myFields.Contains(name))
				{
					var value = from.GetValue();
					if (from.GetValueType().IsEnum)
						value = Enum.ToObject(to.GetValueType(), (byte)value);
					to.SetValue(value);
				}
			});
		}
	}
}