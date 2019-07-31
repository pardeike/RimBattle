using Multiplayer.API;
using RimWorld;

namespace RimBattle
{
	class SyncWorkers
	{
		public static void TransferableOneWaySupport(SyncWorker sync, ref TransferableOneWay value)
		{
			value = value ?? new TransferableOneWay();
			sync.Bind(ref value.things);
			sync.Bind(value, "countToTransfer");
		}
	}
}