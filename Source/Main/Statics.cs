using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static Harmony.AccessTools;

namespace RimBattle
{
	class Ref
	{
		public static GameController controller;

		// ---#3#-#2#---
		// -#4#-#0#-#1#-
		// ---#5#-#6#---

		public static Color[] TeamColors => new Color[]
		{
			new Color(255, 000, 000),
			new Color(000, 000, 255),
			new Color(255, 000, 255),
			new Color(000, 255, 000),
			new Color(255, 255, 000),
			new Color(000, 255, 255),
			new Color(128, 000, 255),
		};

		public static readonly int[][][] teamTiles = new int[][][]
		{
			new [] // 1 tile
			{
				new[] { 0 }, // 1 team
			},
			new [] // 2 tiles
			{
				new[] { 0, 1 }, // 2 teams
			},
			new [] // 3 tiles
			{
				new[] { 4, 1 }, // 2 teams
				new[] { 4, 0, 1 }, // 3 teams
			},
			new [] // 4 tiles
			{
				new[] { 3, 1 }, // 2 teams
				new[] { 3, 2, 1 }, // 3 teams
				new[] { 3, 2, 0, 1 }, // 4 teams
			},
			new [] // 5 tiles
			{
				new[] { 3, 6 }, // 2 teams
				new[] { 3, 0, 6 }, // 3 teams
				new[] { 3, 2, 5, 6 }, // 4 teams
				new[] { 3, 2, 0, 5, 6 }, // 5 teams
			},
			new [] // 6 tiles
			{
				new[] { 3, 6 }, // 2 teams
				new[] { 3, 1, 5 }, // 3 teams
				new[] { 3, 2, 5, 6 }, // 4 teams
				new[] { 3, 2, 1, 5, 6 }, // 5 teams
				new[] { 3, 2, 0, 1, 5, 6 }, // 6 teams
			},
			new [] // 7 tiles
			{
				new[] { 4, 1 }, // 2 teams
				new[] { 3, 1, 5 }, // 3 teams
				new[] { 3, 2, 5, 6 }, // 4 teams
				new[] { 3, 2, 0, 5, 6 }, // 5 teams
				new[] { 3, 2, 4, 1, 5, 6 }, // 6 teams
				new[] { 3,2, 4, 0, 1, 5, 6 }, // 7 teams
			},
		};

		public static readonly int[][] adjactedTiles = new int[][]
		{
			new[] { 1, 2, 3, 4, 5, 6 },
			new[] { 0, 2, 6 },
			new[] { 0, 1, 3 },
			new[] { 0, 2, 4 },
			new[] { 0, 3, 5 },
			new[] { 0, 4, 6 },
			new[] { 0, 1, 5 },
		};

		public const int forceMapSize = 75;
		public const int startTickets = 100;

		public const int defaultVisibleRange = 6;
		public static readonly Dictionary<float, HashSet<IntVec3>> circleCache = new Dictionary<float, HashSet<IntVec3>>();

		public static readonly string[] tileNames = new string[] { "Center", "Right", "TopRight", "TopLeft", "Left", "BottomLeft", "BottomRight" };

		public static readonly FieldRef<Scenario, List<ScenPart>> parts = FieldRefAccess<Scenario, List<ScenPart>>("parts");
		public static readonly FieldRef<ScenPart_PlayerPawnsArriveMethod, PlayerPawnsArriveMethod> method = FieldRefAccess<ScenPart_PlayerPawnsArriveMethod, PlayerPawnsArriveMethod>("method");
		public static readonly FieldRef<Pawn_PlayerSettings, Pawn> master = FieldRefAccess<Pawn_PlayerSettings, Pawn>("master");
		public static readonly FieldRef<SectionLayer, Section> SectionLayer_section = FieldRefAccess<SectionLayer, Section>("section");
		public static readonly FieldRef<FogGrid, Map> map = FieldRefAccess<FogGrid, Map>("map");
		public static readonly FieldRef<MapDrawer, Section[,]> sections = FieldRefAccess<MapDrawer, Section[,]>("sections");
		public static readonly FieldRef<ThingWithComps, List<ThingComp>> comps = FieldRefAccess<ThingWithComps, List<ThingComp>>("comps");
		public static readonly FieldRef<TransferableOneWayWidget, List<object>> TransferableOneWayWidget_sections = FieldRefAccess<TransferableOneWayWidget, List<object>>("sections");
		public static readonly FieldRef<Dialog_FormCaravan, bool> Dialog_FormCaravan_canChooseRoute = FieldRefAccess<Dialog_FormCaravan, bool>("canChooseRoute");
		public static readonly FieldRef<Dialog_FormCaravan, Map> Dialog_FormCaravan_map = FieldRefAccess<Dialog_FormCaravan, Map>("map");
		public static readonly FieldRef<Dialog_FormCaravan, int> Dialog_FormCaravan_startingTile = FieldRefAccess<Dialog_FormCaravan, int>("startingTile");
		public static readonly FieldRef<Dialog_FormCaravan, int> Dialog_FormCaravan_destinationTile = FieldRefAccess<Dialog_FormCaravan, int>("destinationTile");
		public static readonly FieldRef<LordJob_FormAndSendCaravan, int> LordJob_FormAndSendCaravan_startingTile = FieldRefAccess<LordJob_FormAndSendCaravan, int>("startingTile");
		public static readonly FieldRef<LordJob_FormAndSendCaravan, int> LordJob_FormAndSendCaravan_destinationTile = FieldRefAccess<LordJob_FormAndSendCaravan, int>("destinationTile");
		public static readonly FieldRef<LordJob_FormAndSendCaravan, IntVec3> LordJob_FormAndSendCaravan_exitSpot = FieldRefAccess<LordJob_FormAndSendCaravan, IntVec3>("exitSpot");

