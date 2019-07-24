using RimWorld.Planet;
using UnityEngine;

namespace RimBattle
{
	class MouseTile_Middle : WorldLayer_MouseTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 0);
		protected override Material Material => Tools.GetMouseTileMaterial(base.Tile, Tile);
	}

	class MouseTile_Right : WorldLayer_MouseTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 1);
		protected override Material Material => Tools.GetMouseTileMaterial(base.Tile, Tile);
	}

	class MouseTile_TopRight : WorldLayer_MouseTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 2);
		protected override Material Material => Tools.GetMouseTileMaterial(base.Tile, Tile);
	}

	class MouseTile_TopLeft : WorldLayer_MouseTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 3);
		protected override Material Material => Tools.GetMouseTileMaterial(base.Tile, Tile);
	}

	class MouseTile_Left : WorldLayer_MouseTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 4);
		protected override Material Material => Tools.GetMouseTileMaterial(base.Tile, Tile);
	}

	class MouseTile_BottomLeft : WorldLayer_MouseTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 5);
		protected override Material Material => Tools.GetMouseTileMaterial(base.Tile, Tile);
	}

	class MouseTile_BottomRight : WorldLayer_MouseTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 6);
		protected override Material Material => Tools.GetMouseTileMaterial(base.Tile, Tile);
	}

	//

	class SelectedTile_Middle : WorldLayer_SelectedTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 0);
		protected override Material Material => Tools.GetSelectedTileMaterial(base.Tile, Tile);
	}

	class SelectedTile_Right : WorldLayer_SelectedTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 1);
		protected override Material Material => Tools.GetSelectedTileMaterial(base.Tile, Tile);
	}

	class SelectedTile_TopRight : WorldLayer_SelectedTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 2);
		protected override Material Material => Tools.GetSelectedTileMaterial(base.Tile, Tile);
	}

	class SelectedTile_TopLeft : WorldLayer_SelectedTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 3);
		protected override Material Material => Tools.GetSelectedTileMaterial(base.Tile, Tile);
	}

	class SelectedTile_Left : WorldLayer_SelectedTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 4);
		protected override Material Material => Tools.GetSelectedTileMaterial(base.Tile, Tile);
	}

	class SelectedTile_BottomLeft : WorldLayer_SelectedTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 5);
		protected override Material Material => Tools.GetSelectedTileMaterial(base.Tile, Tile);
	}

	class SelectedTile_BottomRight : WorldLayer_SelectedTile
	{
		protected override int Tile => Tools.GetAdjactedTile(base.Tile, 6);
		protected override Material Material => Tools.GetSelectedTileMaterial(base.Tile, Tile);
	}
}