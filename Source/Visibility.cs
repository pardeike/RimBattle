using Harmony;
using Verse;

namespace RimBattle
{
	public class Visibility : IExposable
	{
		private Map map;
		public bool[] visible;

		public Visibility()
		{
		}

		public Visibility(Map map)
		{
			this.map = map;
			visible = new bool[map.cellIndices.NumGridCells];
		}

		public void ExposeData()
		{
			Scribe_References.Look(ref map, "map");
			var count = visible?.Length ?? 0;
			Scribe_Values.Look(ref count, "count");
			DataExposeUtility.BoolArray(ref visible, count, "visible");
		}

		public void MakeVisible(IntVec3 cell)
		{
			FileLog.LogBuffered("set " + cell.x + "," + cell.z + " 1");

			visible[map.cellIndices.CellToIndex(cell)] = true;
			map.mapDrawer.MapMeshDirty(cell, MapMeshFlag.FogOfWar);
		}

		public bool IsVisible(int idx)
		{
			var cell = map.cellIndices.IndexToCell(idx);
			FileLog.LogBuffered("get " + cell.x + "," + cell.z + (visible[idx] ? " 1" : " 0"));

			return visible[idx];
		}

		public bool IsVisible(IntVec3 cell)
		{
			return visible[map.cellIndices.CellToIndex(cell)];
		}
	}
}