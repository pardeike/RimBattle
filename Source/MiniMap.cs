using Harmony;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimBattle
{
	public class MiniMap
	{
		readonly Vector2 markerSize = new Vector2(4, 4);

		readonly int idx;
		readonly Map map;
		int mapSizeX;
		int mapSizeZ;
		bool updated;

		public readonly Texture2D texture;

		public MiniMap(int idx)
		{
			this.idx = idx;
			map = Refs.controller.MapByIndex(idx);
			texture = new Texture2D(map.Size.x, map.Size.z, TextureFormat.RGB24, true);
		}

		public void Draw(Rect rect)
		{
			var repainting = Event.current.type == EventType.Repaint;

			if (repainting)
			{
				if (!updated)
					UpdateTexture();

				GUI.color = Color.black;
				Widgets.DrawBox(rect, 2);
				if (Find.CurrentMap == map || Mouse.IsOver(rect))
				{
					GUI.color = Mouse.IsOver(rect) ? Color.yellow : Color.white;
					Widgets.DrawBox(rect, 1);
				}

				rect = rect.ContractedBy(3);

				GUI.color = Mouse.IsOver(rect) ? Color.white : (Find.CurrentMap == map ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.8f, 0.8f, 0.8f));
				GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
			}

			Rect Marker(Pawn pawn)
			{
				var pos = pawn.PositionHeld;
				var offset = new Vector2((float)pos.x / mapSizeX * rect.width, (1 - (float)pos.z / mapSizeZ) * rect.height);
				return new Rect(rect.position + offset - markerSize / 2, markerSize);
			}

			map.mapPawns.AllPawns
				.Where(pawn => pawn.RaceProps.Humanlike && map.fogGrid.IsFogged(pawn.Position) == false)
				.Do(pawn =>
				{
					var r = Marker(pawn);
					var hostile = pawn.HostileTo(Faction.OfPlayer);
					var colonist = pawn.IsColonist;

					MouseoverSounds.DoRegion(r, SoundDefOf.Mouseover_Standard);

					if (repainting)
					{
						var color = PawnNameColorUtility.PawnNameColorOf(pawn);
						Widgets.DrawBoxSolid(r, color);
						if (Find.Selector.IsSelected(pawn))
						{
							GUI.color = Color.black;
							Widgets.DrawBox(r.ContractedBy(-1), 1);
						}
					}

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
							Refs.controller.battleOverview.showing = false;
							return;
						}
					}
				});

			if (Widgets.ButtonInvisible(rect, true))
			{
				Refs.controller.battleOverview.showing = false;
				SoundDefOf.MapSelected.PlayOneShotOnCamera(null);
				Current.Game.CurrentMap = map;
			}
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

						//if (thing is Pawn pawn)
						//{
						//	pawns.Add(pawn);
						//	break;
						//}

						if (thing.def.category == ThingCategory.Plant)
							texture.SetPixel(x, z, Refs.plantColor);
					}
				}

			//foreach (var pawn in pawns)
			//{
			//	if (pawn.RaceProps.Humanlike == false)
			//		texture.SetPixel(pawn.Position.x, pawn.Position.z, Refs.animalColor);
			//	else
			//		ColonistDot(pawn);
			//}

			texture.Apply(true);
			updated = true;
		}

		//void ColonistDot(Pawn pawn)
		//{
		//	var color = PawnNameColorUtility.PawnNameColorOf(pawn);
		//	foreach (var cell in GenRadial.RadialCellsAround(pawn.Position, 1f, true))
		//		texture.SetPixel(cell.x, cell.z, color);
		//}
	}
}