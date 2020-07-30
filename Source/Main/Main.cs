using Harmony;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace RimBattle
{
	/* TODOs
	 * 
	 * Milestones
	 * - autosave destroys team assignments
	 * - cancel in team select and new try will not reset connect count
	 * - spawning points
	 * - battle map design
	 * - ticket display
	 * 
	 * Patches
	 * - make colonist color markers not show in fog
	 * - allow for overlapping zones
	 * - don't take reserved work from others (same cell)
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
		public static readonly bool fixMultiplayerNames = true;
		public static readonly bool allTeamsOnFirstMap = true;
		public static readonly bool startPaused = true;
		public static readonly bool unfogEverything = false;
		public static readonly HostilityResponseMode defaultHostilityResponse = HostilityResponseMode.Ignore;
		public static readonly HashSet<SkillDef> setMinimumSkills = new HashSet<SkillDef>
		{
			SkillDefOf.Construction,
			SkillDefOf.Plants,
			SkillDefOf.Intellectual,
			SkillDefOf.Mining,
			SkillDefOf.Shooting,
			SkillDefOf.Melee,
			SkillDefOf.Social,
			SkillDefOf.Animals,
			SkillDefOf.Cooking,
			SkillDefOf.Medicine,
			SkillDefOf.Artistic,
			SkillDefOf.Crafting
		};
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