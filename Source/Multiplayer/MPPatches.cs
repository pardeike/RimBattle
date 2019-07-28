﻿using Harmony;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace RimBattle
{
	using Instructions = IEnumerable<CodeInstruction>;

	[HarmonyPatch]
	class HostWindow_PostClose_Patch
	{
		static MethodBase TargetMethod()
		{
			return AccessTools.Method("Multiplayer.Client.HostWindow:PostClose");
		}

		static void Postfix()
		{
			Ref.controller.MultiplayerEstablished(true);
		}
	}

	[HarmonyPatch]
	class ClientJoiningState_PostLoad_Patch
	{
		static MethodBase TargetMethod()
		{
			return AccessTools.Method("Multiplayer.Client.ClientJoiningState:PostLoad");
		}

		static void Postfix()
		{
			Ref.controller.MultiplayerEstablished(false);
		}
	}

	[HarmonyPatch]
	class MPConnect_Events_Patch
	{
		static MethodBase TargetMethod()
		{
			return AccessTools.Method("Multiplayer.Client.ClientPlayingState:HandlePlayerList");
		}

		static readonly MethodInfo m_Connect = SymbolExtensions.GetMethodInfo(() => Connect(null));
		static void Connect(object obj)
		{
			var player = new PlayerInfo(obj);
			if (player.type != MPPlayerType.Arbiter)
			{
				Multiplayer.players.Add(player);
				Multiplayer.dispatcher.Send(MPEventType.Connect, player);
				Multiplayer.dispatcher.Send(MPEventType.ListChanged, player);
			}
		}

		static readonly MethodInfo m_Disconnect = SymbolExtensions.GetMethodInfo(() => Disconnect(0));
		static void Disconnect(int id)
		{
			var player = Multiplayer.players.FirstOrDefault(p => p.id == id);
			if (player.type != MPPlayerType.Arbiter)
			{
				Multiplayer.players.Remove(player);
				Multiplayer.dispatcher.Send(MPEventType.Disconnect, player);
				Multiplayer.dispatcher.Send(MPEventType.ListChanged, player);
			}
		}

		static Instructions Transpiler(Instructions codes)
		{
			var m_Read = MPTools.Method("Client.PlayerInfo", "Read");
			var m_ReadInt32 = MPTools.Method("Common.ByteReader", "ReadInt32");
			var firstTime = true;
			foreach (var code in codes)
			{
				yield return code;
				if (code.opcode == OpCodes.Call && code.operand == m_Read)
				{
					yield return new CodeInstruction(OpCodes.Dup);
					yield return new CodeInstruction(OpCodes.Call, m_Connect);
				}
				if (firstTime && (code.opcode == OpCodes.Call || code.opcode == OpCodes.Callvirt) && code.operand == m_ReadInt32)
				{
					yield return new CodeInstruction(OpCodes.Dup);
					yield return new CodeInstruction(OpCodes.Call, m_Disconnect);
					firstTime = false;
				}
			}
		}
	}
}