		public static FastInvokeHandler PlaySoundOf = MethodInvoker.GetHandler(Method(typeof(TimeControls), "PlaySoundOf"));
		public static readonly TimeSpeed[] CachedTimeSpeedValues = (TimeSpeed[])Enum.GetValues(typeof(TimeSpeed));

		public static readonly Color notVisibleColor = new ColorInt(32, 32, 32).ToColor;
		public static readonly Color fogColor = new ColorInt(61, 53, 51).ToColor;
		public static readonly Color edificeColor = new ColorInt(113, 109, 93).ToColor;
		public static readonly Color waterColor = new ColorInt(45, 96, 167).ToColor;
		public static readonly Color plantColor = new ColorInt(79, 79, 31).ToColor;
		public static readonly Color groundColor = new ColorInt(100, 77, 58).ToColor;
		public static readonly Color mountainColor = new ColorInt(58, 63, 63).ToColor;
		public static readonly Color animalColor = new ColorInt(128, 128, 128).ToColor;
	}

	[StaticConstructorOnStartup]
	class Statics
	{
		public static readonly Material MouseTileError = MaterialPool.MatFrom("MouseTileError", ShaderDatabase.WorldOverlayAdditive, 3560);
		public static readonly Material SelectedTileError = MaterialPool.MatFrom("SelectedTileError", ShaderDatabase.WorldOverlayAdditive, 3560);
		public static readonly Material[] Badges = Tools.GetMaterials("Badges/Badge#", 0, 6, ShaderDatabase.MetaOverlay);
		public static readonly Material BadgeShadow = MaterialPool.MatFrom("Badges/Shadow", ShaderDatabase.MetaOverlay);
		public static readonly Texture2D[] Configs = Tools.GetTextures("Tiles/Config#", 1, 7);
		public static readonly Texture2D[] Teams = Tools.GetTextures("Tiles/Team#", 1, 7);
		public static readonly Material[] OwnedBy = Tools.GetMaterials("OwnedBy/OwnedBy#", 0, 6, ShaderDatabase.MetaOverlay);
		public static readonly Material OwnedByShadow = MaterialPool.MatFrom("OwnedBy/Shadow", ShaderDatabase.MetaOverlay);

		public static readonly Texture2D ButtonBGAtlas = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBG", true);
		public static readonly Texture2D ButtonBGAtlasMouseover = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGMouseover", true);
		public static readonly Texture2D ButtonBGAtlasClick = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGClick", true);
	}
}

// adjacted tile angles, 0-5 (9=not adjacted) as a counter-clockwise index (from right)
// for example (0,1) => 0 and (4,5) => 5
/*
public static readonly int[][] adjactedTileAngles = new int[][]
{
	new[] { 9, 0, 1, 2, 3, 4, 5 },
	new[] { 3, 9, 2, 9, 9, 9, 4 },
	new[] { 4, 5, 9, 3, 9, 9, 9 },
	new[] { 5, 9, 0, 9, 4, 9, 9 },
	new[] { 0, 9, 9, 1, 9, 5, 9 },
	new[] { 1, 9, 9, 9, 2, 9, 0 },
	new[] { 2, 1, 9, 9, 9, 3, 9 },
};*/
