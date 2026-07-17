using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace ProtectiveWear
{
    // Mannequin: a dressform that displays one garment and radiates decor for
    // it. Built on the Item Pedestal's exact machinery (Storage +
    // OrnamentReceptacle gives us the "display item" errand and side screen
    // for free), but it only accepts clothing, and instead of the pedestal's
    // "double the item's own decor" rule -- useless for clothes, which have no
    // item decor -- MannequinDecor (below) grants a per-garment decor bonus.
    public class MannequinConfig : IBuildingConfig
    {
        public const string ID = "Mannequin";

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                ID, 1, 2, "mannequin_kanim", 10, 30f,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER2, TUNING.MATERIALS.RAW_MINERALS, 800f,
                BuildLocationRule.OnFloor, BUILDINGS.DECOR.BONUS.TIER1,
                TUNING.NOISE_POLLUTION.NONE, 0.2f);
            def.DefaultAnimState = "pedestal"; // donor anim's state name is kept
            def.Floodable = false;
            def.Overheatable = false;
            def.ViewMode = OverlayModes.Decor.ID;
            def.AudioCategory = "Glass";
            def.AudioSize = "small";
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            Storage storage = go.AddOrGet<Storage>();
            storage.SetDefaultStoredItemModifiers(new List<Storage.StoredItemModifier>
            {
                Storage.StoredItemModifier.Preserve,
                Storage.StoredItemModifier.Seal,
            });

            Prioritizable.AddRef(go);

            SingleEntityReceptacle receptacle = go.AddOrGet<OrnamentReceptacle>();
            receptacle.AddDepositTag(GameTags.Clothes);
            // Chest height on the dressform; only garments without worn torso
            // art (boots, masks) actually show here -- the rest are drawn ON
            // the dressform via the torso symbol override below.
            receptacle.occupyingObjectRelativePosition = new Vector3(0f, 1.25f, -1f);

            go.AddOrGet<DecorProvider>();
            go.AddOrGet<MannequinDecor>();

            go.GetComponent<KPrefabID>().AddTag(GameTags.Decoration, false);
            go.GetComponent<KPrefabID>().AddTag(GameTags.OrnamentDisplayer, false);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            // Lets MannequinDecor swap the kanim's placeholder "torso" symbol
            // for the displayed garment's worn torso art (same machinery that
            // dresses dupes). Must run here: it asserts that the building's
            // KBatchedAnimController already exists, which isn't the case yet
            // in ConfigureBuildingTemplate.
            SymbolOverrideControllerUtil.AddToPrefab(go);
        }
    }

    // Grants the building decor while a garment is displayed, and dresses the
    // mannequin in it: if the garment has worn torso art (the same art a dupe
    // shows when wearing it), that art is drawn on the dressform through a
    // SymbolOverrideController and the floating item icon is hidden. Decor
    // mirrors ItemPedestal's plumbing (attribute modifiers on the building,
    // which DecorProvider picks up) with our own per-garment values.
    public class MannequinDecor : KMonoBehaviour
    {
        private const float DEFAULT_DECOR = 10f;
        private const float RADIUS_BONUS = 2f;

        // Garments with a look worth more than the default. Matches the decor
        // each item gives while worn, so displaying and wearing are equal.
        private static readonly Dictionary<Tag, float> DecorByGarment =
            new Dictionary<Tag, float>
        {
            { new Tag("SnazzySwimwear"), 25f },
            { new Tag("SnazzyRubberBoots"), 15f },
            { new Tag("SnazzyShoes"), 15f },
            { new Tag("SnazzySuit"), 20f },        // vanilla Primo Garb
            { new Tag(EVASuitConfig.ID), 15f },
            { new Tag(UpgradedWarmCoatConfig.ID), 10f },
        };

        // Dupe rig body parts the dummy shows (subset of PARTS in
        // tools/gen_mannequin_kanim.py that garments can cover). Each is
        // overridden with the displayed garment's worn art when present;
        // uncovered parts keep their linen placeholder, like skin on a dupe.
        private static readonly string[] BodySymbols =
        {
            "torso", "pelvis", "belt", "neck", "arm_sleeve",
            "arm_lower_sleeve", "cuff", "hand_paint", "leg", "foot",
        };

        private SingleEntityReceptacle receptacle;
        private Klei.AI.AttributeModifier decorMod;
        private Klei.AI.AttributeModifier radiusMod;
        private GameObject hiddenOccupant;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            receptacle = GetComponent<SingleEntityReceptacle>();
            Subscribe((int)GameHashes.OccupantChanged, OnOccupantChanged);
            Refresh();
            // On load the receptacle may restore its occupant after us.
            GameScheduler.Instance.ScheduleNextFrame("MannequinDecor.Refresh",
                _ => { if (this != null) Refresh(); });
        }

        protected override void OnCleanUp()
        {
            ShowHiddenOccupant();
            Unsubscribe((int)GameHashes.OccupantChanged, OnOccupantChanged);
            base.OnCleanUp();
        }

        private void OnOccupantChanged(object data)
        {
            Refresh();
            // SingleEntityReceptacle can fire this before its Occupant
            // property is assigned; refresh again next frame so a deposit is
            // never missed.
            GameScheduler.Instance.ScheduleNextFrame("MannequinDecor.Refresh",
                _ => { if (this != null) Refresh(); });
        }

        private void Refresh()
        {
            GameObject occupant = receptacle != null ? receptacle.Occupant : null;
            RefreshDecor(occupant);
            RefreshDressing(occupant);
        }

        private void RefreshDecor(GameObject occupant)
        {
            Klei.AI.Attributes attrs = Klei.AI.ModifiersExtensions.GetAttributes(this);
            if (attrs == null) return;
            if (decorMod != null)
            {
                attrs.Remove(decorMod);
                attrs.Remove(radiusMod);
                decorMod = null;
                radiusMod = null;
            }

            if (occupant == null) return;

            KPrefabID kpid = occupant.GetComponent<KPrefabID>();
            if (kpid == null) return;
            float decor;
            if (!DecorByGarment.TryGetValue(kpid.PrefabTag, out decor))
                decor = DEFAULT_DECOR;

            string label = GameTagExtensions.ProperName(kpid.PrefabTag);
            Db db = Db.Get();
            decorMod = new Klei.AI.AttributeModifier(
                db.BuildingAttributes.Decor.Id, decor, label, false, false, true);
            radiusMod = new Klei.AI.AttributeModifier(
                db.BuildingAttributes.DecorRadius.Id, RADIUS_BONUS, label, false, false, true);
            attrs.Add(decorMod);
            attrs.Add(radiusMod);
        }

        // Dress the mannequin: override the kanim's placeholder torso symbol
        // with the garment's worn torso art and hide the floating item icon.
        // Garments with no worn torso art (boots, shoes, masks) keep the icon.
        private void RefreshDressing(GameObject occupant)
        {
            ShowHiddenOccupant();

            SymbolOverrideController soc = GetComponent<SymbolOverrideController>();
            if (soc != null)
                foreach (string part in BodySymbols)
                    soc.RemoveSymbolOverride((HashedString)part, 0);

            if (occupant == null || soc == null) return;
            Equippable eq = occupant.GetComponent<Equippable>();
            // GetBuildOverride resolves facade skins (vanilla outfit items like
            // CustomClothing carry their worn art there, not on the def).
            KAnimFile worn = eq != null ? eq.GetBuildOverride() : null;
            if (worn == null && eq != null && eq.def != null)
                worn = eq.def.BuildOverride;
            KAnimFileData data = worn != null ? worn.GetData() : null;
            if (data == null || data.build == null)
            {
                Debug.Log("[ProtectiveWear] Mannequin dressing: occupant="
                    + occupant.name + " has no worn build; showing item icon");
                return;
            }

            int dressed = 0;
            foreach (string part in BodySymbols)
            {
                KAnim.Build.Symbol s = data.build.GetSymbol((KAnimHashedString)part);
                if (s == null) continue;
                soc.AddSymbolOverride((HashedString)part, s, 0);
                dressed++;
            }
            Debug.Log("[ProtectiveWear] Mannequin dressing: occupant=" + occupant.name
                + " overrode " + dressed + "/" + BodySymbols.Length + " body symbols");
            if (dressed == 0) return;

            // The game's own stored-item hide: disables the anim controller
            // component outright, which the camera-culling system respects
            // (a mere SetVisiblity(false) gets flipped back on when the
            // building scrolls into view).
            Storage.MakeItemInvisible(occupant, true, false);
            hiddenOccupant = occupant;
        }

        // The hidden item icon belongs to the stored garment itself, so it
        // must be made visible again before the garment goes anywhere else
        // (swapped out, dropped, mannequin deconstructed).
        private void ShowHiddenOccupant()
        {
            if (hiddenOccupant == null) return;
            Storage.MakeItemInvisible(hiddenOccupant, false, false);
            hiddenOccupant = null;
        }

        // Re-hide after the receptacle's own occupant handling, which
        // force-re-enables the occupant's anim controller (see patch below).
        internal void ReapplyIconHide()
        {
            if (hiddenOccupant != null)
                Storage.MakeItemInvisible(hiddenOccupant, true, false);
        }
    }

    // SingleEntityReceptacle.PositionOccupyingObject ends with a hard
    // enabled=false/enabled=true toggle on the occupant's anim controller
    // (to force batch re-registration after moving it), which un-hides the
    // displayed garment's item icon every time it runs. Re-apply the hide
    // immediately afterwards on mannequins that are wearing their garment.
    [HarmonyPatch(typeof(SingleEntityReceptacle), "PositionOccupyingObject")]
    public static class Mannequin_PositionOccupyingObject_Patch
    {
        public static void Postfix(SingleEntityReceptacle __instance)
        {
            MannequinDecor md = __instance.GetComponent<MannequinDecor>();
            if (md != null) md.ReapplyIconHide();
        }
    }

    // Build-menu strings + placement (Furniture tab, right next to the Item
    // Pedestal it's descended from).
    [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
    public static class Mannequin_GeneratedBuildings
    {
        private static bool _added;

        public static void Prefix()
        {
            if (_added) return;
            _added = true;

            string p = "STRINGS.BUILDINGS.PREFABS." + MannequinConfig.ID.ToUpperInvariant() + ".";
            Strings.Add(p + "NAME", "Mannequin");
            Strings.Add(p + "DESC",
                "Clothing that sits in a box helps no one. Clothing on display sets a standard.");
            Strings.Add(p + "EFFECT",
                "Displays one piece of unused clothing. A dressed Mannequin significantly increases a room's Decor; fancier garments impress more.");

            BUILDINGS.PLANSUBCATEGORYSORTING[MannequinConfig.ID] = "decor";
            ModUtil.AddBuildingToPlanScreen("Furniture", MannequinConfig.ID, "decor", "ItemPedestal");
        }
    }

    // Research: Textile Production ("Clothing"), alongside the Refashionator
    // and the rest of the mod's clothing recipes.
    [HarmonyPatch(typeof(Db), "Initialize")]
    public static class Mannequin_Tech
    {
        public static void Postfix()
        {
            Tech tech = Db.Get().Techs.TryGet("Clothing");
            if (tech != null)
                tech.unlockedItemIDs.Add(MannequinConfig.ID);
            else
                Debug.LogWarning("[ProtectiveWear] Clothing tech not found; Mannequin stays unlocked from the start");
        }
    }
}
