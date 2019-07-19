using Harmony;
using RimWorld;
using System.Reflection;
using UnityEngine;
using Verse;

namespace RimBattle
{
	class RimBattleMod : Mod
	{
		public static RimBattleModSettings Settings;

		public RimBattleMod(ModContentPack content) : base(content)
		{
			Settings = GetSettings<RimBattleModSettings>();

			//HarmonyInstance.DEBUG = true;
			var harmony = HarmonyInstance.Create("net.pardeike.rimworld.mod.rimbattle");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
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