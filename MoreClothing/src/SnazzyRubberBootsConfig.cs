using UnityEngine;

namespace SnazzySwimwear
{
    // Snazzy Rubber Boots: the vanilla Rubber Boots with a gold sequin finish
    // and +15 room decor. Same delegation approach as the swimwear -- keep the
    // feet-protection tag and wet-feet/slip immunities from RubberBootsConfig,
    // swap the item kanim, and add decor. Vanilla boots have no worn-body
    // kanim, so (like vanilla) only the item art changes.
    public class SnazzyRubberBootsConfig : IEquipmentConfig
    {
        public const string ID = "SnazzyRubberBoots";
        public const float DECOR = 15f;

        private readonly RubberBootsConfig vanilla = new RubberBootsConfig();

        public EquipmentDef CreateEquipmentDef()
        {
            EquipmentDef def = vanilla.CreateEquipmentDef();
            def.Id = ID;
            def.Anim = Assets.GetAnim((HashedString)"snazzy_rubber_boots_item_kanim");

            System.Action<Equippable> baseEquip = def.OnEquipCallBack;
            System.Action<Equippable> baseUnequip = def.OnUnequipCallBack;
            def.OnEquipCallBack = (Equippable eq) =>
            {
                baseEquip?.Invoke(eq);
                SnazzyDecor.Add(eq, DECOR, "Snazzy Rubber Boots");
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

        // Obsolete interface member; no DLC restriction of our own.
        public string[] GetDlcIds()
        {
            return null;
        }
    }
}
