using System;
using System.Collections.Generic;
using HarmonyLib;
using KMod;
using TUNING;
using UnityEngine;

// Fix layer for the merged "Magpie Bridges+" mod.
// The base Magpie Bridge (鹊桥) mod registers its vanilla liquid/gas/wire bridges
// with Chinese names and via the 2-arg AddBuildingToPlanScreen, which drops them into
// the "uncategorized" subcategory (shown as "Label"). This patch overrides the names
// to English and moves each to its correct build-menu subcategory.
namespace MagpieBridgesPlus
{
	public class MagpieBridgesPlusMod : UserMod2
	{
		public override void OnLoad(Harmony harmony)
		{
			base.OnLoad(harmony);
			harmony.PatchAll();
		}
	}

	public struct BridgeInfo
	{
		public string name, desc, effect, subcat;
		public BridgeInfo(string n, string d, string e, string s) { name = n; desc = d; effect = e; subcat = s; }
	}

	[HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
	public static class MagpieBridges_Fix_Patch
	{
		// internal building id (lowercase) -> English strings + correct subcategory
		static readonly Dictionary<string, BridgeInfo> Bridges = new Dictionary<string, BridgeInfo>
		{
			{ "shuiguanqiao2", new BridgeInfo("Liquid Pipe Bridge (2-Tile Gap)",
				"A longer Liquid Pipe Bridge that spans a 2-tile gap, letting one liquid pipe cross over others without connecting to them.",
				"Carries liquid across a 2-tile gap, passing over other pipes without joining them.", "pipes") },
			{ "shuiguanqiao3", new BridgeInfo("Liquid Pipe Bridge (3-Tile Gap)",
				"A longer Liquid Pipe Bridge that spans a 3-tile gap, letting one liquid pipe cross over others without connecting to them.",
				"Carries liquid across a 3-tile gap, passing over other pipes without joining them.", "pipes") },
			{ "qiguanqiao2", new BridgeInfo("Gas Pipe Bridge (2-Tile Gap)",
				"A longer Gas Pipe Bridge that spans a 2-tile gap, letting one gas pipe cross over others without connecting to them.",
				"Carries gas across a 2-tile gap, passing over other pipes without joining them.", "pipes") },
			{ "qiguanqiao3", new BridgeInfo("Gas Pipe Bridge (3-Tile Gap)",
				"A longer Gas Pipe Bridge that spans a 3-tile gap, letting one gas pipe cross over others without connecting to them.",
				"Carries gas across a 3-tile gap, passing over other pipes without joining them.", "pipes") },
			{ "dianxianqiao2", new BridgeInfo("Wire Bridge (2-Tile Gap)",
				"A longer Wire Bridge that spans a 2-tile gap, letting one power wire cross over others without connecting to them.",
				"Carries power across a 2-tile gap, crossing other wires without joining their circuits.", "wires") },
			{ "dianxianqiao3", new BridgeInfo("Wire Bridge (3-Tile Gap)",
				"A longer Wire Bridge that spans a 3-tile gap, letting one power wire cross over others without connecting to them.",
				"Carries power across a 3-tile gap, crossing other wires without joining their circuits.", "wires") },
			{ "daoxianqiao2", new BridgeInfo("Conductive Wire Bridge (2-Tile Gap)",
				"A longer Conductive Wire Bridge that spans a 2-tile gap, letting one conductive wire cross over others without connecting to them.",
				"Carries high-wattage power across a 2-tile gap, crossing other wires without joining their circuits.", "wires") },
			{ "daoxianqiao3", new BridgeInfo("Conductive Wire Bridge (3-Tile Gap)",
				"A longer Conductive Wire Bridge that spans a 3-tile gap, letting one conductive wire cross over others without connecting to them.",
				"Carries high-wattage power across a 3-tile gap, crossing other wires without joining their circuits.", "wires") },
		};

		// Runs AFTER the base mod's Prefix has registered the bridges (Chinese name, "uncategorized").
		public static void Postfix()
		{
			try
			{
				// 1) Override names/descriptions/effects to English.
				foreach (var kv in Bridges)
				{
					string ID = kv.Key.ToUpperInvariant();
					Strings.Add("STRINGS.BUILDINGS.PREFABS." + ID + ".NAME", kv.Value.name);
					Strings.Add("STRINGS.BUILDINGS.PREFABS." + ID + ".DESC", kv.Value.desc);
					Strings.Add("STRINGS.BUILDINGS.PREFABS." + ID + ".EFFECT", kv.Value.effect);

					// Def.Name is a read-only property; override its cached value via its backing field.
					BuildingDef def = Assets.GetBuildingDef(kv.Key);
					if (def != null)
					{
						var f = AccessTools.Field(typeof(Def), "<Name>k__BackingField");
						if (f != null) f.SetValue(def, kv.Value.name);
					}
				}

				// 2) Move each from the "uncategorized" (Label) subcategory to the correct one.
				int fixedCount = 0;
				foreach (var plan in TUNING.BUILDINGS.PLANORDER)
				{
					var list = plan.buildingAndSubcategoryData;
					if (list == null) continue;
					for (int i = 0; i < list.Count; i++)
					{
						BridgeInfo info;
						if (Bridges.TryGetValue(list[i].Key, out info))
						{
							list[i] = new KeyValuePair<string, string>(list[i].Key, info.subcat);
							fixedCount++;
						}
					}
				}
				Debug.Log("[MagpieBridgesPlus] Relabeled 8 base bridges to English; corrected subcategory on " + fixedCount + " entries.");
			}
			catch (Exception e)
			{
				Debug.LogWarning("[MagpieBridgesPlus] fix failed (non-fatal): " + e);
			}
		}
	}
}
