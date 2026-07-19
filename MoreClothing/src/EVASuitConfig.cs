using System.Collections.Generic;
using Klei.AI;
using UnityEngine;

namespace ProtectiveWear
{
    // EVA Suit: a lightweight, checkpoint-free alternative to the Atmo Suit.
    //
    // It lives in the regular CLOTHING slot (delegating to WarmVestConfig), so
    // dupes put it on like any vest -- no Atmo Suit Checkpoint or dock. That
    // also means it can't carry a real oxygen tank the way an Atmo Suit does,
    // so instead of "never breathes" it grants a much larger breath reserve
    // (BreathMax) -- the dupe simply lasts a lot longer before running out of
    // air. Everything else is layered on as equipment AttributeModifiers, the
    // same mechanism the Atmo/Lead suits use, so the game applies them on
    // equip and removes them on unequip automatically:
    //
    //   Insulation          +25   mild protection vs cold AND non-extreme heat
    //   ScaldingThreshold   +15   tolerate hotter surroundings before scalding
    //   RadiationResistance +0.25 mild rad shielding (Lead Suit is 0.66)
    //   BreathMax          +200   go much longer without fresh oxygen
    //   BladderDelta       -0.05  bladder fills slower -> fewer bathroom trips
    //
    // Deliberately weaker than an Atmo Suit (which fully seals + carries O2),
    // balanced by mild values, a two-garment + fiber craft cost, and the fact
    // that it occupies the clothing slot (no snazzy suit / warm coat alongside).
    public class EVASuitConfig : IEquipmentConfig
    {
        public const string ID = "EVASuit";

        // Extra breath reserve the suit grants over a normal dupe's lungful.
        // Shared so the helmet manager knows where "normal" ends for its
        // two-tier refill (fast up to normal, slow for the bonus above it).
        public const float BREATH_BONUS = 200f;

        // Delegate to the Warm Sweater: gives us the clothing slot, a worn body
        // kanim, and its own base cold insulation -- all fitting for a suit.
        private readonly WarmVestConfig vanilla = new WarmVestConfig();

        public EquipmentDef CreateEquipmentDef()
        {
            EquipmentDef def = vanilla.CreateEquipmentDef();
            def.Id = ID;
            def.Anim = Assets.GetAnim((HashedString)"eva_suit_item_kanim");
            def.BuildOverride = Assets.GetAnim((HashedString)"body_eva_suit_kanim");

            if (def.AttributeModifiers == null)
                def.AttributeModifiers = new List<AttributeModifier>();

            Db db = Db.Get();
            var mods = new[]
            {
                new KeyValuePair<string, float>(db.Attributes.Insulation.Id, 30f),
                new KeyValuePair<string, float>(db.Attributes.ScaldingThreshold.Id, 15f),
                new KeyValuePair<string, float>(db.Attributes.RadiationResistance.Id, 0.30f),
                new KeyValuePair<string, float>(db.Amounts.Breath.maxAttribute.Id, BREATH_BONUS),
                new KeyValuePair<string, float>(db.Amounts.Bladder.deltaAttribute.Id, -0.05f),
            };
            foreach (var m in mods)
                def.AttributeModifiers.Add(new AttributeModifier(m.Key, m.Value, "Soft Suit", false));

            // Sealed suit: resist the wet effects (Sopping Wet, Soggy Feet) and the
            // ambient-temperature moods (ColdAir = "Chilly Surroundings", WarmAir =
            // "Warm Surroundings") like the Atmo Suit does, plus eye irritation
            // (Minor/Major Irritation).
            if (def.EffectImmunites == null)
                def.EffectImmunites = new List<Effect>();
            foreach (string effId in new[] { "SoakingWet", "WetFeet", "ColdAir", "WarmAir", "MinorIrritation", "MajorIrritation", "PoppedEarDrums" })
            {
                Effect eff = db.effects.Get(effId);
                if (eff != null && !def.EffectImmunites.Contains(eff))
                    def.EffectImmunites.Add(eff);
            }

            // Track wearers so the dynamic helmet appears in dangerous air,
            // and fill the SHOES slot with the suit's own hidden boots.
            System.Action<Equippable> baseEquip = def.OnEquipCallBack;
            System.Action<Equippable> baseUnequip = def.OnUnequipCallBack;
            def.OnEquipCallBack = (Equippable eq) =>
            {
                baseEquip?.Invoke(eq);
                EVAHelmetManager.OnEquip(eq);
                EVABoots.OnSuitEquipped(eq);
            };
            def.OnUnequipCallBack = (Equippable eq) =>
            {
                EVABoots.OnSuitUnequipped(eq);
                EVAHelmetManager.OnUnequip(eq);
                baseUnequip?.Invoke(eq);
            };

            return def;
        }

        public void DoPostConfigure(GameObject go)
        {
            vanilla.DoPostConfigure(go);
        }

        public string[] GetDlcIds()
        {
            return null;
        }
    }
}
