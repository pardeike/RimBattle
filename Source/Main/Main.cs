using Harmony;
using System.Reflection;
using UnityEngine;
using Verse;

namespace RimBattle
{
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