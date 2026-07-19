using System.Collections.Generic;
using Klei.AI;
using UnityEngine;

namespace SnazzySwimwear
{
    // Adds room decor while an item is worn, without going through the single
    // clothing-slot decor modifier the game keeps per duplicant (that slot is
    // owned by the vest/suit and can't be shared). We add our own named
    // AttributeModifier straight onto the wearer's DecorProvider, so a snazzy
    // suit and snazzy boots stack, and neither fights the base clothing slot.
    //
    // The equip/unequip callbacks re-run on load (equipment re-equips during
    // deserialization), so the modifier is rebuilt then too -- no save state
    // of our own to keep.
    public static class SnazzyDecor
    {
        private static readonly Dictionary<Equippable, AttributeModifier> Applied =
            new Dictionary<Equippable, AttributeModifier>();

        private static GameObject GetWearer(Equippable eq)
        {
            IAssignableIdentity assignee = eq?.assignee;
            Ownables owner = assignee?.GetSoleOwner();
            if (owner == null) return null;
            MinionAssignablesProxy proxy = owner.GetComponent<MinionAssignablesProxy>();
            return proxy != null ? proxy.GetTargetGameObject() : null;
        }

        public static void Add(Equippable eq, float decor, string description)
        {
            GameObject wearer = GetWearer(eq);
            DecorProvider dp = wearer != null ? wearer.GetComponent<DecorProvider>() : null;
            if (dp == null || dp.decor == null) return;

            Remove(eq); // guard against a double-equip leaving a stale modifier
            AttributeModifier mod = new AttributeModifier(
                Db.Get().BuildingAttributes.Decor.Id, decor, description, false, false, true);
            dp.decor.Add(mod);
            Applied[eq] = mod;

            // Track this garment/footwear so its worn art gets pulled while a
            // real suit is worn on top (see ProtectiveWear.SuitInteraction).
            ProtectiveWear.SuitInteraction.Register(eq);
        }

        public static void Remove(Equippable eq)
        {
            ProtectiveWear.SuitInteraction.Unregister(eq);
            if (eq == null || !Applied.TryGetValue(eq, out AttributeModifier mod)) return;
            Applied.Remove(eq);
            GameObject wearer = GetWearer(eq);
            DecorProvider dp = wearer != null ? wearer.GetComponent<DecorProvider>() : null;
            if (dp != null && dp.decor != null) dp.decor.Remove(mod);
        }
    }
}
