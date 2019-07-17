using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Profile;

namespace RimBattle
{
	static class Tools
	{
		// reload new colonists
		//
		public static void RecreateNewColonists()
		{
			Find.GameInitData.startingAndOptionalPawns.Clear();

			var count = Math.Max(1, Find.Scenario.AllParts
				.OfType<ScenPart_ConfigPage_ConfigureStartingPawns>()
				.Select(scenPart => scenPart.pawnCount)
				.FirstOrDefault());

			for (var i = 0; i < count; i++)
				Find.GameInitData.startingAndOptionalPawns.Add(StartingPawnUtility.NewGeneratedStartingPawn());
		}

		// create all settlements
		//
		public static IEnumerable<Settlement> CreateSettlements()
		{
			foreach (var tile in Refs.controller.startingTiles)
			{
				var settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
				settlement.SetFaction(Find.GameInitData.playerFaction);
				settlement.Tile = tile;
				settlement.Name = NameGenerator.GenerateName(Faction.OfPlayer.def.settlementNameMaker);
				Find.WorldObjects.Add(settlement);
				yield return settlement;

				break; // TODO
			}
		}

		// shameless copy of Game.InitNewGame()
		// too much has changed and moved around
		//
		public static void InitNewGame()
		{
			var game = Current.Game;

			var str = LoadedModManager.RunningMods.Select(mod => mod.ToString()).ToCommaList(false);
			Log.Message("Initializing new game with mods " + str, false);
			if (game.Maps.Any<Map>())
			{
				Log.Error("Called InitNewGame() but there already is a map. There should be 0 maps...", false);
				return;
			}
			if (game.InitData == null)
			{
				Log.Error("Called InitNewGame() but init data is null. Create it first.", false);
				return;
			}
			MemoryUtility.UnloadUnusedUnityAssets();
			DeepProfiler.Start("InitNewGame");
			try
			{
				Current.ProgramState = ProgramState.MapInitializing;

				var intVec = new IntVec3(game.InitData.mapSize, 1, game.InitData.mapSize);
				game.World.info.initialMapSize = intVec;
				if (game.InitData.permadeath)
				{
					game.Info.permadeathMode = true;
					game.Info.permadeathModeUniqueName = PermadeathModeUtility.GeneratePermadeathSaveName();
				}

				game.tickManager.gameStartAbsTick = GenTicks.ConfiguredTicksAbsAtGameStart;

				foreach (var settlement in CreateSettlements())
				{
					RecreateNewColonists();
					var map = MapGenerator.GenerateMap(intVec, settlement, settlement.MapGeneratorDef, settlement.ExtraGenStepDefs, null);
					PawnUtility.GiveAllStartingPlayerPawnsThought(ThoughtDefOf.NewColonyOptimism);
					Team.CreateWithColonistsOnMap(map);
					Refs.controller.CreateMapPart(map);
				}

				game.FinalizeInit();

				Current.Game.CurrentMap = Refs.controller.MapForTile(0); // TODO: change to random
				Find.CameraDriver.JumpToCurrentMapLoc(MapGenerator.PlayerStartSpot);
				Find.CameraDriver.ResetSize();
				Find.Scenario.PostGameStart();

				if (Faction.OfPlayer.def.startingResearchTags != null)
				{
					foreach (var tag in Faction.OfPlayer.def.startingResearchTags)
					{
						foreach (var researchProjectDef in DefDatabase<ResearchProjectDef>.AllDefs)
						{
							if (researchProjectDef.HasTag(tag))
							{
								game.researchManager.FinishProject(researchProjectDef, false, null);
							}
						}
					}
				}

				GameComponentUtility.StartedNewGame();
				game.InitData = null;
			}
			finally
			{
				DeepProfiler.End();
			}
		}

		// given a center tile, returns a list of surrounding tiles (r,tr,tl,l,bl,br)
		//
		public static int[] CalculateTiles(int center)
		{
			var worldGrid = Find.WorldGrid;
			var tileIDToNeighbors_offsets = worldGrid.tileIDToNeighbors_offsets;
			var tileIDToNeighbors_values = worldGrid.tileIDToNeighbors_values;

			var tiles = new List<int>() { center };
			var max = (center + 1 >= tileIDToNeighbors_offsets.Count) ? tileIDToNeighbors_values.Count : tileIDToNeighbors_offsets[center + 1];
			for (var i = tileIDToNeighbors_offsets[center]; i < max; i++)
				tiles.Add(tileIDToNeighbors_values[i]);
			return tiles.ToArray();
		}

