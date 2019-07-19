using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimBattle
{
	public class BattleOverview
	{
		const int outerspace = 20;
		const int tabBarHeight = 35;

		readonly MiniMap[] minimaps = new MiniMap[] { null, null, null, null, null, null, null };
		public bool showing = false;
		int mapCounter = -1;

		public void OnGUI()
		{
			if (showing)
				Draw();
		}

		public void Update()
		{
			if (mapCounter == -1)
			{
				for (var i = 0; i < GameController.tileCount; i++)
				{
					if (minimaps[i] == null)
						minimaps[i] = new MiniMap(i);
					minimaps[i].UpdateTexture();
				}
				return;
			}

			if (Find.TickManager.TicksGame % 100 == 0)
			{
				mapCounter = (mapCounter + 1) % GameController.tileCount;
				if (minimaps[mapCounter] == null)
					minimaps[mapCounter] = new MiniMap(mapCounter);
				minimaps[mapCounter].UpdateTexture();
			}
		}

		public void Draw()
		{
			DrawBackground();
			DrawMaps();
		}

		private void DrawBackground()
		{
			if (Event.current.type != EventType.Repaint)
				return;

			var rect = new Rect(0, 0, UI.screenWidth, UI.screenHeight - tabBarHeight);
			Widgets.DrawBoxSolid(rect, new Color(0f, 0f, 0f, 0.6f));
		}

		private void DrawMaps()
		{
			var devToolsHeight = Prefs.DevMode ? 25 : 0;

			var innerspace = 4;
			var halfspace = innerspace / 2;
			var midX = UI.screenWidth / 2;
			var topSpace = outerspace + devToolsHeight;

			Vector2[] Positions(int n, out int dim)
			{
				var dx = (UI.screenWidth - 2 * outerspace - 2 * innerspace) / 3;
				var dz = (UI.screenHeight - 2 * outerspace - 2 * innerspace - tabBarHeight - devToolsHeight) / 3;
				dim = Math.Min(dx, dz);
				var halfdim = dim / 2;

				// ---#3#-#2#---
				// -#4#-#0#-#1#-
				// ---#5#-#6#---

				return new Vector2[][] {
					new [] { // #2
						new Vector2(midX + halfdim + innerspace, topSpace + dim + innerspace), // c
						new Vector2(midX - halfdim - innerspace - dim, topSpace + dim + innerspace), // r
					},
					new [] { // #3
						new Vector2(midX - halfdim - innerspace - dim, topSpace + dim + innerspace), // l
						new Vector2(midX - halfdim, topSpace + dim + innerspace), //c
						new Vector2(midX + halfdim + innerspace, topSpace + dim + innerspace), // r
					},
					new [] { // #4
						new Vector2(midX - halfspace - dim, topSpace + 0), // tl
						new Vector2(midX + halfspace, topSpace + 0), // tr
						new Vector2(midX - halfdim, topSpace + dim + innerspace), // c
						new Vector2(midX + halfdim + innerspace, topSpace + dim + innerspace), // r
					},
					new [] { // #5
						new Vector2(midX - halfspace - dim, topSpace + 0), // tl
						new Vector2(midX + halfspace, topSpace + 0), // tr
						new Vector2(midX - halfdim, topSpace + dim + innerspace), // c
						new Vector2(midX - halfspace - dim, topSpace + dim + innerspace + dim + innerspace), // bl
						new Vector2(midX + halfspace, topSpace + dim + innerspace + dim + innerspace), // br
					},
					new [] { // #6
						new Vector2(midX - halfspace - dim, topSpace + 0), // tl
						new Vector2(midX + halfspace, topSpace + 0), // tr
						new Vector2(midX - halfdim, topSpace + dim + innerspace), // c
						new Vector2(midX + halfdim + innerspace, topSpace + dim + innerspace), // r
						new Vector2(midX - halfspace - dim, topSpace + dim + innerspace + dim + innerspace), // bl
						new Vector2(midX + halfspace, topSpace + dim + innerspace + dim + innerspace), // br
					},
					new [] { // #7
						new Vector2(midX - halfspace - dim, topSpace + 0), // tl
						new Vector2(midX + halfspace, topSpace + 0), // tr
						new Vector2(midX - halfdim - innerspace - dim, topSpace + dim + innerspace), // l
						new Vector2(midX - halfdim, topSpace + dim + innerspace), // c
						new Vector2(midX + halfdim + innerspace, topSpace + dim + innerspace), // r
						new Vector2(midX - halfspace - dim, topSpace + dim + innerspace + dim + innerspace), // bl
						new Vector2(midX + halfspace, topSpace + dim + innerspace + dim + innerspace), // br
					}
				}[n];
			}

			var positions = Positions(GameController.tileCount - 2, out var size);
			for (var i = 0; i < GameController.tileCount; i++)
			{
				var mapRect = new Rect(positions[i], new Vector2(size, size));
				minimaps[i]?.Draw(mapRect);
			}
		}
	}
}