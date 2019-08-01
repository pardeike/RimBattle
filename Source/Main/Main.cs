using Harmony;
using System.Reflection;
using UnityEngine;
using Verse;

namespace RimBattle
{
	/* TODOs
	 * 
	 * Milestones
	 * - owned zones
	 * - spawning points
	 * - battle map design
	 * - ticket display
	 * 
	 * Patches
	 * - patch away designation icons (hunting, plant cutting, mining etc)
	 * - patch away selection and cursor of other teams
	 * - make colonists spawn with equipped weapons or let them auto equip
	 * - make a no-kill zone around the spawning point
	 * - tweak startup work schedule
	 * - tweak startup enemy response (not flee)
	 * - make destination draft position visible under fog
	 * - add extra save (& quit) button
	 * - add extra load button (redesign main page)
	 * - hide wildlife (in tab/table) when fogged
	 * - don't allow targeting enemies with a weapon when they are not visiblea
	 * 
	 */
	class Flags
	{
		public static bool fixMultiplayerNames = true;
		public static bool allTeamsOnFirstMap = true;
		public static bool startPaused = true;
	}

	[StaticConstructorOnStartup]
	class RimBattlePatches
	{
#pragma warning disable CA1810
		static RimBattlePatches()
		{
			// HarmonyInstance.DEBUG = true;
			var harmony = HarmonyInstance.Create("net.pardeike.rimworld.mod.rimbattle");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
#pragma warning restore CA1810
	}

	class RimBattleMod : Mod
	{
		public static RimBattleModSettings Settings;

		public RimBattleMod(ModContentPack content) : base(content)
		{
			Settings = GetSettings<RimBattleModSettings>();
			ToggleBattle.Patch();
			Multiplayer.Init();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			Settings.DoWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "RimBattle";
		}
	}
}