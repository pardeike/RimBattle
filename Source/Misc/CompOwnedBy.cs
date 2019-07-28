using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimBattle
{
	public class CompOwnedBy : ThingComp
	{
		public int team = -1;

		public CompOwnedBy()
		{
			team = -1;
		}

		public CompOwnedBy(int team)
		{
			this.team = team;
		}

		public static void SetTeam(ThingWithComps compThing, int team)
		{
			if (compThing == null || team < 0)
				return;

			var ownedBy = compThing.GetComp<CompOwnedBy>();
			if (ownedBy == null)
			{
				var comps = Ref.comps(compThing);
				if (comps == null)
				{
					comps = new List<ThingComp>();
					Ref.comps(compThing) = comps;
				}
				ownedBy = new CompOwnedBy(team) { parent = compThing };
				comps.Add(ownedBy);
			}
			ownedBy.team = team;
		}

		public static void SetTeam(ThingWithComps compThing, Pawn pawn)
		{
			if (pawn == null)
				return;

			var team = pawn.GetTeamID();
			if (team < 0)
				return;

			SetTeam(compThing, team);
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref team, "team", -1, false);
		}

		public override void PostDraw()
		{
			if (parent == null || Ref.controller.team == team || team < 0)
				return;

			var matrix = default(Matrix4x4);
			matrix.SetTRS(parent.DrawPos, Quaternion.identity, Vector3.one);
			Graphics.DrawMesh(MeshPool.plane10, matrix, Statics.OwnedByShadow, 0);
			Graphics.DrawMesh(MeshPool.plane10, matrix, Statics.OwnedBy[team], 0);
		}

		public override void PostSplitOff(Thing piece)
		{
			SetTeam(piece as ThingWithComps, team);
		}

		public override string CompInspectStringExtra()
		{
			return ToString() + "\n";
		}

		public override string GetDescriptionPart()
		{
			return ToString();
		}

		public override string ToString()
		{
			return team >= 0 ? $"Build by team #{team + 1}" : "";
		}
	}
}