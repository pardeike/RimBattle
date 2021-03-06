﻿using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimBattle
{
	// TODO: make these work with a transpiler
	//
	class CopiedMethods
	{
		// fog regeneration with extra visibility injected
		//
		public static void RegenerateFog(SectionLayer_FogOfWar layer)
		{
			var section = Ref.SectionLayer_section(layer);
			var map = section.map;
			var team = Ref.controller.Team;

			var subMesh = layer.GetSubMesh(MatBases.FogOfWar);
			if (subMesh.mesh.vertexCount == 0)
				SectionLayerGeometryMaker_Solid.MakeBaseGeometry(section, subMesh, AltitudeLayer.FogOfWar);
			subMesh.Clear(MeshParts.Colors);

			var fogGrid = map.fogGrid.fogGrid;
			var visibleGrid = map.GetComponent<Visibility>();
			var cellIndices = map.cellIndices;

			bool FoggedOrNotVisible(int x, int z)
			{
				var idx = cellIndices.CellToIndex(x, z);
				return visibleGrid.IsVisible(team, idx) == false || fogGrid[idx];
			}

			var cellRect = section.CellRect;
			var num = map.Size.z - 1;
			var num2 = map.Size.x - 1;
			var flag = false;
			var vertsCovered = new bool[9];

			for (var i = cellRect.minX; i <= cellRect.maxX; i++)
			{
				for (var j = cellRect.minZ; j <= cellRect.maxZ; j++)
				{
					if (FoggedOrNotVisible(i, j))
					{
						for (var k = 0; k < 9; k++)
							vertsCovered[k] = true;
					}
					else
					{
						for (var k = 0; k < 9; k++)
							vertsCovered[k] = false;

						if (j < num && FoggedOrNotVisible(i, j + 1))
						{
							vertsCovered[2] = true;
							vertsCovered[3] = true;
							vertsCovered[4] = true;
						}

						if (j > 0 && FoggedOrNotVisible(i, j - 1))
						{
							vertsCovered[6] = true;
							vertsCovered[7] = true;
							vertsCovered[0] = true;
						}

						if (i < num2 && FoggedOrNotVisible(i + 1, j))
						{
							vertsCovered[4] = true;
							vertsCovered[5] = true;
							vertsCovered[6] = true;
						}

						if (i > 0 && FoggedOrNotVisible(i - 1, j))
						{
							vertsCovered[0] = true;
							vertsCovered[1] = true;
							vertsCovered[2] = true;
						}

						if (j > 0 && i > 0 && FoggedOrNotVisible(i - 1, j - 1))
							vertsCovered[0] = true;

						if (j < num && i > 0 && FoggedOrNotVisible(i - 1, j + 1))
							vertsCovered[2] = true;

						if (j < num && i < num2 && FoggedOrNotVisible(i + 1, j + 1))
							vertsCovered[4] = true;

						if (j > 0 && i < num2 && FoggedOrNotVisible(i + 1, j - 1))
							vertsCovered[6] = true;
					}

					for (var m = 0; m < 9; m++)
					{
						byte a;
						if (vertsCovered[m])
						{
							a = byte.MaxValue;
							flag = true;
						}
						else
							a = 0;

						subMesh.colors.Add(new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, a));
					}
				}
			}
			if (flag)
			{
				subMesh.disabled = false;
				subMesh.FinalizeMesh(MeshParts.Colors);
			}
			else
				subMesh.disabled = true;
		}

		// custom zone visibility
		//
		public static void RegenerateZone(SectionLayer myBase, Section section)
		{
			var map = section.map;
			var visibility = map.GetComponent<Visibility>();
			var cellIndices = map.cellIndices;
			var team = Ref.controller.Team;

			var y = AltitudeLayer.Zone.AltitudeFor();
			var zoneManager = map.zoneManager;
			var cellRect = new CellRect(section.botLeft.x, section.botLeft.z, 17, 17);
			_ = cellRect.ClipInsideMap(map);

			foreach (var layerSubMesh in myBase.subMeshes)
				layerSubMesh.Clear(MeshParts.All);

			for (var i = cellRect.minX; i <= cellRect.maxX; i++)
				for (var j = cellRect.minZ; j <= cellRect.maxZ; j++)
				{
					if (visibility.IsVisible(team, cellIndices.CellToIndex(i, j)) == false)
						continue;

					var zone = zoneManager.ZoneAt(new IntVec3(i, 0, j));
					if (zone != null && !zone.hidden/* && zone.OwnedByTeam() == Ref.controller.team*/)
					{
						var subMesh = myBase.GetSubMesh(zone.Material);
						var count = subMesh.verts.Count;
						subMesh.verts.Add(new Vector3((float)i, y, (float)j));
						subMesh.verts.Add(new Vector3((float)i, y, (float)(j + 1)));
						subMesh.verts.Add(new Vector3((float)(i + 1), y, (float)(j + 1)));
						subMesh.verts.Add(new Vector3((float)(i + 1), y, (float)j));
						subMesh.tris.Add(count);
						subMesh.tris.Add(count + 1);
						subMesh.tris.Add(count + 2);
						subMesh.tris.Add(count);
						subMesh.tris.Add(count + 2);
						subMesh.tris.Add(count + 3);
					}
				}

			for (var i = 0; i < myBase.subMeshes.Count; i++)
				if (myBase.subMeshes[i].verts.Count > 0)
					myBase.subMeshes[i].FinalizeMesh(MeshParts.Verts | MeshParts.Tris);
		}

		// DesignateSingleCell
		//
		public static void DesignateSingleCell_WithTeam(Designator_Build designator, IntVec3 c, int team)
		{
			var entDef = Ref.entDef(designator);
			var stuffDef = Ref.stuffDef(designator);
			var placingRot = Ref.placingRot(designator);

			Thing spawned = null;
			if (DebugSettings.godMode || entDef.GetStatValueAbstract(StatDefOf.WorkToBuild, stuffDef) == 0f)
			{
				if (entDef is TerrainDef)
					designator.Map.terrainGrid.SetTerrain(c, (TerrainDef)entDef);
				else
				{
					var thing = ThingMaker.MakeThing((ThingDef)entDef, stuffDef);
					thing.SetFactionDirect(Faction.OfPlayer);
					spawned = GenSpawn.Spawn(thing, c, designator.Map, placingRot, WipeMode.Vanish, false);
				}
			}
			else
			{
				GenSpawn.WipeExistingThings(c, placingRot, entDef.blueprintDef, designator.Map, DestroyMode.Deconstruct);
				spawned = GenConstruct.PlaceBlueprintForBuild(entDef, c, designator.Map, placingRot, Faction.OfPlayer, stuffDef);
			}

			// this insertion is why we make a copy of this method
			//
			if (team >= 0)
				if (spawned is ThingWithComps compThing)
					CompOwnedBy.SetTeam(compThing, team);

			MoteMaker.ThrowMetaPuffs(GenAdj.OccupiedRect(c, placingRot, entDef.Size), designator.Map);

			if (entDef.PlaceWorkers != null)
				for (var i = 0; i < entDef.PlaceWorkers.Count; i++)
					entDef.PlaceWorkers[i].PostPlace(designator.Map, entDef, c, placingRot);
		}

		// DesignateMultiCell
		//
		public static void DesignateMultiCell_WithTeam(Designator_Build designator, IEnumerable<IntVec3> cells, int team)
		{
			var somethingSucceeded = false;
			var flag = false;
			foreach (var intVec in cells)
			{
				if (designator.CanDesignateCell(intVec).Accepted)
				{
					DesignateSingleCell_WithTeam(designator, intVec, team);
					somethingSucceeded = true;
					if (!flag)
						flag = designator.ShowWarningForCell(intVec);
				}
			}
			designator.Finalize(somethingSucceeded);
		}
	}
}