using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static Harmony.AccessTools;

namespace RimBattle
{
	[StaticConstructorOnStartup]
	static class Refs
	{
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

		public static GameController controller;
		public static readonly int forceMapSize = 120;
		public static readonly int startTickets = 100;

		public static readonly int defaultVisibleRange = 6;
		public static readonly Dictionary<float, HashSet<IntVec3>> circleCache = new Dictionary<float, HashSet<IntVec3>>();
		public static readonly Dictionary<Pawn, Team> teamMemberCache = new Dictionary<Pawn, Team>();

		public static readonly FieldRef<SectionLayer, Section> section = FieldRefAccess<SectionLayer, Section>("section");
		public static readonly FieldRef<FogGrid, Map> map = FieldRefAccess<FogGrid, Map>("map");
		public static readonly FieldRef<ThingWithComps, List<ThingComp>> comps = FieldRefAccess<ThingWithComps, List<ThingComp>>("comps");

		public static readonly string[] tileNames = new string[] { "Center", "Right", "TopRight", "TopLeft", "Left", "BottomLeft", "BottomRight" };

		public static readonly Color notVisibleColor = new ColorInt(32, 32, 32).ToColor;
		public static readonly Color fogColor = new ColorInt(61, 53, 51).ToColor;
		public static readonly Color edificeColor = new ColorInt(113, 109, 93).ToColor;
		public static readonly Color waterColor = new ColorInt(45, 96, 167).ToColor;
		public static readonly Color plantColor = new ColorInt(79, 79, 31).ToColor;
		public static readonly Color groundColor = new ColorInt(100, 77, 58).ToColor;
		public static readonly Color mountainColor = new ColorInt(58, 63, 63).ToColor;
		public static readonly Color animalColor = new ColorInt(128, 128, 128).ToColor;

		public static readonly Material UndiscovereddMat = MaterialPool.MatFrom("Undiscovered", ShaderDatabase.MoteGlow);
		public static readonly Material MouseTileError = MaterialPool.MatFrom("MouseTileError", ShaderDatabase.WorldOverlayAdditive, 3560);
		public static readonly Material SelectedTileError = MaterialPool.MatFrom("SelectedTileError", ShaderDatabase.WorldOverlayAdditive, 3560);
		public static readonly Texture2D[] Configs = Tools.GetTextures("Tiles/Config#", 1, 7);
		public static readonly Texture2D[] Teams = Tools.GetTextures("Tiles/Team#", 1, 7);
		public static readonly Texture2D TeamIdInner = ContentFinder<Texture2D>.Get("TeamIdInner");
		public static readonly Texture2D TeamIdOuter = ContentFinder<Texture2D>.Get("TeamIdOuter");

		public static readonly MainButtonDef Battle = new MainButtonDef()
		{
			defName = "Battle",
			label = "battle",
			description = "Shows the main battle overview with its 7 maps and possible spawns.",
			workerClass = typeof(MainButtonWorker_ToggleBattle),
			order = 100,
			defaultHotKey = KeyCode.F12,
			validWithoutMap = true
		};
	}

	public class Keys
	{
		public static readonly KeyBindingDef BattleMap = new KeyBindingDef()
		{
			label = "Toggle battle map tab",
			defName = "MainTab_Battle",
			category = KeyBindingCategoryDefOf.MainTabs,
			defaultKeyCodeA = KeyCode.Tab,
			defaultKeyCodeB = KeyCode.BackQuote,
			modContentPack = MainButtonDefOf.Architect.modContentPack
		};
	}
}