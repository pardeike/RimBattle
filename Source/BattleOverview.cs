using System;
using UnityEngine;
using Verse;

namespace RimBattle
{
	public class BattleOverview
	{
		readonly MiniMap[] minimaps = new MiniMap[] { null, null, null, null, null, null, null };
		public bool showing = false;
		int mapCounter = -1;

		public void OnGUI()
		{
			if (Event.current.type == EventType.Layout)
				return;
			if (Event.current.type != EventType.Repaint)
				return;
			if (!showing)
				return;

			Draw();
		}

		public void Update()
		{
			if (mapCounter == -1)
			{
				for (var i = 0; i < 7; i++)
				{
					if (minimaps[i] == null)
						minimaps[i] = new MiniMap(Refs.controller.MapByIndex(i));
					minimaps[i].UpdateTexture();
				}
				return;
			}

			if (Find.TickManager.TicksGame % 100 == 0)
			{
				mapCounter = (mapCounter + 1) % 7;
				if (minimaps[mapCounter] == null)
					minimaps[mapCounter] = new MiniMap(Refs.controller.MapByIndex(mapCounter));
				minimaps[mapCounter].UpdateTexture();
			}
		}

		public void Draw()
		{
			var aroundspace = 35;
			var bottomspace = 45;
			var innerspace = 4;
			var halfspace = innerspace / 2;
			var topLeft = new Vector2(aroundspace, aroundspace);
			var midX = UI.screenWidth / 2;
			var dx = (UI.screenWidth - aroundspace - aroundspace - 2 * innerspace) / 3;
			var dz = (UI.screenHeight - aroundspace - bottomspace - 2 * innerspace) / 3;
			var dim = Math.Min(dx, dz);
			var halfdim = dim / 2;

			// ---#3#-#2#---
			// -#4#-#0#-#1#-
			// ---#5#-#6#---

			var offsets = new[]
			{
				/* 0 */ new Vector2(midX - halfdim, dim + innerspace),
				/* 1 */ new Vector2(midX + halfdim + innerspace, dim + innerspace),
				/* 2 */ new Vector2(midX + halfspace, 0),
				/* 3 */ new Vector2(midX - halfspace - dim, 0),
				/* 4 */ new Vector2(midX - halfdim - innerspace - dim, dim + innerspace),
				/* 5 */ new Vector2(midX - halfspace - dim, dim + innerspace + dim + innerspace),
				/* 6 */ new Vector2(midX + halfspace, dim + innerspace + dim + innerspace)
			};

			for (var i = 0; i < 7; i++)
			{
				var mapRect = new Rect(topLeft + offsets[i], new Vector2(dim, dim)); ;
				minimaps[i]?.Draw(mapRect);
			}
		}
	}
}