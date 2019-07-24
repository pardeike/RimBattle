using Harmony;
using RimWorld;
using System;
using System.Collections;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimBattle
{
	public class MiniMap
	{
		public class Configuration
		{
			public Func<Map, bool> isCurrent;
			public Func<Map, Color?> isSelected;
			public Func<Map, bool> canSelect;
			public Action<Map> setSelected;
			public bool canSelectMarkers;
		}

		readonly Vector2 markerSize = new Vector2(4, 4);

		readonly Map map;
		int mapSizeX;
		int mapSizeZ;
		bool updated;

		public readonly Texture2D texture;

		public MiniMap(int idx)
		{
			map = Ref.controller.MapByIndex(idx);
			texture = new Texture2D(map.Size.x, map.Size.z, TextureFormat.RGB24, true);
		}

		public void Draw(Rect rect, Configuration config)
		{
			var repainting = Event.current.type == EventType.Repaint;
			var isCurrent = config.isCurrent(map);
			var isSelected = config.isSelected(map);
			var canSelect = config.canSelect(map);

			Rect Marker(Pawn pawn)
			{
				var pos = pawn.PositionHeld;
				var offset = new Vector2((float)pos.x / mapSizeX * rect.width, (1 - (float)pos.z / mapSizeZ) * rect.height);
				return new Rect(rect.position + offset - markerSize / 2, markerSize);
			}

			if (repainting)
			{
				if (!updated)
					UpdateTexture();

				if (isSelected.HasValue)
				{
					GUI.color = isSelected.Value;
					Widgets.DrawBox(rect, 2);
				}
				else
				{
					GUI.color = Color.black;
					Widgets.DrawBox(rect, 2);
					GUI.color = canSelect ? Color.white : new Color(0.2f, 0.2f, 0.2f);
					Widgets.DrawBox(rect, 1);
				}

				GUI.color = new Color(1f, 1f, 1f, 0.4f);
				if (canSelect || isCurrent)
					GUI.color = (canSelect && Mouse.IsOver(rect)) ? Color.white : (isSelected.HasValue ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.8f, 0.8f, 0.8f));
				GUI.DrawTexture(rect.ContractedBy(3), texture, ScaleMode.ScaleToFit);

				if (isCurrent)
				{
					var length = rect.width / 6f;
					var thick = Math.Min(8f, length / 5f);

					Widgets.DrawBoxSolid(new Rect(rect.xMin, rect.yMin, length, thick), Color.white);
					Widgets.DrawBoxSolid(new Rect(rect.xMin, rect.yMin, thick, length), Color.white);

					Widgets.DrawBoxSolid(new Rect(rect.xMax - length, rect.yMin, length, thick), Color.white);
					Widgets.DrawBoxSolid(new Rect(rect.xMax - thick, rect.yMin, thick, length), Color.white);

					Widgets.DrawBoxSolid(new Rect(rect.xMin, rect.yMax - thick, length, thick), Color.white);
					Widgets.DrawBoxSolid(new Rect(rect.xMin, rect.yMax - length, thick, length), Color.white);

					Widgets.DrawBoxSolid(new Rect(rect.xMax - length, rect.yMax - thick, length, thick), Color.white);
					Widgets.DrawBoxSolid(new Rect(rect.xMax - thick, rect.yMax - length, thick, length), Color.white);
				}
			}

			var fogGrid = map.fogGrid;
			var visibleGrid = map.GetComponent<MapPart>().visibility;
			map.mapPawns.AllPawns.Do(pawn =>
			{
				if (pawn.RaceProps.Humanlike == false)
					return;

				if (pawn.Position.IsValid == false)
					return;

				var cellIndex = map.cellIndices.CellToIndex(pawn.Position);
				if (fogGrid.IsFogged(cellIndex) || visibleGrid.IsVisible(cellIndex) == false)
					return;

				var r = Marker(pawn);
				var hostile = pawn.HostileTo(Faction.OfPlayer);
				var colonist = pawn.IsColonist;

				MouseoverSounds.DoRegion(r, SoundDefOf.Mouseover_Standard);

				if (repainting)
				{
					var color = PawnNameColorUtility.PawnNameColorOf(pawn);
					color.a = canSelect || isCurrent ? 1f : 0.4f;
					Widgets.DrawBoxSolid(r, color);
					if (Find.Selector.IsSelected(pawn))
					{
						GUI.color = Color.black;
						Widgets.DrawBox(r.ContractedBy(-1), 1);
					}
				}

				if (config.canSelectMarkers)
				{
					r = r.ContractedBy(-2);
					if (Mouse.IsOver(r.ContractedBy(-2)))
					{
						if (repainting)
						{
							GUI.color = Color.yellow;
							Widgets.DrawBox(r, 1);
						}

						if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
						{
							Event.current.Use();
							CameraJumper.TryJumpAndSelect(pawn);
							Ref.controller.battleOverview.showing = false;
							return;
						}
					}
				}
			});

			if (canSelect && Widgets.ButtonInvisible(rect, true))
				config.setSelected(map);

			GUI.color = Color.white;
		}

		public void UpdateTextureFully()
		{
			var it = UpdateTexture();
			while (it.MoveNext()) ;
		}

		public IEnumerator UpdateTexture()
		{
			const int pixelsPerYield = 100;
			var yieldCounter = 0;

			mapSizeX = map.Size.x;
			mapSizeZ = map.Size.z;

			if (texture.width != mapSizeX || texture.height != mapSizeZ)
			{
				texture.Resize(mapSizeX, mapSizeZ);
				texture.Apply(true);
			}

			var visibility = map.GetComponent<MapPart>().visibility;
			for (var x = 0; x < mapSizeX; x++)
				for (var z = 0; z < mapSizeZ; z++)
				{
					var idx = z * mapSizeX + x;

					if (map.fogGrid.IsFogged(idx))
					{
						texture.SetPixel(x, z, Statics.fogColor);
						continue;
					}

					if (visibility.IsVisible(idx) == false)
					{
						texture.SetPixel(x, z, Statics.notVisibleColor);
						continue;
					}

					var edifice = map.edificeGrid[idx];
					if (edifice != null)
					{
						if (edifice.def.building.isNaturalRock)
						{
							texture.SetPixel(x, z, Statics.mountainColor);
							goto done;
						}

						var filled = edifice.def.Fillage == FillCategory.Full && !edifice.def.IsDoor;
						texture.SetPixel(x, z, Statics.edificeColor * (filled ? 1f : 0.5f));
						goto done;
					}

					var terrain = map.terrainGrid.TerrainAt(idx);
					if (terrain.IsWater || terrain.IsRiver)
					{
						texture.SetPixel(x, z, Statics.waterColor);
						goto done;
					}

					texture.SetPixel(x, z, Statics.groundColor);

				done:
					var things = map.thingGrid.ThingsListAtFast(idx);
					for (var i = 0; i < things.Count; i++)
						if (things[i].def.category == ThingCategory.Plant)
							texture.SetPixel(x, z, Statics.plantColor);

					if (++yieldCounter > pixelsPerYield)
					{
						yieldCounter = 0;
						yield return null;
					}
				}

			texture.Apply(true);
			updated = true;
		}
	}
}