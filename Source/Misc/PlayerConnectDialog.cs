using Multiplayer.API;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace RimBattle
{
	class PlayerConnectDialog : Window
	{
		const float size = 128f;
		const float innerMargin = 20f;
		readonly int cols;
		readonly int rows;

		public PlayerConnectDialog()
		{
			soundClose = SoundDefOf.GameStartSting;
			focusWhenOpened = true;
			silenceAmbientSound = false;
			preventDrawTutor = true;
			preventCameraMotion = true;
			forcePause = true;
			closeOnAccept = false;
			doCloseButton = false;
			absorbInputAroundWindow = true;

			optionalTitle = "Choose your team";
			rows = Ref.controller.teamCount / 4 + 1;
			cols = Math.Min(4, Ref.controller.teamCount);
		}

		public override Vector2 InitialSize
		{
			get
			{
				var w = 2 * Margin + cols * (size + innerMargin) - innerMargin;
				var h = 2 * Margin + rows * (size + innerMargin) - innerMargin + 100 + CloseButSize.y;
				return new Vector2(w, h);
			}
		}

		public override void DoWindowContents(Rect inRect)
		{
			Rect r;
			var i = 0;
			for (var y = 0; y < rows && i < Ref.controller.teamCount; y++)
				for (var x = 0; x < cols && i < Ref.controller.teamCount; x++)
				{
					var settlement = Find.WorldObjects.SettlementAt(Ref.controller.tiles[i]);
					var rx = x * (size + innerMargin);
					var ry = y * (size + innerMargin + 25);

					r = new Rect(rx, ry, size, size);
					Widgets.DrawBoxSolid(r, Ref.TeamColors[i]);
					if (Ref.controller.team != i)
						Widgets.DrawBoxSolid(r.ExpandedBy(-4), new Color(0f, 0f, 0f, 0.8f));
					if (Mouse.IsOver(r))
					{
						GUI.color = Color.black;
						Widgets.DrawBox(r);

						if (Event.current.type == EventType.MouseDown)
						{
							Event.current.Use();
							Multiplayer.JoinTeam(i);
							Log.Warning($"Team now {Ref.controller.team}");
						}
					}

					GUI.color = Color.white;
					Text.Anchor = TextAnchor.MiddleCenter;
					Text.Font = GameFont.Small;
					Widgets.Label(r.ExpandedBy(-8), settlement.Name);

					r = new Rect(rx, ry + size + 2, size, 20);
					Text.Anchor = TextAnchor.UpperCenter;
					Text.Font = GameFont.Tiny;
					Widgets.Label(r, Ref.controller.team == i ? MP.PlayerName : $"Team {i + 1}");

					i++;
				}

			Text.Anchor = TextAnchor.LowerLeft;
			r = new Rect(0, inRect.height - CloseButSize.y, inRect.width - CloseButSize.x, CloseButSize.y);
			Widgets.Label(r, $"{Multiplayer.players.Count}/{Ref.controller.teamCount} players connected");

			r = new Rect(inRect.width - CloseButSize.x, inRect.height - CloseButSize.y, CloseButSize.x, CloseButSize.y);
			if (Widgets.ButtonText(r, "Cancel"))
			{
				Find.WindowStack.TryRemove(this, true);

				// TODO: this is the start code - move it somewhere else
				Find.MusicManagerPlay.ForceSilenceFor(7f);
				Find.MusicManagerPlay.disabled = false;
				Find.WindowStack.Notify_GameStartDialogClosed();

				for (i = 0; i < Ref.controller.teamCount; i++)
					Multiplayer.SetSpeed(i, TimeSpeed.Normal);
			}

			Text.Anchor = default;
		}
	}
}