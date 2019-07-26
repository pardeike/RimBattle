using Harmony;
using Multiplayer.API;
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
			HarmonyInstance.DEBUG = true;
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

			if (MP.enabled)
				MP.RegisterAll();
			else
				Log.Error("RimBattle needs Multiplayer to be enabled!");
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