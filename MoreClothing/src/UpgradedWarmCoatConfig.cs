using System.Collections.Generic;
using Klei.AI;
using UnityEngine;

namespace ProtectiveWear
{
    // Upgraded Warm Coat: the vanilla Warm Sweater refashioned into a heavier
    // insulated coat. We delegate to WarmVestConfig so it keeps the sweater's
    // base cold protection (applied via ClothingInfo), then add an extra
    // Insulation attribute modifier so it resists temperature noticeably better
    // than the plain sweater. Crafted at the Refashionator from a Warm Sweater
    // plus reed fiber.
    public class UpgradedWarmCoatConfig : IEquipmentConfig
    {
        public const string ID = "UpgradedWarmCoat";
        private const float DECOR = 10f;

        private readonly WarmVestConfig vanilla = new WarmVestConfig();

        public EquipmentDef CreateEquipmentDef()
        {
            EquipmentDef def = vanilla.CreateEquipmentDef();
            def.Id = ID;
            def.Anim = Assets.GetAnim((HashedString)"upgraded_warm_coat_item_kanim");
            def.BuildOverride = Assets.GetAnim((HashedString)"body_upgraded_warm_coat_kanim");

            if (def.AttributeModifiers == null)
                def.AttributeModifiers = new List<AttributeModifier>();

            // Extra cold/heat insulation on top of the sweater's base warmth.
            def.AttributeModifiers.Add(new AttributeModifier(
                Db.Get().Attributes.Insulation.Id, 30f, "Upgraded Warm Coat", false));

            // A handsome tailored coat: modest room decor while worn, via the
            // same stackable wearer-decor helper the Snazzy garments use.
            System.Action<Equippable> baseEquip = def.OnEquipCallBack;
            System.Action<Equippable> baseUnequip = def.OnUnequipCallBack;
            def.OnEquipCallBack = (Equippable eq) =>
            {
                baseEquip?.Invoke(eq);
                SnazzySwimwear.SnazzyDecor.Add(eq, DECOR, "Winter Coat");
            };
            def.OnUnequipCallBack = (Equippable eq) =>
            {
                SnazzySwimwear.SnazzyDecor.Remove(eq);
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