		// validates several tiles at once
		//
		public static bool CheckTiles(int[] tiles, bool silent = false)
		{
			foreach (var tile in tiles)
			{
				var stringBuilder = new StringBuilder();
				if (!TileFinder.IsValidTileForNewSettlement(tile, stringBuilder))
				{
					if (silent == false)
						Messages.Message(stringBuilder.ToString(), MessageTypeDefOf.RejectInput, false);
					return false;
				}
			}
			return true;
		}

		// returns an adjacted tile
		//
		public static int GetAdjactedTile(int baseTile, int n)
		{
			if (baseTile == -1) return -1;
			var adjactedTiles = Tools.CalculateTiles(baseTile);
			if (n < 0 || n >= adjactedTiles.Length) return -1;
			return adjactedTiles[n];
		}

		// returns custom material reflecting if tile is valid
		//
		public static Material GetMouseTileMaterial(int baseTile, int tile)
		{
			if (baseTile == -1) return default;
			var ok = Tools.CheckTiles(new int[] { baseTile, tile }, true);
			return ok ? WorldMaterials.MouseTile : Refs.MouseTileError;
		}

		// returns custom material reflecting if tile is valid
		//
		public static Material GetSelectedTileMaterial(int baseTile, int tile)
		{
			if (baseTile == -1) return default;
			var ok = Tools.CheckTiles(new int[] { baseTile, tile }, true);
			return ok ? WorldMaterials.SelectedTile : Refs.SelectedTileError;
		}

		// get team id of a colonist
		//
		public static int GetTeamID(this Pawn pawn)
		{
			if (Refs.teamMemberCache.TryGetValue(pawn, out var team))
				return team.id;
			return -1;
		}

		// get team of a colonist
		//
		public static Team GetTeam(this Pawn pawn)
		{
			if (Refs.teamMemberCache.TryGetValue(pawn, out var team))
				return team;
			return null;
		}

		// get weapon range or default for no weapon
		//
		public static float WeaponRange(this Pawn pawn)
		{
			var verb = pawn.equipment?.PrimaryEq?.PrimaryVerb;
			if (verb != null && verb.verbProps.IsMeleeAttack == false)
				return Math.Min(90f, verb.verbProps.range);
			return Refs.defaultVisibleRange;
		}

		private static IEnumerable<IntVec3> GetCircleVectors(float radius)
		{
			if (Refs.circleCache.TryGetValue(radius, out var cells) == false)
			{
				cells = new HashSet<IntVec3>();
				var enumerator = GenRadial.RadialPatternInRadius(radius).GetEnumerator();
				while (enumerator.MoveNext())
				{
					var v = enumerator.Current;
					cells.Add(v);
					cells.Add(new IntVec3(-v.x, 0, v.z));
					cells.Add(new IntVec3(-v.x, 0, -v.z));
					cells.Add(new IntVec3(v.x, 0, -v.z));
				}
				enumerator.Dispose();
				Refs.circleCache[radius] = cells;
			}
			return cells;
		}

		// execute a callback for each cell in a circle
		//
		public static void DoInCircle(this Map map, IntVec3 center, float radius, Action<IntVec3> callback)
		{
			foreach (var vec in GetCircleVectors(radius))
			{
				var pos = center + vec;
				if (pos.InBounds(map))
					callback(pos);
			}
		}

		// fog regeneration with extra visibility injected
		// TODO: make this work with a transpiler
		//
		public static void Regenerate(SectionLayer layer)
		{
			var section = Refs.sectionRef(layer);
			var map = section.map;

			var subMesh = layer.GetSubMesh(MatBases.FogOfWar);
			if (subMesh.mesh.vertexCount == 0)
				SectionLayerGeometryMaker_Solid.MakeBaseGeometry(section, subMesh, AltitudeLayer.FogOfWar);
			subMesh.Clear(MeshParts.Colors);

			var fogGrid = map.fogGrid.fogGrid;
			var visibleGrid = Refs.controller.mapParts[map].visibility.visible;
			var cellIndices = map.cellIndices;

			bool FoggedOrNotVisible(int x, int y)
			{
				var n = cellIndices.CellToIndex(x, y);
				return visibleGrid[n] == false || fogGrid[n];
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
	}
}