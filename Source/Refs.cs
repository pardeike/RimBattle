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

		public static readonly Texture2D MapTexture = new Texture2D(255, 255, TextureFormat.RGB24, true);
		public static readonly int mapRes = 2;
		public static readonly bool showPlants = true;
		public static readonly Color edificeColor = Color.grey;
		public static readonly Color PrisonerColor = Color.yellow;
		public static readonly Color HostilesColor = Color.red;
		public static readonly Color AnimalColor = Color.green;
		public static readonly Color ColonistColor = Color.cyan;
		public static readonly Color FriendliesColor = Color.blue;
		public static readonly Color EdificeColor = Color.grey;
		public static readonly Color GroundColor = new ColorInt(7, 8, 13).ToColor;
		public static readonly Color WaterColor = new ColorInt(21, 63, 73).ToColor;
		public static readonly Color FogColor = new ColorInt(20, 20, 20).ToColor;
		public static readonly Color PlantColor = new Color(0.322f, 0.408f, 0.322f);

		public static readonly Material UndiscovereddMat = MaterialPool.MatFrom("Undiscovered", ShaderDatabase.MoteGlow);
		public static readonly Material MouseTileError = MaterialPool.MatFrom("MouseTileError", ShaderDatabase.WorldOverlayAdditive, 3560);
		public static readonly Material SelectedTileError = MaterialPool.MatFrom("SelectedTileError", ShaderDatabase.WorldOverlayAdditive, 3560);
		public static readonly int startTickets = 100;

		public static readonly FieldRef<ThingWithComps, List<ThingComp>> comps = FieldRefAccess<ThingWithComps, List<ThingComp>>("comps");
	}
}