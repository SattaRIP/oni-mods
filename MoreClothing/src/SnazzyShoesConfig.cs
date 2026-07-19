using UnityEngine;

namespace SnazzySwimwear
{
    // Snazzy Shoes: sleek black-and-gold dress shoes. Unlike the Snazzy Rubber
    // Boots, these are NOT rubber boots and give no protection -- purely a decor
    // boost. We delegate to RubberBootsConfig only to reuse the feet-slot item
    // plumbing, then strip its attribute modifiers (the wet-feet/slip protection)
    // and effect immunities so nothing but decor remains, and swap in the shoe art.
    public class SnazzyShoesConfig : IEquipmentConfig
    {
        public const string ID = "SnazzyShoes";
        public const float DECOR = 15f;

        private readonly RubberBootsConfig vanilla = new RubberBootsConfig();

        public EquipmentDef CreateEquipmentDef()
        {
            EquipmentDef def = vanilla.CreateEquipmentDef();
            def.Id = ID;
            def.Anim = Assets.GetAnim((HashedString)"snazzy_shoes_item_kanim");
            // Worn-shoe body art DISABLED for now (same footwear-vs-suit render
            // conflict as the Snazzy Rubber Boots -- see that config). Behaves
            // like vanilla footwear; the kanim is still generated for a revisit.

            // Purely cosmetic: drop the rubber boots' functional protection.
            def.AttributeModifiers?.Clear();
            def.EffectImmunites?.Clear();

            System.Action<Equippable> baseEquip = def.OnEquipCallBack;
            System.Action<Equippable> baseUnequip = def.OnUnequipCallBack;
            def.OnEquipCallBack = (Equippable eq) =>
            {
                baseEquip?.Invoke(eq);
                SnazzyDecor.Add(eq, DECOR, "Shoes");
            };
            def.OnUnequipCallBack = (Equippable eq) =>
            {
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
