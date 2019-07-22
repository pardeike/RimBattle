using Harmony;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimBattle
{
	public class Team : IExposable
	{
		public int id;
		public string name;
		public Color color;
		public int ticketsLeft;
		public HashSet<Pawn> members;

		public Team()
		{
		}

		public Team(int id)
		{
			this.id = id;
			name = NameGenerator.GenerateName(Faction.OfPlayer.def.factionNameMaker);
			color = GenColor.RandomColorOpaque();
			ticketsLeft = Refs.startTickets;
			members = new HashSet<Pawn>();
		}

		public static void CreateWithColonists()
		{
			var existingTeams = Refs.controller.teams;
			var team = new Team(existingTeams.Count);
			Find.GameInitData.startingAndOptionalPawns.Do(pawn => team.Add(pawn));
			existingTeams.Add(team);
		}

		public void Add(Pawn pawn)
		{
			members.Add(pawn);
			Refs.teamMemberCache[pawn] = this;
		}

		public void Remove(Pawn pawn)
		{
			members.Remove(pawn);
			Refs.teamMemberCache.Remove(pawn);
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref id, "id");
			Scribe_Values.Look(ref name, "name");
			Scribe_Values.Look(ref color, "color");
			Scribe_Values.Look(ref ticketsLeft, "ticketsLeft");
			Scribe_Collections.Look(ref members, "members", LookMode.Reference);
		}

		public override string ToString()
		{
			return $"Team {name}";
		}
	}
}