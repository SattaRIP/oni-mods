using UnityEngine;

namespace SnazzySwimwear
{
    // Snazzy Swimwear: the vanilla Swimwear (DrySuit) with gold sequin bands
    // and +25 room decor. We delegate to the real DrySuitConfig so every bit
    // of its behaviour is preserved -- the swim tag, the wet-feet/soaking-wet
    // immunities, collision, snap points -- then swap only the id, the two
    // kanims, and wrap the equip callbacks to layer our decor on top.
    public class SnazzySwimwearConfig : IEquipmentConfig
    {
        public const string ID = "SnazzySwimwear";
        public const float DECOR = 25f;

        private readonly DrySuitConfig vanilla = new DrySuitConfig();

        public EquipmentDef CreateEquipmentDef()
        {
            EquipmentDef def = vanilla.CreateEquipmentDef();
            def.Id = ID;
            def.Anim = Assets.GetAnim((HashedString)"snazzy_swimwear_item_kanim");
            def.BuildOverride = Assets.GetAnim((HashedString)"body_snazzy_swimwear_kanim");

            System.Action<Equippable> baseEquip = def.OnEquipCallBack;
            System.Action<Equippable> baseUnequip = def.OnUnequipCallBack;
            def.OnEquipCallBack = (Equippable eq) =>
            {
                baseEquip?.Invoke(eq);
                SnazzyDecor.Add(eq, DECOR, "Snazzy Swimwear");
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
