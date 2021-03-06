﻿using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimBattle
{
	class ConfigGUI
	{
		static Rect GetMainRect(Rect rect)
		{
			var num = 45f;
			return new Rect(0f, num, rect.width, rect.height - 38f - num - 17f);
		}

		static void DrawExtraTitle(Rect rect, string title)
		{
			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(rect.width / 2, 0f, rect.width / 2, 45f), title);
			Text.Font = GameFont.Small;
		}

		public static void DoWindowContents(Rect rect)
		{
			DrawExtraTitle(rect, "RimBattle");

			rect.xMin += rect.width / 2;
			rect.yMin += 45f;

			var list = new Listing_Standard();
			list.Begin(rect);

			list.Label($"Number of teams: {Ref.controller.teamCount}");
			Ref.controller.teamCount = (int)list.Slider(Ref.controller.teamCount, 2, 7);
			Ref.controller.tileCount = Math.Max(Ref.controller.tileCount, Ref.controller.teamCount);
			list.Gap(10);

			Text.Font = GameFont.Small;
			list.Label($"Number of map tiles: {Ref.controller.tileCount}");
			list.Gap(2);
			var r = list.GetRect(64);
			r.width = 64;
			for (var tileCount = 2; tileCount <= 7; tileCount++)
			{
				if (tileCount < Ref.controller.teamCount)
				{
					GUI.color = new Color(1f, 1f, 1f, 0.2f);
					GUI.DrawTexture(r, Statics.Configs[tileCount - 1], ScaleMode.ScaleToFit);
				}
				else
				{
					GUI.color = new Color(1f, 1f, 1f, Mouse.IsOver(r) ? 0.5f : 1f);
					GUI.DrawTexture(r, Statics.Configs[tileCount - 1], ScaleMode.ScaleToFit);
					var tileIndicies = Tools.TeamTiles(tileCount, Ref.controller.teamCount);
					for (var i = 0; i < tileIndicies.Length; i++)
					{
						GUI.color = Ref.TeamColors[i];
						GUI.DrawTexture(r, Statics.Teams[tileIndicies[i]], ScaleMode.ScaleToFit);
					}
					if (tileCount == Ref.controller.tileCount)
					{
						GUI.color = Color.yellow;
						Widgets.DrawBox(r, 1);
					}
					if (Widgets.ButtonInvisible(r, true))
					{
						Ref.controller.tileCount = tileCount;
						Ref.controller.teamCount = Math.Min(Ref.controller.teamCount, Ref.controller.tileCount);
						SoundDefOf.Click.PlayOneShotOnCamera(null);
					}
				}
				r.position += new Vector2(rect.width / 6, 0);
			}
			GUI.color = Color.white;
			list.Gap(18);

			list.Label($"Total respawns (tickets): {Ref.controller.totalTickets}");
			Ref.controller.totalTickets = (int)list.Slider(Ref.controller.totalTickets, 0, 100);
			list.Gap(14);

			var quadrumFactor = 15f * 24f;
			list.Label($"Maximum match time: {Tools.TranslateHoursToText(Ref.controller.maxQuadrums * quadrumFactor)}");
			var newValue = (int)list.Slider(Ref.controller.maxQuadrums * quadrumFactor, quadrumFactor, 60f * quadrumFactor);
			Ref.controller.maxQuadrums = (int)Math.Round(newValue / quadrumFactor, MidpointRounding.ToEven);
			list.Gap(14);

			list.End();
		}
	}
}