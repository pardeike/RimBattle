using Harmony;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Profile;

// TODO: IntVec3 playerStartSpot = MapGenerator.PlayerStartSpot;

namespace RimBattle
{
	static class Tools
	{
		// recreate new colonists
		//
		public static void AddNewColonistsToTeam(Team team)
		{
			var count = Math.Max(1, Find.Scenario.AllParts
				.OfType<ScenPart_ConfigPage_ConfigureStartingPawns>()
				.Select(scenPart => scenPart.pawnCount)
				.FirstOrDefault());

			for (var i = 0; i < count; i++)
			{
				var pawn = StartingPawnUtility.NewGeneratedStartingPawn();
				team.Add(pawn);
				Find.GameInitData.startingAndOptionalPawns.Add(pawn);
			}
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
			if (Statics.forceMapSize > 0)
				game.InitData.mapSize = Statics.forceMapSize;

			MemoryUtility.UnloadUnusedUnityAssets();
			DeepProfiler.Start("InitNewGame");
			try
			{
				Current.ProgramState = ProgramState.MapInitializing;

				Ref.controller.mapSize = game.InitData.mapSize;
				var mapSize = new IntVec3(game.InitData.mapSize, 1, game.InitData.mapSize);
				game.World.info.initialMapSize = mapSize;
				if (game.InitData.permadeath)
				{
					game.Info.permadeathMode = true;
					game.Info.permadeathModeUniqueName = PermadeathModeUtility.GeneratePermadeathSaveName();
				}

				game.tickManager.gameStartAbsTick = GenTicks.ConfiguredTicksAbsAtGameStart;

				var arrivalMethod = Find.Scenario.AllParts.OfType<ScenPart_PlayerPawnsArriveMethod>().First();
				Traverse.Create(arrivalMethod).Field("method").SetValue(PlayerPawnsArriveMethod.Standing);

				var allTiles = Ref.controller.tiles;
				var allTileIndices = TeamTiles(Ref.controller.tileCount, Ref.controller.tileCount);
				var teamTileIndices = TeamTiles(Ref.controller.tileCount, Ref.controller.teamCount);
				for (var i = 0; i < allTiles.Count; i++)
				{
					var tile = allTiles[i];
					var tileIndex = allTileIndices[i];
					var hasTeam = teamTileIndices.Contains(tileIndex);

					Find.GameInitData.startingAndOptionalPawns.Clear();
					Team team = null;
					if (hasTeam)
					{
						team = Ref.controller.CreateTeam();
						AddNewColonistsToTeam(team);
					}

					var settlement = CreateSettlement(tile);
					var map = MapGenerator.GenerateMap(mapSize, settlement, settlement.MapGeneratorDef, settlement.ExtraGenStepDefs, null);
					PawnUtility.GiveAllStartingPlayerPawnsThought(ThoughtDefOf.NewColonyOptimism);

					Ref.controller.CreateMapPart(map);
					if (i == 0)
						Current.Game.CurrentMap = map;
				}

				game.FinalizeInit();
				game.playSettings.useWorkPriorities = true;

				Find.CameraDriver.JumpToCurrentMapLoc(MapGenerator.PlayerStartSpot);
				Find.CameraDriver.ResetSize();
				Find.Scenario.PostGameStart();

				if (Faction.OfPlayer.def.startingResearchTags != null)
					foreach (var tag in Faction.OfPlayer.def.startingResearchTags)
						foreach (var researchProjectDef in DefDatabase<ResearchProjectDef>.AllDefs)
							if (researchProjectDef.HasTag(tag))
								game.researchManager.FinishProject(researchProjectDef, false, null);

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
		public static Texture2D[] GetTextures(string path, int idx1, int idx2)
		{
			return Enumerable.Range(idx1, idx2)
				.Select(i => ContentFinder<Texture2D>.Get(path.Replace("#", $"{i}")))
				.ToArray();
		}

		// read multiple materials at once
		//
		public static Material[] GetMaterials(string path, int idx1, int idx2, Shader shader)
		{
			return Enumerable.Range(idx1, idx2)
				.Select(i =>
					MaterialPool.MatFrom(path.Replace("#", $"{i}"), shader)
				)
				.ToArray();
		}

		// general stats manipulation
		//
		public static void TweakStat(Thing thing, StatDef stat, ref float result)
		{
			var pawn = thing as Pawn;
			if (pawn != null && pawn.RaceProps.Animal)
				if (stat == StatDefOf.MinimumHandlingSkill)
				{
					result = 0f;
					return;
				}

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
			return TilePattern()
				.Where(idx => idx < tiles.Count)
				.Select(idx => tiles[idx]).ToArray();
		}

		// returns the current c,r,tr,tl,l,bl,br indices for all tiles
		// read left to right, top to bottom
		//
		public static int[] TilePattern()
		{
			var n = Ref.controller.tileCount;
			return Statics.teamTiles[n - 1][n - 2];
		}

		// returns a map for a tile number
		// TODO: does this exist in vanilla?
		//
		public static Map MapForTile(int tile)
		{
			return Find.Maps.First(map => map.Tile == tile);
		}

		// returns a specific constellation form number of maps and teams
		//
		public static int[] TeamTiles(int tileCount, int teamCount)
		{
			return Statics.teamTiles[tileCount - 1][teamCount - 2];
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

		// convert angle index to basic directions
		//
		public static void RowAndColumnFromIndex(int idx, out int row, out int col)
		{
			row = 0 + (idx == 4 || idx < 2 ? 1 : 0) + (idx > 4 ? 2 : 0);
			col = 0 + (idx == 3 || idx == 5 ? 1 : 0) + (idx == 0 ? 2 : 0) + (idx == 2 || idx == 6 ? 3 : 0) + (idx == 1 ? 4 : 0);
		}

		// convert angle index to basic directions
		//
		public static Rot4 GetRo4Angle(int n)
		{
			if (n / 2 == 0)
				return Rot4.East;
			if (n / 2 == 1)
				return Rot4.North;
			if (n / 2 == 2)
				return Rot4.West;
			return Rot4.South;
		}

		// half edges divide each edge in the middle, resulting in 8 half edges
		// they are ordered counter-clockwise from 3 o'clock
		//
		public static IEnumerable<IEnumerable<IntVec3>> GetHalfEdges(int len)
		{
			for (var n = 0; n < 8; n++)
			{
				var rot = GetRo4Angle(n);
				var cells = new CellRect(0, 0, len, len).GetEdgeCells(rot);
				yield return n % 2 == 0 ? cells.Skip(len / 2) : cells.Take(len / 2);
			}
		}

		// get exit cells in groups that resemble half edges
		// each group has their half edges ordered from the location towards a specific direction
		//
		public static IEnumerable<IEnumerable<IntVec3>> GetSortedHalfEdgeGroups(Map fromMap, Map toMap, IntVec3 pawnsCenter)
		{
			var controller = Ref.controller;
			var fromIndex = controller.TileIndex(fromMap.Tile);
			var toIndex = controller.TileIndex(toMap.Tile);

			RowAndColumnFromIndex(fromIndex, out var rowFrom, out var colFrom);
			RowAndColumnFromIndex(toIndex, out var rowTo, out var colTo);

			var destination = fromMap.Center;
			if (rowFrom == rowTo)
				destination.x += colFrom < colTo ? fromMap.Size.x : -fromMap.Size.x;
			else
			{
				destination.x += colFrom < colTo ? fromMap.Size.x / 2 : -fromMap.Size.x / 2;
				destination.z += rowFrom < rowTo ? -fromMap.Size.x : fromMap.Size.x;
			}

			Func<IEnumerable<IntVec3>, int> MinEdgeDistanceTo(IntVec3 pos)
			{
				return (cells) => cells.Sum(cell => (cell - pos).LengthHorizontalSquared + (cell - pawnsCenter).LengthHorizontalSquared / 2);
			}

			foreach (var halfEdge in GetHalfEdges(fromMap.Size.x).OrderBy(MinEdgeDistanceTo(destination)))
				yield return halfEdge;
		}

		// get a spot that is used to select a nearby cell in an edge group
		//
		public static IntVec3 GetAverageCenter(IEnumerable<Pawn> pawns)
		{
			var map = pawns.First().Map;
			var cell = map.Center;
			if (pawns.Any())
			{
				cell = IntVec3.Zero;
				foreach (var pawn in pawns)
					cell += pawn.Position;
				cell.x /= pawns.Count();
				cell.z /= pawns.Count();
			}
			return cell;
		}

		// returns a flipped enter spot from an exit spot
		//
		public static IntVec3 GetEnterSpot(int fromTile, int toTile, IntVec3 exitSpot)
		{
			var map = MapForTile(toTile);

			RowAndColumnFromIndex(Ref.controller.TileIndex(fromTile), out var rowFrom, out var colFrom);
			RowAndColumnFromIndex(Ref.controller.TileIndex(toTile), out var rowTo, out var colTo);

			if (rowFrom == rowTo)
				exitSpot.x = map.Size.x - exitSpot.x;
			else
			{
				exitSpot.x += colFrom < colTo ? -map.Center.x : map.Center.x;
				exitSpot.z = map.Size.z - exitSpot.z;
			}

			return exitSpot;
		}

		// get a random exit spot on a map edge towards the destination
		//
		public static IntVec3 FindEdgeSpot(int fromTile, int toTile, IEnumerable<Pawn> pawns, IntVec3 nearSpot)
		{
			var fromMap = MapForTile(fromTile);
			var toMap = MapForTile(toTile);

			bool Validator(IntVec3 cell)
			{
				if (cell.Fogged(fromMap) || cell.Standable(fromMap) == false) return false;
				return pawns.Any() == false || pawns.All(pawn => pawn.CanReach(cell, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn));
			}

			if (nearSpot.IsValid)
			{
				var cells = new CellRect(0, 0, toMap.Size.x, toMap.Size.z).EdgeCells
					.Where(Validator).OrderBy(cell => (cell - nearSpot).LengthHorizontalSquared);
				if (cells.Any())
					return cells.First();
				return IntVec3.Invalid;
			}

			var location = GetAverageCenter(pawns);
			foreach (var halfEdge in GetSortedHalfEdgeGroups(fromMap, toMap, location))
			{
				var cells = halfEdge.Where(Validator).OrderBy(cell => (cell - location).LengthHorizontalSquared);
				if (cells.Any())
					return cells.First();
			}
			return IntVec3.Invalid;
		}

		// returns custom material reflecting if tile is valid
		//
		public static Material GetMouseTileMaterial(int baseTile, int tile)
		{
			if (baseTile == -1) return default;
			var ok = CheckTiles(new int[] { baseTile, tile }, true);
			return ok ? WorldMaterials.MouseTile : Statics.MouseTileError;
		}

		// returns custom material reflecting if tile is valid
		//
		public static Material GetSelectedTileMaterial(int baseTile, int tile)
		{
			if (baseTile == -1) return default;
			var ok = CheckTiles(new int[] { baseTile, tile }, true);
			return ok ? WorldMaterials.SelectedTile : Statics.SelectedTileError;
		}

		// create and add a team master to the world
		//
		public static Pawn CreateTeamMaster()
		{
			var master = StartingPawnUtility.NewGeneratedStartingPawn();
			master.health.SetDead();
			master.playerSettings.displayOrder = 1000 + Ref.controller.teams.Count;
			Find.WorldPawns.PassToWorld(master, PawnDiscardDecideMode.KeepForever);
			return master;
		}

		// get team id of a colonist
		//
		public static int GetTeamID(this Pawn pawn)
		{
			if (pawn == null || pawn.playerSettings == null) return -1;
			var master = Ref.master(pawn.playerSettings);
			if (master == null) return -1;
			var teamIdOfRealMaster = master.GetTeamID();
			if (teamIdOfRealMaster >= 0)
				return teamIdOfRealMaster;
			return master.playerSettings.displayOrder - 1000;
		}

		// update our visibility grid
		//
		public static void UpdateVisibility(Thing thing, IntVec3 pos)
		{
			var pawn = thing as Pawn;
			if (pawn == null) return;
			var map = pawn.Map;
			if (map == null) return;
			if (pawn.Faction != Faction.OfPlayer) return;

			var controller = Ref.controller;
			if (controller.InMyTeam(pawn))
			{
				var visibility = map.GetComponent<MapPart>().visibility;
				map.DoInCircle(pos, pawn.WeaponRange(), visibility.MakeVisible);
				var radius = (int)Math.Ceiling(pawn.WeaponRange());
				map.MapMeshDirtyRect(pos.x - radius, pos.z - radius, pos.x + radius, pos.z + radius);
			}
		}

		// check our visibility grid using a cell
		//
		public static bool IsVisible(Map map, IntVec3 loc)
		{
			if (map == null) return false;
			return map.GetComponent<MapPart>().visibility.IsVisible(loc);
		}

		// check our visibility grid for a pawn
		//
		public static bool IsVisible(Thing pawn)
		{
			return IsVisible(pawn.Map, pawn.Position);
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
				range = Statics.defaultVisibleRange;
			return squared ? range * range : range;
		}

		// efficiently replace MapDrawer.MapMeshDirty with a rectangular algorithm
		// optimized to death, lets hope for the best
		//
		public static void MapMeshDirtyRect(this Map map, int xMin, int zMin, int xMax, int zMax)
		{
			var len = map.Size.x;
			if (xMin < 1) xMin = 1;
			if (zMin < 1) zMin = 1;
			if (xMax > len - 2) xMax = len - 2;
			if (zMax > len - 2) zMax = len - 2;

			var sectionsRef = Ref.sections(map.mapDrawer);
			for (var x = (xMin - 1) / 17; x <= (xMax + 1) / 17; x++)
				for (var z = (zMin - 1) / 17; z <= (zMax + 1) / 17; z++)
					sectionsRef[x, z].dirtyFlags |= MapMeshFlag.FogOfWar;
		}

		// test if an object is selectable (vanilla only allows Thing and Zone)
		//
		public static bool CanSelect(object obj)
		{
			if (obj == null)
				return false;

			var controller = Ref.controller;
			if (obj is Zone zone)
				return zone.cells.Any(cell => Tools.IsVisible(zone.Map, cell));

			var thing = obj as Thing;
			if (thing == null)
				return true;

			if (!thing.def.selectable)
				return false;

			if (thing.def.size.x != 1 || thing.def.size.z != 1)
			{
				var map = thing.Map;
				return thing.OccupiedRect().Cells.Any(cell => Tools.IsVisible(map, cell));
			}

			if (Tools.IsVisible(thing) == false)
				return false;

			var pawn = thing as Pawn;
			if (pawn == null)
				return true;

			if (pawn.Faction != Faction.OfPlayer)
				return true;

			return controller.IsInVisibleRange(pawn);
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
			if (Statics.circleCache.TryGetValue(radius, out var cells) == false)
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
				Statics.circleCache[radius] = cells;
			}
			return cells;
		}

		// draw mesh
		//
		public static void DrawMesh(Mesh mesh, Material mat, float matSizeX, float matSizeZ, Vector3 pos)
		{
			var matrix = default(Matrix4x4);
			matrix.SetTRS(pos, Quaternion.identity, new Vector3(matSizeX, 1f, matSizeZ));
			Graphics.DrawMesh(mesh, matrix, mat, 0);
		}

		// execute a callback for each cell in a circle
		//
		public static void DoInCircle(this Map map, IntVec3 center, float radius, Action<int, int> callback)
		{
			var len = map.Size.x;
			foreach (var vec in GetCircleVectors(radius))
			{
				var pos = center + vec;
				if (pos.x >= 0 && pos.z < len && pos.z >= 0 && pos.z < len)
					callback(pos.x, pos.z);
			}
		}

		// add a form caravan gizmo if any caravanable pawn is selected
		//
		public static void AddFormCaravanGizmo(List<Gizmo> list)
		{
			if (Find.Selector.SelectedObjects.All(obj =>
			{
				var pawn = obj as Pawn;
				if (pawn == null) return false;
				if (pawn.IsColonistPlayerControlled) return true;
				return pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer;
			}) == false) return;

			list.Add(new Command_Action
			{
				defaultLabel = "CommandFormCaravan".Translate(),
				defaultDesc = "CommandFormCaravanDesc".Translate(),
				icon = FormCaravanComp.FormCaravanCommand,
				hotKey = KeyBindingDefOf.Misc2,
				tutorTag = "FormCaravan",
				action = delegate () { Find.WindowStack.Add(new Dialog_FormCaravan(Find.CurrentMap, false, null, false)); }
			});
		}

		public static byte[] PackBoolsInByteArray(bool[] bools)
		{
			var len = bools.Length;
			var bytes = len >> 3;
			if ((len & 0x07) != 0) ++bytes;
			var result = new byte[bytes + 4];
			var lenBytes = BitConverter.GetBytes(len);
			Array.Copy(lenBytes, result, 4);
			for (var i = 0; i < bools.Length; i++)
				if (bools[i])
					result[4 + i >> 3] |= (byte)(1 << (i & 0x07));
			return result;
		}

		public static bool[] UnpackByteArrayInBools(byte[] bytes)
		{
			var len = BitConverter.ToInt32(bytes, 0);
			var result = new bool[len];

			return result;
		}

		// read bytes from file
		//
		public static byte[] LoadFile(string name, Func<byte[]> defaultValue)
		{
			var path = "-";
			try
			{
				var folder = Path.Combine(GenFilePaths.ConfigFolderPath, "RimBattle");
				Directory.CreateDirectory(folder);
				path = Path.Combine(folder, $"{name}.dat");
				return File.ReadAllBytes(path);
			}
#pragma warning disable CA1031
			catch (Exception e)
			{
				Log.Error($"Exception loading visibility file {path}: {e}");
				return defaultValue();
			}
#pragma warning restore CA1031
		}

		// save bytes to file
		//
		public static void SaveFile(string name, byte[] data)
		{
			var folder = Path.Combine(GenFilePaths.ConfigFolderPath, "RimBattle");
			Directory.CreateDirectory(folder);
			var path = Path.Combine(folder, $"{name}.dat");
			File.WriteAllBytes(path, data);
		}
	}
}