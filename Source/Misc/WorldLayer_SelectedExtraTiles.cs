using RimWorld.Planet;
using UnityEngine;

namespace RimBattle
{
	class WorldLayer_SelectedTile_Middle : WorldLayer_SelectedTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 0);
		protected override Material Material => Tools.GetSelectedTileMaterial(base.Tile, Tile);
	}

	class WorldLayer_SelectedTile_Right : WorldLayer_SelectedTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 1);
		protected override Material Material => Tools.GetSelectedTileMaterial(base.Tile, Tile);
	}

	class WorldLayer_SelectedTile_TopRight : WorldLayer_SelectedTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 2);
		protected override Material Material => Tools.GetSelectedTileMaterial(base.Tile, Tile);
	}

	class WorldLayer_SelectedTile_TopLeft : WorldLayer_SelectedTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 3);
		protected override Material Material => Tools.GetSelectedTileMaterial(base.Tile, Tile);
	}

	class WorldLayer_SelectedTile_Left : WorldLayer_SelectedTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 4);
		protected override Material Material => Tools.GetSelectedTileMaterial(base.Tile, Tile);
	}

	class WorldLayer_SelectedTile_BottomLeft : WorldLayer_SelectedTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 5);
		protected override Material Material => Tools.GetSelectedTileMaterial(base.Tile, Tile);
	}

	class WorldLayer_SelectedTile_BottomRight : WorldLayer_SelectedTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 6);
		protected override Material Material => Tools.GetSelectedTileMaterial(base.Tile, Tile);
	}
}