using RimWorld;
using System.Collections.Generic;
using Verse;

namespace RimBattle
{
	public class Team : IExposable
	{
		public int id;
		public Pawn master;
		public string name;
		public int ticketsLeft;
		public HashSet<Pawn> members;

		public int previousWorldSpeed = 1;
		public int worldSpeed = 0;

		public List<int> previousMapSpeeds = new List<int>() { 1, 1, 1, 1, 1, 1, 1 };
		public List<int> mapSpeeds = new List<int>() { 0, 0, 0, 0, 0, 0, 0 };

		public Team() { }

		public Team(int id)
		{
			this.id = id;
			master = Tools.CreateTeamMaster();
			name = NameGenerator.GenerateName(Faction.OfPlayer.def.factionNameMaker);
			ticketsLeft = Ref.startTickets;
			members = new HashSet<Pawn>();
		}

		public void Add(Pawn pawn)
		{
			Ref.master(pawn.playerSettings) = master;
			_ = members.Add(pawn);
		}

		public void Remove(Pawn pawn)
		{
			Ref.master(pawn.playerSettings) = null;
			_ = members.Remove(pawn);
		}

		public int GetSpeed(int tile, bool previous)
		{
			if (Multiplayer.IsUsingAsyncTime)
			{
				var i = Ref.controller.TileIndex(tile);
				return previous ? previousMapSpeeds[i] : mapSpeeds[i];
			}
			return previous ? previousWorldSpeed : worldSpeed;
		}

		public void SetSpeed(int tile, int speed)
		{
			if (Multiplayer.IsUsingAsyncTime)
			{
				var i = Ref.controller.TileIndex(tile);
				previousMapSpeeds[i] = mapSpeeds[i];
				mapSpeeds[i] = speed;
			}
			else
			{
				previousWorldSpeed = worldSpeed;
				worldSpeed = speed;
			}
			MPTools.SetSpeedSynced(id, tile, speed);
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref id, "id");
			Scribe_References.Look(ref master, "master");
			Scribe_Values.Look(ref name, "name");
			Scribe_Values.Look(ref ticketsLeft, "ticketsLeft");
			Scribe_Collections.Look(ref members, false, "members", LookMode.Reference);
			Scribe_Values.Look(ref previousWorldSpeed, "previousWorldSpeed");
			Scribe_Values.Look(ref worldSpeed, "worldSpeed");
			Scribe_Collections.Look(ref mapSpeeds, "mapSpeeds");
			Scribe_Collections.Look(ref previousMapSpeeds, "previousMapSpeeds");
		}

		public override string ToString()
		{
			return $"Team {name}";
		}
	}
}