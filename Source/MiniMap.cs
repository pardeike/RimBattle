using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;


namespace RimBattle
{
	public class MiniMap
	{
		readonly Map map;
		int mapSizeX;
		int mapSizeZ;
		bool updated;

		public readonly Texture2D texture;

		public MiniMap(Map map)
		{
			this.map = map;
			texture = new Texture2D(map.Size.x, map.Size.z, TextureFormat.RGB24, true);
		}

		public void Draw(Rect rect)
		{
			if (!updated)
				UpdateTexture();

			GUI.color = new Color(1f, 1f, 1f);
			GUI.DrawTexture(rect, Refs.MiniMapBG);
			rect = rect.ContractedBy(1);
			GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill);
		}

		public void UpdateTexture()
		{
			mapSizeX = map.Size.x;
			mapSizeZ = map.Size.z;

			if (texture.width != mapSizeX || texture.height != mapSizeZ)
			{
				texture.Resize(mapSizeX, mapSizeZ);
				texture.Apply(true);
			}

			var pawns = new List<Pawn>();
			var visibility = Refs.controller.mapParts[map].visibility;
			for (var x = 0; x < mapSizeX; x++)
				for (var z = 0; z < mapSizeZ; z++)
				{
					var idx = z * mapSizeX + x;

					if (map.fogGrid.IsFogged(idx))
					{
						texture.SetPixel(x, z, Refs.fogColor);
						continue;
					}

					if (visibility.IsVisible(idx) == false)
					{
						texture.SetPixel(x, z, Refs.notVisibleColor);
						continue;
					}

					var edifice = map.edificeGrid[idx];
					if (edifice != null)
					{
						if (edifice.def.building.isNaturalRock)
						{
							texture.SetPixel(x, z, Refs.mountainColor);
							goto done;
						}

						var filled = edifice.def.Fillage == FillCategory.Full && !edifice.def.IsDoor;
						texture.SetPixel(x, z, Refs.edificeColor * (filled ? 1f : 0.5f));
						goto done;
					}

					var terrain = map.terrainGrid.TerrainAt(idx);
					if (terrain.IsWater || terrain.IsRiver)
					{
						texture.SetPixel(x, z, Refs.waterColor);
						goto done;
					}

					texture.SetPixel(x, z, Refs.groundColor);

				done:
					var things = map.thingGrid.ThingsListAtFast(idx);
					for (var i = 0; i < things.Count; i++)
					{
						var thing = things[i];

						if (thing is Pawn pawn)
						{
							pawns.Add(pawn);
							break;
						}

						if (thing.def.category == ThingCategory.Plant)
							texture.SetPixel(x, z, Refs.plantColor);
					}
				}

			foreach (var pawn in pawns)
			{


				if (pawn.RaceProps.Humanlike == false)
					texture.SetPixel(pawn.Position.x, pawn.Position.z, Refs.animalColor);
				else
					ColonistDot(pawn);
			}

			texture.Apply(true);
			updated = true;
		}

		void ColonistDot(Pawn pawn)
		{
			var color = PawnNameColorUtility.PawnNameColorOf(pawn);
			foreach (var cell in GenRadial.RadialCellsAround(pawn.Position, 1f, true))
				texture.SetPixel(cell.x, cell.z, color);
		}
	}
}