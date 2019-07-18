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
		public static GameController controller;
		public static readonly int forceMapSize = 120;

		public static readonly int defaultVisibleRange = 6;
		public static readonly Dictionary<float, HashSet<IntVec3>> circleCache = new Dictionary<float, HashSet<IntVec3>>();
		public static readonly Dictionary<Pawn, Team> teamMemberCache = new Dictionary<Pawn, Team>();
		public static readonly FieldRef<SectionLayer, Section> sectionRef = FieldRefAccess<SectionLayer, Section>("section");
		public static readonly string[] tileNames = new string[] { "Center", "Right", "TopRight", "TopLeft", "Left", "BottomLeft", "BottomRight" };
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

		public static readonly Color notVisibleColor = new ColorInt(32, 32, 32).ToColor;
		public static readonly Color fogColor = new ColorInt(61, 53, 51).ToColor;
		public static readonly Color edificeColor = new ColorInt(113, 109, 93).ToColor;
		public static readonly Color waterColor = new ColorInt(45, 96, 167).ToColor;
		public static readonly Color plantColor = new ColorInt(79, 79, 31).ToColor;
		public static readonly Color groundColor = new ColorInt(100, 77, 58).ToColor;
		public static readonly Color mountainColor = new ColorInt(58, 63, 63).ToColor;
		public static readonly Color animalColor = new ColorInt(128, 128, 128).ToColor;

		public static readonly Texture2D MiniMapBG = ContentFinder<Texture2D>.Get("MiniMapBG", true);
		public static readonly Material UndiscovereddMat = MaterialPool.MatFrom("Undiscovered", ShaderDatabase.MoteGlow);
		public static readonly Material MouseTileError = MaterialPool.MatFrom("MouseTileError", ShaderDatabase.WorldOverlayAdditive, 3560);
		public static readonly Material SelectedTileError = MaterialPool.MatFrom("SelectedTileError", ShaderDatabase.WorldOverlayAdditive, 3560);
		public static readonly int startTickets = 100;

		public static readonly FieldRef<ThingWithComps, List<ThingComp>> comps = FieldRefAccess<ThingWithComps, List<ThingComp>>("comps");
	}
}