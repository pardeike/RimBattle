using System.Collections.Generic;
using Verse;

namespace RimBattle
{
	public class Visibility : MapComponent
	{
		private List<bool[]> visibleCells;

		public Visibility(Map map) : base(map)
		{
			var cellCount = map.Size.x * map.Size.z;
			visibleCells = new List<bool[]>();
			var controller = Current.Game.GetComponent<GameController>();
			for (var team = 0; team < controller.teamCount; team++)
			{
				var cells = new bool[cellCount];
				if (Flags.unfogEverything)
					for (var i = 0; i < cellCount; i++)
						cells[i] = true;
				visibleCells.Add(cells);
			}
		}

		public bool[] GetCells(int team)
		{
			return visibleCells[team];
		}

		public override void ExposeData()
		{
			base.ExposeData();

			if (Scribe.mode == LoadSaveMode.LoadingVars)
				visibleCells = new List<bool[]>();

			var controller = Current.Game.GetComponent<GameController>();
			for (var team = 0; team < controller.teamCount; team++)
			{
				if (Scribe.mode == LoadSaveMode.Saving)
				{
					var cells = visibleCells[team];
					DataExposeUtility.BoolArray(ref cells, map.Size.x * map.Size.z, $"cells{team}");
				}
				if (Scribe.mode == LoadSaveMode.LoadingVars)
				{
					bool[] cells = null;
					DataExposeUtility.BoolArray(ref cells, map.Size.x * map.Size.z, $"cells{team}");
					visibleCells.Add(cells);
				}
			}
		}

		public void MakeVisible(int team, int x, int z)
		{
			if (x >= 0 && x < map.Size.x && z >= 0 && z < map.Size.z)
				(visibleCells[team])[z * map.Size.x + x] = true;
		}

		public bool IsVisible(int team, int idx)
		{
			return (visibleCells[team])[idx];
		}

		public bool IsVisible(int team, IntVec3 cell)
		{
			if (cell.x < 0 || cell.z < 0 || cell.x >= map.Size.x || cell.z >= map.Size.z) return false;
			return (visibleCells[team])[cell.z * map.Size.x + cell.x];
		}
	}
}