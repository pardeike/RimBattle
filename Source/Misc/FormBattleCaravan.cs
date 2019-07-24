using RimWorld;
using System.Collections.Generic;
using Verse;

namespace RimBattle
{
	class FormBattleCaravan : LordJob_FormAndSendCaravan
	{
		public List<Pawn> pawns;
		public int startingTile;
		public int destinationTile;
		public IntVec3 exitSpot;

		public FormBattleCaravan() { }

		public FormBattleCaravan(List<TransferableOneWay> transferables, List<Pawn> downedPawns, IntVec3 meetingPoint, IntVec3 exitSpot, int startingTile, int destinationTile)
			: base(transferables, downedPawns, meetingPoint, exitSpot, startingTile, destinationTile)
		{
			this.startingTile = startingTile;
			this.destinationTile = destinationTile;
			this.exitSpot = exitSpot;
		}

		public void SetPawns(List<Pawn> pawns)
		{
			this.pawns = pawns;
		}
	}
}