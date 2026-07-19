using System.Collections.Generic;
using Klei.AI;
using UnityEngine;

namespace SnazzySwimwear
{
    // Snazzy Swimwear: a gold-sequin refashion of the vanilla Swimwear (Dry Suit),
    // +25 room decor.
    //
    // We delegate to WarmVestConfig -- NOT DrySuitConfig -- on purpose. The Dry
    // Suit's own vest layer renders OVER a worn Atmo/Lead/Jet Suit (it made the
    // suit look invisible), whereas a WarmVest-based garment hides correctly under
    // a suit (confirmed in-game via the Upgraded Warm Coat). So we take the vest
    // that behaves, strip the sweater's warmth, and re-add the Dry Suit's real
    // function: feet+waist protection (wet-feet + bionic water immunity) and the
    // wet-effect immunities.
    public class SnazzySwimwearConfig : IEquipmentConfig
    {
        public const string ID = "SnazzySwimwear";
        public const float DECOR = 25f;

        private readonly WarmVestConfig vanilla = new WarmVestConfig();

        public EquipmentDef CreateEquipmentDef()
        {
            EquipmentDef def = vanilla.CreateEquipmentDef();
            def.Id = ID;
            def.Anim = Assets.GetAnim((HashedString)"snazzy_swimwear_item_kanim");
            def.BuildOverride = Assets.GetAnim((HashedString)"body_snazzy_swimwear_kanim");
            def.BuildOverridePriority = 4;

            // Not a warm garment: drop the Warm Sweater's insulation.
            def.AttributeModifiers?.Clear();

            // Dry Suit's wet-effect immunities.
            if (def.EffectImmunites == null) def.EffectImmunites = new List<Effect>();
            foreach (string effId in new[] { "WetFeet", "SoakingWet" })
            {
                Effect e = Db.Get().effects.Get(effId);
                if (e != null && !def.EffectImmunites.Contains(e)) def.EffectImmunites.Add(e);
            }

            System.Action<Equippable> baseEquip = def.OnEquipCallBack;
            System.Action<Equippable> baseUnequip = def.OnUnequipCallBack;
            def.OnEquipCallBack = (Equippable eq) =>
            {
                baseEquip?.Invoke(eq);
                SnazzyDecor.Add(eq, DECOR, "Snazzy Swimwear");
                // The Dry Suit grants this tag on equip; re-add it since we no
                // longer inherit the Dry Suit's callback. Gives wet-feet immunity
                // and protects bionic dupes from the water shock (up to 0.35 depth).
                GameObject w = ProtectiveWear.EVAHelmetManager.GetWearer(eq);
                if (w != null) w.AddTag(GameTags.FeetAndWaistProtection);
            };
            def.OnUnequipCallBack = (Equippable eq) =>
            {
                GameObject w = ProtectiveWear.EVAHelmetManager.GetWearer(eq);
                if (w != null) w.RemoveTag(GameTags.FeetAndWaistProtection);
                SnazzyDecor.Remove(eq);
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
