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
		public static void RegenerateFog(SectionLayer layer)
		{
			var section = Ref.SectionLayer_section(layer);
			var map = section.map;

			var subMesh = layer.GetSubMesh(MatBases.FogOfWar);
			if (subMesh.mesh.vertexCount == 0)
				SectionLayerGeometryMaker_Solid.MakeBaseGeometry(section, subMesh, AltitudeLayer.FogOfWar);
			subMesh.Clear(MeshParts.Colors);

			var fogGrid = map.fogGrid.fogGrid;
			var visibleGrid = map.GetComponent<MapPart>().visibility.visible;
			var cellIndices = map.cellIndices;

			bool FoggedOrNotVisible(int x, int y)
			{
				var n = cellIndices.CellToIndex(x, y);
				return visibleGrid[n] == 0 || fogGrid[n];
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

		// ???
		//
		public static void RegenerateZone(SectionLayer myBase, Section section)
		{
			var map = section.map;
			var visibility = map.GetComponent<MapPart>().visibility;
			var cellIndices = map.cellIndices;

			var y = AltitudeLayer.Zone.AltitudeFor();
			var zoneManager = map.zoneManager;
			var cellRect = new CellRect(section.botLeft.x, section.botLeft.z, 17, 17);
			_ = cellRect.ClipInsideMap(map);

			foreach (var layerSubMesh in myBase.subMeshes)
				layerSubMesh.Clear(MeshParts.All);

			for (var i = cellRect.minX; i <= cellRect.maxX; i++)
				for (var j = cellRect.minZ; j <= cellRect.maxZ; j++)
				{
					if (visibility.IsVisible(cellIndices.CellToIndex(i, j)) == false)
						continue;

					var zone = zoneManager.ZoneAt(new IntVec3(i, 0, j));
					if (zone != null && !zone.hidden)
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
	}
}
