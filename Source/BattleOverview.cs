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
			DrawMaps(new Rect(0, 0, UI.screenWidth, UI.screenHeight));
		}

		static void DrawBackground()
		{
			if (Event.current.type != EventType.Repaint)
				return;

			var rect = new Rect(0, 0, UI.screenWidth, UI.screenHeight - tabBarHeight);
			Widgets.DrawBoxSolid(rect, new Color(0f, 0f, 0f, 0.6f));
		}

		public void DrawMaps(Rect baseRect, bool withMargins = true)
		{
			var tabBarSize = withMargins ? tabBarHeight : 0;
			var innerspace = 4;
			var outerspace = withMargins ? 20 : 0;
			var devToolsHeight = withMargins && Prefs.DevMode ? 25 : 0;
			var midX = baseRect.width / 2;
			var midZ = outerspace + devToolsHeight + (baseRect.height - devToolsHeight - outerspace - tabBarSize - outerspace) / 2;

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

			var hasOddRowOffset = colCounts.Count() > 1 && colCounts.All(n => n % 2 == 0);
			var adjustedColCount = maxColCount + (hasOddRowOffset ? 0.5f : 0f);

			var dx = (baseRect.width - (adjustedColCount - 1) * innerspace - 2 * outerspace) / adjustedColCount;
			var dz = (baseRect.height - (rowCount - 1) * innerspace - 2 * outerspace - tabBarSize - devToolsHeight) / rowCount;
			var dim = Math.Min(dx, dz);
			var dimPlusSpace = dim + innerspace;
			var realHeight = rowCount * dimPlusSpace - innerspace;
			var realWidth = adjustedColCount * dimPlusSpace - innerspace;

			var i = 0;
			for (var row = 0; row < rowCount; row++)
			{
				var colCount = colCounts[row];
				var rowOffset = colCount < maxColCount ? dimPlusSpace / 2 : 0;
				var z = midZ - realHeight / 2 + row * dimPlusSpace;
				var oddRowOffset = hasOddRowOffset && (row % 2 != 0) ? dimPlusSpace / 2 : 0f;

				for (var col = 0; col < colCount; col++)
				{
					var x = midX - realWidth / 2 + col * dimPlusSpace;
					minimaps[i++]?.Draw(new Rect(baseRect.x + x + oddRowOffset + rowOffset, baseRect.y + z, dim, dim));
				}
			}
		}
	}
}