using UnityEngine;

namespace ProtectiveWear
{
    // The Soft Suit covers its wearer head to toe, so it fills the SHOES slot
    // too: equipping the suit auto-equips a hidden pair of "Soft Suit Boots"
    // (kicking off any real boots, which drop like a normal swap), and taking
    // the suit off removes and destroys them. The boots delegate to the
    // vanilla Rubber Boots config, so they carry the real footwear behaviour
    // (FeetProtection tag, wet-feet immunity, sure footing) for free.
    //
    // The boots only ever exist while worn -- they're never craftable, never
    // drop, and self-destruct on unequip -- so they can't leak into storage.
    public class EVASuitBootsConfig : IEquipmentConfig
    {
        public const string ID = "EVASuitBoots";

        private readonly RubberBootsConfig vanilla = new RubberBootsConfig();

        public EquipmentDef CreateEquipmentDef()
        {
            EquipmentDef def = vanilla.CreateEquipmentDef();
            def.Id = ID;
            def.Anim = Assets.GetAnim((HashedString)"eva_boots_item_kanim");

            // Self-destruct on unequip: whether the suit came off, the wearer
            // died, or the player unequipped just the boots from the menu,
            // loose Soft Suit Boots must never hit the floor.
            System.Action<Equippable> baseUnequip = def.OnUnequipCallBack;
            def.OnUnequipCallBack = (Equippable eq) =>
            {
                baseUnequip?.Invoke(eq);
                EVABoots.DestroyBoots(eq);
            };
            return def;
        }

        public void DoPostConfigure(GameObject go)
        {
            vanilla.DoPostConfigure(go);
        }

        public string[] GetDlcIds()
        {
            // Register everywhere, like our other configs (RubberBootsConfig
            // implements its DLC gating explicitly, so it can't be delegated).
            // Without the Bionic DLC the boots simply never spawn: the suit
            // recipe needs vanilla Rubber Boots as an ingredient anyway.
            return null;
        }
    }

    // Equip/unequip choreography for the hidden boots, driven from the Soft
    // Suit's own equipment callbacks (EVASuitConfig).
    public static class EVABoots
    {
        private static readonly Tag BootsTag = new Tag(EVASuitBootsConfig.ID);

        public static void OnSuitEquipped(Equippable suitEq)
        {
            GameObject w = EVAHelmetManager.GetWearer(suitEq);
            if (w == null) return;
            Equipment equipment = GetEquipment(w);
            if (equipment == null) return;

            AssignableSlotInstance slot = equipment.GetSlot(Db.Get().AssignableSlots.Shoes);
            if (slot == null) return;

            Assignable current = slot.assignable;
            if (current != null)
            {
                KPrefabID kpid = current.GetComponent<KPrefabID>();
                if (kpid != null && kpid.PrefabTag == BootsTag)
                    return; // already wearing suit boots (equip callbacks re-run on load)

                // Kick real footwear off first; it drops like any clothing swap.
                Equippable curEq = current.GetComponent<Equippable>();
                if (curEq != null) equipment.Unequip(curEq);
            }

            GameObject prefab = Assets.GetPrefab(BootsTag);
            if (prefab == null)
            {
                Debug.LogWarning("[ProtectiveWear] EVASuitBoots prefab missing (no Bionic DLC?)");
                return;
            }
            GameObject boots = Util.KInstantiate(prefab, w.transform.position);
            boots.SetActive(true);
            Equippable beq = boots.GetComponent<Equippable>();
            if (beq == null) return;
            beq.Assign(suitEq.assignee);
            equipment.Equip(beq);
        }

        public static void OnSuitUnequipped(Equippable suitEq)
        {
            GameObject w = EVAHelmetManager.GetWearer(suitEq);
            if (w == null) return;
            Equipment equipment = GetEquipment(w);
            if (equipment == null) return;

            AssignableSlotInstance slot = equipment.GetSlot(Db.Get().AssignableSlots.Shoes);
            Assignable current = slot != null ? slot.assignable : null;
            if (current == null) return;
            KPrefabID kpid = current.GetComponent<KPrefabID>();
            if (kpid == null || kpid.PrefabTag != BootsTag) return;

            Equippable beq = current.GetComponent<Equippable>();
            if (beq != null) equipment.Unequip(beq); // its unequip callback destroys it
        }

        // Deferred so we never destroy an Equippable in the middle of the
        // equipment system's own unequip bookkeeping.
        public static void DestroyBoots(Equippable eq)
        {
            GameObject go = eq != null ? eq.gameObject : null;
            if (go == null) return;
            GameScheduler.Instance.ScheduleNextFrame("EVABoots.Destroy", _ =>
            {
                if (go != null) Util.KDestroyGameObject(go);
            });
        }

        private static Equipment GetEquipment(GameObject wearer)
        {
            MinionIdentity mi = wearer.GetComponent<MinionIdentity>();
            return mi != null ? mi.GetEquipment() : null;
        }
    }
}
