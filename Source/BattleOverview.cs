using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimBattle
{
	public class BattleOverview
	{
		const int tabBarHeight = 35;

		readonly MiniMap[] minimaps = new MiniMap[] { null, null, null, null, null, null, null };
		public bool showing = false;
		public bool lastShowing = false;
		int mapCounter = -1;

		public void OnGUI()
		{
			if (showing)
				Draw();
		}

		void UpdateTextureFully()
		{
			for (var i = 0; i < GameController.tileCount; i++)
			{
				if (minimaps[i] == null)
					minimaps[i] = new MiniMap(i);
				minimaps[i].UpdateTextureFully();
			}
		}

		IEnumerator updater = null;
		public void Update()
		{
			if (mapCounter == -1)
			{
				UpdateTextureFully();
				return;
			}

			if (Find.TickManager.Paused)
				return;

			if (showing == false)
			{
				lastShowing = false;
				return;
			}

			if (lastShowing == false)
			{
				UpdateTextureFully();
				lastShowing = true;
				return;
			}

			updater = updater ?? minimaps[mapCounter].UpdateTexture();
			if (updater.MoveNext() == false)
			{
				mapCounter = (mapCounter + 1) % GameController.tileCount;
				updater = minimaps[mapCounter].UpdateTexture();
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
			var innerspace = 4;
			var outerspace = 20;
			var devToolsHeight = Prefs.DevMode ? 25 : 0;
			var midX = UI.screenWidth / 2;
			var midZ = outerspace + devToolsHeight + (UI.screenHeight - devToolsHeight - outerspace - tabBarHeight - outerspace) / 2;

			var colCounts = new int[][] {
				new int[] { 1 },
				new int[] { 2 },
				new int[] { 3 },
				new int[] { 2, 2 },
				new int[] { 2, 1, 2 },
				new int[] { 2, 2, 2 },
				new int[] { 2, 3, 2 },
			}[GameController.tileCount - 1];

			var rowCount = colCounts.Length;
			var maxColCount = colCounts.Max();

			var dx = (UI.screenWidth - (maxColCount - 1) * innerspace - 2 * outerspace) / maxColCount;
			var dz = (UI.screenHeight - (rowCount - 1) * innerspace - 2 * outerspace - tabBarHeight - devToolsHeight) / rowCount;
			var dim = Math.Min(dx, dz);
			var h = rowCount * dim + (rowCount - 1) * innerspace;

			var shearAmount = 0;
			if (colCounts.Count() > 1 && colCounts.All(n => n % 2 == 0))
				shearAmount = (dim + innerspace) / -4;

			var i = 0;
			for (var row = 0; row < rowCount; row++)
			{
				var colCount = colCounts[row];
				var z = midZ - h / 2 + row * dim + (row - 1) * innerspace;

				var w = colCount * dim + (colCount - 1) * innerspace;
				for (var col = 0; col < colCount; col++)
				{
					var x = midX - w / 2 + col * dim + (col - 1) * innerspace;
					minimaps[i++]?.Draw(new Rect(x + shearAmount, z, dim, dim));
					shearAmount *= -1;
				}
			}
		}
	}
}