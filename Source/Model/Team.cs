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

		public int previousSpeed = 1;
		public int gameSpeed = 0;

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
			members.Add(pawn);
		}

		public void Remove(Pawn pawn)
		{
			Ref.master(pawn.playerSettings) = null;
			members.Remove(pawn);
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref id, "id");
			Scribe_References.Look(ref master, "master");
			Scribe_Values.Look(ref name, "name");
			Scribe_Values.Look(ref ticketsLeft, "ticketsLeft");
			Scribe_Collections.Look(ref members, false, "members", LookMode.Reference);
			Scribe_Values.Look(ref previousSpeed, "previousSpeed");
			Scribe_Values.Look(ref gameSpeed, "gameSpeed");
		}

		public override string ToString()
		{
			return $"Team {name}";
		}
	}
}