using System;
using Verse;

namespace RimBattle
{
	public class Visibility : IExposable
	{
		private readonly Map map;

		private string id;
		public byte[] visible;

		public Visibility(Map map)
		{
			this.map = map;
			id = Guid.NewGuid().ToString();
			visible = new byte[map.Size.x * map.Size.z];
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref id, "id");

			if (Scribe.mode == LoadSaveMode.Saving)
				Tools.SaveFile(id.ToString(), visible);

			if (Scribe.mode == LoadSaveMode.LoadingVars)
				visible = Tools.LoadFile(id.ToString(), () => new byte[map.Size.x * map.Size.z]);
		}

		public void MakeVisible(int x, int z)
		{
			visible[z * map.Size.x + x] = 1;
		}

		public bool IsVisible(int idx)
		{
			return visible[idx] != 0;
		}

		public bool IsVisible(IntVec3 cell)
		{
			return visible[cell.z * map.Size.x + cell.x] != 0;
		}
	}
}