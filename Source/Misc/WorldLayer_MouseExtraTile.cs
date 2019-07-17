using RimWorld.Planet;
using UnityEngine;

namespace RimBattle
{
	class WorldLayer_MouseTile_Middle : WorldLayer_MouseTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 0);
		protected override Material Material => Tools.GetMouseTileMaterial(base.Tile, Tile);
	}

	class WorldLayer_MouseTile_Right : WorldLayer_MouseTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 1);
		protected override Material Material => Tools.GetMouseTileMaterial(base.Tile, Tile);
	}

	class WorldLayer_MouseTile_TopRight : WorldLayer_MouseTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 2);
		protected override Material Material => Tools.GetMouseTileMaterial(base.Tile, Tile);
	}

	class WorldLayer_MouseTile_TopLeft : WorldLayer_MouseTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 3);
		protected override Material Material => Tools.GetMouseTileMaterial(base.Tile, Tile);
	}

	class WorldLayer_MouseTile_Left : WorldLayer_MouseTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 4);
		protected override Material Material => Tools.GetMouseTileMaterial(base.Tile, Tile);
	}

	class WorldLayer_MouseTile_BottomLeft : WorldLayer_MouseTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 5);
		protected override Material Material => Tools.GetMouseTileMaterial(base.Tile, Tile);
	}

	class WorldLayer_MouseTile_BottomRight : WorldLayer_MouseTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 6);
		protected override Material Material => Tools.GetMouseTileMaterial(base.Tile, Tile);
	}
}