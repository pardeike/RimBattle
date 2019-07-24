using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimBattle
{
	public class Team : IExposable
	{
		public int id;
		public Pawn master;
		public string name;
		public Color color;
		public int ticketsLeft;
		public HashSet<Pawn> members;

		public Team() { }

		public Team(int id)
		{
			this.id = id;
			master = Tools.CreateTeamMaster();
			name = NameGenerator.GenerateName(Faction.OfPlayer.def.factionNameMaker);
			color = GenColor.RandomColorOpaque();
			ticketsLeft = Statics.startTickets;
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
			Scribe_Values.Look(ref color, "color");
			Scribe_Values.Look(ref ticketsLeft, "ticketsLeft");
			Scribe_Collections.Look(ref members, false, "members", LookMode.Reference);
		}

		public override string ToString()
		{
			return $"Team {name}";
		}
	}
}