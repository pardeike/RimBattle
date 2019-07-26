using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;

namespace RimBattle
{
	using Instructions = IEnumerable<CodeInstruction>;

	// make different teams hostile to each other
	// - globally patch thing/thing method
	//
	[HarmonyPatch(typeof(GenHostility))]
	[HarmonyPatch(nameof(GenHostility.HostileTo))]
	[HarmonyPatch(new[] { typeof(Thing), typeof(Thing) })]
	static class GenHostility_HostileTo_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(Thing a, Thing b, ref bool __result)
		{
			if (a is Pawn p1 && b is Pawn p2)
				if (p1.IsColonist && p2.IsColonist)
				{
					__result = p1.GetTeamID() != p2.GetTeamID();
					return false;
				}
			return true;
		}
	}

	// make different teams hostile to each other
	// - globally patch IsActiveThreatToPlayer
	//
	[HarmonyPatch(typeof(GenHostility))]
	[HarmonyPatch(nameof(GenHostility.IsActiveThreatToPlayer))]
	static class GenHostility_IsActiveThreatToPlayer_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(IAttackTarget target, ref bool __result)
		{
			if (target is Pawn pawn && pawn.IsColonist && Ref.controller.InMyTeam(pawn) == false)
			{
				__result = true;
				return false;
			}
			return true;
		}
	}

	// make different teams hostile to each other
	// - globally patch CausesTimeSlowdown
	//
	[HarmonyPatch(typeof(Verb))]
	[HarmonyPatch("CausesTimeSlowdown")]
	static class Verb_CausesTimeSlowdown_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(Verb __instance, LocalTargetInfo castTarg, ref bool __result)
		{
			if (castTarg.HasThing)
				if (__instance.caster is Pawn p1 && castTarg.Thing is Pawn p2)
					if (p1.IsColonist && p2.IsColonist)
					{
						__result = p1.GetTeamID() != p2.GetTeamID();
						return false;
					}
			return true;
		}
	}

	// make different teams hostile to each other
	// - globally patch AnyHostileActiveThreatTo
	//
	[HarmonyPatch(typeof(GenHostility))]
	[HarmonyPatch(nameof(GenHostility.AnyHostileActiveThreatTo))]
	static class GenHostility_AnyHostileActiveThreatTo_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(Map map, ref bool __result)
		{
			if (__result)
				return;
			if (map.mapPawns.AllPawns.Any(pawn => pawn.IsColonist && Ref.controller.InMyTeam(pawn) == false))
				__result = true;
		}
	}

	// make different teams hostile to each other
	// - globally patch GetPotentialTargetsFor
	//
	[HarmonyPatch(typeof(AttackTargetsCache))]
	[HarmonyPatch(nameof(AttackTargetsCache.GetPotentialTargetsFor))]
	static class AttackTargetsCache_GetPotentialTargetsFor_Patch
	{
		[HarmonyPriority(10000)]
		static void Postfix(IAttackTargetSearcher th, ref List<IAttackTarget> __result)
		{
			var pawn = th.Thing as Pawn;
			if (pawn == null || pawn.IsColonist == false)
				return;
			var pawnTeam = pawn.GetTeamID();
			__result.AddRange(pawn.Map.mapPawns.AllPawns.Where(p => p.IsColonist && p.GetTeamID() != pawnTeam).Cast<IAttackTarget>());
		}
	}

	// make different teams hostile to each other
	// - handle indiviual cases
	//
	[HarmonyPatch]
	static class Hostility_MultiPatches
	{
		// replace from

		static readonly MethodInfo m_HostileTo = SymbolExtensions.GetMethodInfo(() => GenHostility.HostileTo(default, default(Faction)));
		static readonly MethodInfo m_IsActiveThreatToPlayer = SymbolExtensions.GetMethodInfo(() => GenHostility.IsActiveThreatToPlayer(default));
		//static readonly MethodInfo m_IsActiveThreatTo = SymbolExtensions.GetMethodInfo(() => GenHostility.IsActiveThreatTo(default, default));
		static readonly MethodInfo m_ForAttackHostile = SymbolExtensions.GetMethodInfo(() => TargetingParameters.ForAttackHostile());
		static readonly Type t_GenAI_Inner_InDangerousCombat = AccessTools.FirstInner(typeof(GenAI), t => t.Name.Contains("InDangerousCombat"));

		// replace to

		static readonly MethodInfo m_AlwaysHostileTo = SymbolExtensions.GetMethodInfo(() => AlwaysHostileTo(default, default));
		static bool AlwaysHostileTo(Thing thing, Faction faction)
		{
			_ = thing; _ = faction;
			return true;
		}

		static readonly MethodInfo m_MyHostileTo = SymbolExtensions.GetMethodInfo(() => MyHostileTo(default, default, default));
		static bool MyHostileTo(Thing thing, Faction faction, Thing extraThing)
		{
			if (faction == Faction.OfPlayer)
				if (thing is Pawn p1 && extraThing is Pawn p2)
					if (p1.IsColonist && p2.IsColonist)
						return p1.GetTeamID() != p2.GetTeamID();
			return thing.HostileTo(faction);
		}

		static readonly MethodInfo m_MyIsActiveThreatToPlayer = SymbolExtensions.GetMethodInfo(() => MyIsActiveThreatToPlayer(default, default));
		static bool MyIsActiveThreatToPlayer(IAttackTarget target, Thing extraThing)
		{
			if (target.Thing is Pawn p1 && extraThing is Pawn p2)
				if (p1.IsColonist && p2.IsColonist)
					return p1.GetTeamID() != p2.GetTeamID();
			return GenHostility.IsActiveThreatToPlayer(target);
		}

		/*static readonly MethodInfo m_MyIsActiveThreatTo = SymbolExtensions.GetMethodInfo(() => MyIsActiveThreatTo(default, default, null));
		static bool MyIsActiveThreatTo(IAttackTarget target, Faction faction, Thing extraThing)
		{
			if (target.Thing is Pawn p1 && extraThing is Pawn p2)
				if (p1.IsColonist && p2.IsColonist)
					return p1.GetTeamID() != p2.GetTeamID();
			return GenHostility.IsActiveThreatTo(target, faction);
		}*/

		static readonly MethodInfo m_MyForAttackHostile = SymbolExtensions.GetMethodInfo(() => MyForAttackHostile());
		static TargetingParameters MyForAttackHostile()
		{
			var targetingParams = TargetingParameters.ForAttackHostile();
			targetingParams.validator = delegate (TargetInfo targ)
			{
				var result = targetingParams.validator(targ);
				if (result) return true;
				return targ.HasThing && targ.Thing is Pawn p && p.IsColonist && Ref.controller.InMyTeam(p) == false;
			};
			return targetingParams;
		}

		// multi patching

		static readonly MultiPatches multiPatches = new MultiPatches(
			typeof(Hostility_MultiPatches),
			new MultiPatchInfo(
				AccessTools.Method(typeof(Faction), "IsMutuallyHostileCrossfire"),
				m_HostileTo, m_AlwaysHostileTo
			),
			new MultiPatchInfo(
				AccessTools.Method(typeof(FloatMenuMakerMap), "AddDraftedOrders"),
				m_ForAttackHostile, m_MyForAttackHostile
			),
			new MultiPatchInfo(
				AccessTools.Method(typeof(FloatMenuMakerMap), "AddDraftedOrders"),
				m_HostileTo, m_MyHostileTo,
				new CodeInstruction(OpCodes.Ldarg_1)
			),
			new MultiPatchInfo(
				AccessTools.Method(typeof(AutoUndrafter), "AnyHostilePreventingAutoUndraft"),
				m_IsActiveThreatToPlayer, m_MyIsActiveThreatToPlayer,
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(AutoUndrafter), "pawn"))
			),
			new MultiPatchInfo(
				AccessTools.Method(typeof(GenAI), nameof(GenAI.CanBeArrestedBy)),
				m_HostileTo, m_MyHostileTo,
				new CodeInstruction(OpCodes.Ldarg_1)
			),
			new MultiPatchInfo(
				AccessTools.Method(t_GenAI_Inner_InDangerousCombat, "<>m__2"),
				m_HostileTo, m_MyHostileTo,
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(t_GenAI_Inner_InDangerousCombat, "pawn"))
			)
		);

		static IEnumerable<MethodBase> TargetMethods()
		{
			return multiPatches.TargetMethods();
		}

		[HarmonyPriority(10000)]
		static Instructions Transpiler(MethodBase original, Instructions instructions)
		{
			return multiPatches.Transpile(original, instructions);
		}
	}
}