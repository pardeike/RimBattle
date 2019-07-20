using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Profile;

// NOTES
//
// IntVec3 playerStartSpot = MapGenerator.PlayerStartSpot;

namespace RimBattle
{
	static class Tools
	{
		// reload new colonists
		//
		public static void RecreateNewColonists(bool clear)
		{
			Find.GameInitData.startingAndOptionalPawns.Clear();
			if (clear)
				return;

			var count = Math.Max(1, Find.Scenario.AllParts
				.OfType<ScenPart_ConfigPage_ConfigureStartingPawns>()
				.Select(scenPart => scenPart.pawnCount)
				.FirstOrDefault());

			for (var i = 0; i < count; i++)
				Find.GameInitData.startingAndOptionalPawns.Add(StartingPawnUtility.NewGeneratedStartingPawn());
		}

		// create a settlement
		//
		public static Settlement CreateSettlement(int tile)
		{
			var settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
			settlement.SetFaction(Find.GameInitData.playerFaction);
			settlement.Tile = tile;
			settlement.Name = NameGenerator.GenerateName(Faction.OfPlayer.def.settlementNameMaker);
			Find.WorldObjects.Add(settlement);
			return settlement;
		}

		// shameless copy of Game.InitNewGame()
		// too much has changed and moved around
		//
		public static void InitNewGame()
		{
			var game = Current.Game;

			var str = LoadedModManager.RunningMods.Select(mod => mod.ToString()).ToCommaList(false);
			Log.Message($"Initializing new game with mods {str}", false);
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
			if (Refs.forceMapSize > 0)
				game.InitData.mapSize = Refs.forceMapSize;

			MemoryUtility.UnloadUnusedUnityAssets();
			DeepProfiler.Start("InitNewGame");
			try
			{
				Current.ProgramState = ProgramState.MapInitializing;

				var mapSize = new IntVec3(game.InitData.mapSize, 1, game.InitData.mapSize);
				game.World.info.initialMapSize = mapSize;
				if (game.InitData.permadeath)
				{
					game.Info.permadeathMode = true;
					game.Info.permadeathModeUniqueName = PermadeathModeUtility.GeneratePermadeathSaveName();
				}

				game.tickManager.gameStartAbsTick = GenTicks.ConfiguredTicksAbsAtGameStart;

				var allTiles = Refs.controller.tiles;
				var allTileIndices = TeamTiles(GameController.tileCount, GameController.tileCount);
				var teamTileIndices = TeamTiles(GameController.tileCount, GameController.teamCount);
				for (var i = 0; i < allTiles.Count; i++)
				{
					var tile = allTiles[i];
					var tileIndex = allTileIndices[i];
					var hasTeam = teamTileIndices.Contains(tileIndex);
					RecreateNewColonists(hasTeam == false);
					var settlement = CreateSettlement(tile);
					var map = MapGenerator.GenerateMap(mapSize, settlement, settlement.MapGeneratorDef, settlement.ExtraGenStepDefs, null);
					PawnUtility.GiveAllStartingPlayerPawnsThought(ThoughtDefOf.NewColonyOptimism);
					if (hasTeam)
						Team.CreateWithColonistsOnMap(map);
					Refs.controller.CreateMapPart(map);
				}

				game.FinalizeInit();

				Current.Game.CurrentMap = Refs.controller.MapForTile(0);

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

		// read multiple textures at once
		//
		internal static Texture2D[] GetTextures(string path, int idx1, int idx2)
		{
			return Enumerable.Range(idx1, idx2)
				.Select(i => ContentFinder<Texture2D>.Get(path.Replace("#", $"{i}")))
				.ToArray();
		}

		// general stats manipulation
		//
		public static void TweakStat(StatDef stat, ref float result)
		{
			// much faster
			if (stat == StatDefOf.ConstructionSpeed) { result *= 10f; return; }
			if (stat == StatDefOf.ConstructionSpeedFactor) { result *= 10f; return; }
			if (stat == StatDefOf.ResearchSpeed) { result *= 10f; return; }
			if (stat == StatDefOf.ResearchSpeedFactor) { result *= 10f; return; }
			if (stat == StatDefOf.PlantWorkSpeed) { result *= 10f; return; }
			if (stat == StatDefOf.SmoothingSpeed) { result *= 10f; return; }
			if (stat == StatDefOf.UnskilledLaborSpeed) { result *= 10f; return; }
			if (stat == StatDefOf.AnimalGatherSpeed) { result *= 10f; return; }
			if (stat == StatDefOf.ImmunityGainSpeed) { result *= 10f; return; }
			if (stat == StatDefOf.ImmunityGainSpeedFactor) { result *= 10f; return; }
			if (stat == StatDefOf.WorkTableWorkSpeedFactor) { result *= 10f; return; }

			// chances
			if (stat == StatDefOf.ConstructSuccessChance) { result *= 2f; return; }
			if (stat == StatDefOf.TameAnimalChance) { result *= 2f; return; }

			// delays
			if (stat == StatDefOf.EquipDelay) { result /= 2f; return; }

			// faster
			if (stat == StatDefOf.EatingSpeed) { result *= 2f; return; }
			if (stat == StatDefOf.MiningSpeed) { result *= 2f; return; }
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
			return GameController.TilePattern
				.Where(idx => idx < tiles.Count)
				.Select(idx => tiles[idx]).ToArray();
		}

		// returns a specific constellation form number of maps and teams
		//
		public static int[] TeamTiles(int tileCount, int teamCount)
		{
			return Refs.teamTiles[tileCount - 1][teamCount - 2];
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
			var adjactedTiles = CalculateTiles(baseTile);
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
		public static float WeaponRange(this Pawn pawn, bool squared = false)
		{
			var verb = pawn.equipment?.PrimaryEq?.PrimaryVerb;
			float range;
			if (verb != null && verb.verbProps.IsMeleeAttack == false)
				range = Math.Min(90f, verb.verbProps.range);
			else
				range = Refs.defaultVisibleRange;
			return squared ? range * range : range;
		}

		// hours to human readable text
		//
		public static string TranslateHoursToText(float hours)
		{
			var ticks = (int)(GenDate.TicksPerHour * hours);
			return ticks.ToStringTicksToPeriodVerbose(false, true);
		}

		// a cached generator for all cells in a given circle
		//
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
			var section = Refs.section(layer);
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