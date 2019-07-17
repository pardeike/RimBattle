using UnityEngine;
using Verse;

namespace RimBattle
{
	class BattleOverview : Window
	{
		public override Vector2 InitialSize => new Vector2(100f + 200f, 125f + 200f);

		public override void WindowUpdate()
		{
			base.WindowUpdate();
			if (Find.TickManager.TicksGame % 100 == 0)
				UpdateTexture(Find.CurrentMap);
		}

		public override void DoWindowContents(Rect inRect)
		{
			Minimap(inRect);
		}

		static void UpdateTexture(Map map)
		{
			if (Refs.MapTexture.width != map.Size.x * Refs.mapRes)
			{
				Refs.MapTexture.Resize(map.Size.x * Refs.mapRes, map.Size.z * Refs.mapRes);
				Refs.MapTexture.Apply(true);
			}
			foreach (var c in map.AllCells)
				ProcessCell(c, map);
			Refs.MapTexture.Apply(true);
		}

		static void ProcessCell(IntVec3 c, Map map)
		{
			var edifice = c.GetEdifice(map);
			if (edifice != null)
			{
				var filled = edifice.def.Fillage == FillCategory.Full && !edifice.def.IsDoor;
				Set(c.x, c.z, Refs.edificeColor * (filled ? 1f : 0.5f));
				return;
			}

			if (c.GetTerrain(map).IsWater)
			{
				Set(c.x, c.z, Refs.WaterColor);
				return;
			}

			if (Refs.showPlants && c.GetPlant(map) != null)
			{
				Set(c.x, c.z, Refs.PlantColor);
				return;
			}

			Set(c.x, c.z, Refs.GroundColor);
			return;
		}

		static void Set(int x, int y, Color c)
		{
			if (Refs.mapRes == 1)
			{
				Refs.MapTexture.SetPixel(x, y, c);
				return;
			}

			x *= Refs.mapRes;
			y *= Refs.mapRes;
			for (var i = 0; i < Refs.mapRes; i++)
				for (var j = 0; j < Refs.mapRes; j++)
					Refs.MapTexture.SetPixel(x + i, y + j, c);
		}

		static void Minimap(Rect inRect)
		{
			var map = Find.CurrentMap;
			UpdateTexture(map);

			var rect = inRect.ContractedBy(1f);
			GUI.BeginClip(rect);
			GUI.BeginGroup(inRect);
			Widgets.DrawTextureFitted(new Rect(0f, 0f, inRect.width, inRect.height), Refs.MapTexture, 1f);
			GUI.EndGroup();
			GUI.EndClip();
		}
	}
}