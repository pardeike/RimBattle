using Harmony;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace RimBattle
{
	// getting our own OnGUI
	//
	[HarmonyPatch(typeof(UIRoot))]
	[HarmonyPatch(nameof(UIRoot.UIRootOnGUI))]
	class UIRoot_UIRootOnGUI_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix()
		{
			Ref.controller?.OnGUI();
		}
	}

	// hide colonist bar if battle map is showing
	//
	[HarmonyPatch(typeof(ColonistBar))]
	[HarmonyPatch(nameof(ColonistBar.ColonistBarOnGUI))]
	class ColonistBar_ColonistBarOnGUI_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix()
		{
			return (Ref.controller?.battleOverview?.showing ?? true) == false;
		}
	}

	// hide pawn labels if battle map is showing
	//
	[HarmonyPatch(typeof(GenMapUI))]
	[HarmonyPatch("DrawPawnLabel")]
	[HarmonyPatch(new[] { typeof(Pawn), typeof(Rect), typeof(float), typeof(float), typeof(Dictionary<string, string>), typeof(GameFont), typeof(bool), typeof(bool) })]
	class GenMapUI_DrawPawnLabel_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix()
		{
			return Ref.controller.battleOverview.showing == false;
		}
	}

	// remove World tab
	//
	[HarmonyPatch(typeof(MainButtonsRoot))]
	[HarmonyPatch(MethodType.Constructor)]
	class MainButtonsRoot_Constructor_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(List<MainButtonDef> ___allButtonsInOrder)
		{
			MainButtonDefOf.World.hotKey = null;
			___allButtonsInOrder.Remove(MainButtonDefOf.World);
		}
	}

	// tweak stats 1
	//
	[HarmonyPatch(typeof(StatExtension))]
	[HarmonyPatch(nameof(StatExtension.GetStatValue))]
	[HarmonyPatch(new[] { typeof(Thing), typeof(StatDef), typeof(bool) })]
	class StatExtension_GetStatValue_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(ref float __result, Thing thing, StatDef stat)
		{
			Tools.TweakStat(thing, stat, ref __result);
		}
	}

	// tweak stats 2
	//
	[HarmonyPatch(typeof(StatExtension))]
	[HarmonyPatch(nameof(StatExtension.GetStatValueAbstract))]
	[HarmonyPatch(new[] { typeof(BuildableDef), typeof(StatDef), typeof(ThingDef) })]
	class StatExtension_GetStatValueAbstract_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(ref float __result, StatDef stat)
		{
			Tools.TweakStat(null, stat, ref __result);
		}
	}

	// auto taming animals (not training)
	//
	[HarmonyPatch(typeof(InteractionWorker_RecruitAttempt))]
	[HarmonyPatch(nameof(InteractionWorker_RecruitAttempt.DoRecruit))]
	[HarmonyPatch(new[] { typeof(Pawn), typeof(Pawn), typeof(float), typeof(string), typeof(string), typeof(bool), typeof(bool) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out, ArgumentType.Normal, ArgumentType.Normal })]
	class InteractionWorker_RecruitAttempt_DoRecruit_Patch
	{
		[HarmonyPriority(10000)]
		static void Prefix(ref float recruitChance)
		{
			recruitChance = 1f;
		}
	}

	// auto master of tamed animals
	//
	[HarmonyPatch(typeof(RelationsUtility))]
	[HarmonyPatch(nameof(RelationsUtility.TryDevelopBondRelation))]
	class RelationsUtility_TryDevelopBondRelation_Patch
	{
		/* we could make this 100% bonding
		 * 
		[HarmonyPriority(10000)]
		static void Prefix(ref float baseChance)
		{
			baseChance = 1f;
		}*/

		[HarmonyPriority(10000)]
		static void Postfix(bool __result, Pawn humanlike, Pawn animal)
		{
			if (__result)
				Ref.master(animal.playerSettings) = humanlike;
		}
	}

	// allow anyone to build
	//
	[HarmonyPatch(typeof(GenConstruct))]
	[HarmonyPatch(nameof(GenConstruct.CanConstruct))]
	class GenConstruct_CanConstruct_Patch
	{
		[HarmonyPriority(10000)]
		static void Prefix(ref bool checkConstructionSkill)
		{
			checkConstructionSkill = false;
		}
	}

	// allow anyone to craft
	//
	[HarmonyPatch]
	class RecipeDef_PawnSatisfiesSkillRequirements_Patch
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return SymbolExtensions.GetMethodInfo(() => new RecipeDef().PawnSatisfiesSkillRequirements(null));
			yield return SymbolExtensions.GetMethodInfo(() => new SkillRequirement().PawnSatisfies(null));
		}

		[HarmonyPriority(10000)]
		static bool Prefix(ref bool __result)
		{
			__result = true;
			return false;
		}
	}
}