﻿using Multiplayer.API;
using RimWorld;
using System.Linq;
using Verse;
using Verse.Profile;

namespace RimBattle
{
	class GameState
	{
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
			if (Ref.forceMapSize > 0)
				game.InitData.mapSize = Ref.forceMapSize;

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

				_ = Ref.parts(Find.Scenario).RemoveAll(part => part is ScenPart_GameStartDialog);
				var arrivalMethod = Find.Scenario.AllParts.OfType<ScenPart_PlayerPawnsArriveMethod>().First();
				Ref.method(arrivalMethod) = PlayerPawnsArriveMethod.Standing;

				var allTiles = Ref.controller.tiles;
				var allTileIndices = Tools.TeamTiles(Ref.controller.tileCount, Ref.controller.tileCount);
				var teamTileIndices = Tools.TeamTiles(Ref.controller.tileCount, Ref.controller.teamCount);
				var skipTeamsOnRemainingMaps = false;
				for (var i = 0; i < allTiles.Count; i++)
				{
					var tile = allTiles[i];
					var tileIndex = allTileIndices[i];
					var hasTeam = teamTileIndices.Contains(tileIndex);

					Find.GameInitData.startingAndOptionalPawns.Clear();
					Team team = null;
					if (hasTeam && skipTeamsOnRemainingMaps == false)
					{
						if (Flags.allTeamsOnFirstMap)
						{
							for (var j = 0; j < Ref.controller.teamCount; j++)
							{
								team = Ref.controller.CreateTeam();
								Tools.AddNewColonistsToTeam(team);
							}
							skipTeamsOnRemainingMaps = true;
						}
						else
						{
							team = Ref.controller.CreateTeam();
							Tools.AddNewColonistsToTeam(team);
						}
					}

					var settlement = Tools.CreateSettlement(tile);
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
				Tools.NameSettlements();

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

		public static void StartMultiplayer()
		{
			var hostWindow = Multiplayer.GetHostWindow();
			Find.WindowStack.Add(hostWindow);

			foreach (var tile in Ref.controller.tiles)
				MPTools.SetCurrentSpeed(tile, 0);
		}

		public static void ConnectPlayers()
		{
			var idx = Ref.controller.teamChoices.IndexOf(MP.PlayerName);
			if (idx >= 0)
			{
				Ref.controller.Team = idx;
				Ref.controller.JoinTeam(idx);
				Find.ColonistBar.MarkColonistsDirty();

				PlayerConnectDialog.hideColonistBar = false;
				Ref.controller.CurrentTeam.SetSpeed(Find.CurrentMap.Tile, Flags.startPaused ? 0 : 1);

				return;
			}

			Find.MusicManagerPlay.disabled = true;
			Find.WindowStack.Notify_GameStartDialogOpened();

			var dialog = new PlayerConnectDialog();
			Find.WindowStack.Add(dialog);
		}
	}
}