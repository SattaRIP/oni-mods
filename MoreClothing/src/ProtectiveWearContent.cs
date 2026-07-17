using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using KMod;
using UnityEngine;

namespace ProtectiveWear
{
    // Single mod entry point for all of More Clothing; the one PatchAll picks
    // up every Harmony patch class in the assembly, including the Snazzy
    // Swimwear namespace (whose own UserMod2 was removed in the merge).
    public class MoreClothingMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    // Names/descriptions for the two new equippables and their recipes, added
    // before the DB is built so equipment lookup finds them.
    [HarmonyPatch(typeof(Db), "Initialize")]
    public static class Db_Initialize_Strings
    {
        private static bool _added;

        public static void Prefix()
        {
            if (_added) return;
            _added = true;

            Add(UpgradedWarmCoatConfig.ID, "Winter Coat",
                "A Warm Sweater refashioned into a heavier insulated coat with markedly better cold protection. Handsomely tailored: a little room decor while worn.",
                "Refashion a Warm Sweater into a thicker insulated coat that shrugs off the cold far better -- and looks good doing it.",
                "Coat");

            Add(EVASuitConfig.ID, "Soft Suit",
                "A lightweight sealed suit. No checkpoint required. Protection from cold, heat, radiation and airborne germs; keeps its wearer dry (no Sopping Wet or Soggy Feet) and shrugs off eye irritation; a large breath reserve and fewer bathroom breaks -- but no air tank, so it only delays suffocation. Covers head to toe: the suit's own boots fill the shoes slot while it's worn.",
                "Combine a Warm Sweater, Swimwear, Rubber Boots, an Oxygen Mask and fiber into a checkpoint-free Soft Suit: cold/heat/radiation/germ shielding, immunity to wet and eye-irritation effects, a big breath reserve, slower bladder fill, and built-in boots.",
                "Suit");

            Add(EVASuitBootsConfig.ID, "Soft Suit Boots",
                "The Soft Suit's built-in footwear. Comes with the suit, goes with the suit.",
                "Part of the Soft Suit.",
                "Boots");
        }

        private static void Add(string id, string name, string desc, string recipeDesc, string generic)
        {
            string p = "STRINGS.EQUIPMENT.PREFABS." + id.ToUpperInvariant() + ".";
            Strings.Add(p + "NAME", name);
            Strings.Add(p + "DESC", desc);
            Strings.Add(p + "RECIPE_DESC", recipeDesc);
            // The user menu formats its buttons as "Unequip {GENERICNAME}" --
            // without this key the button renders a giant MISSING.STRINGS path.
            Strings.Add(p + "GENERICNAME", generic);
        }
    }

    // All fibers our clothing recipes accept interchangeably: vanilla Reed
    // Fiber always, Feather Fiber when its DLC content is active, and Rayon
    // Fiber when Ronivans Legacy (which adds it) is loaded. Resolved at
    // recipe-registration time so absent materials never appear as dead
    // ingredient options.
    public static class Fibers
    {
        public static Tag[] Options()
        {
            List<Tag> tags = new List<Tag> { (Tag)"BasicFabric" };
            if (DlcManager.IsAllContentSubscribed(DlcManager.DLC4))
                tags.Add((Tag)"FeatherFabric");
            if (System.Type.GetType(
                    "Dupes_Industrial_Overhaul.Chemical_Processing.Chemicals.RayonFabricConfig, RonivansLegacy_ChemicalProcessing") != null)
                tags.Add((Tag)"RayonFiber");
            return tags.ToArray();
        }
    }

    // Register the upgrade recipes at the Clothing Refashionator:
    //   Warm Sweater                                          + 5 kg fiber  -> Upgraded Warm Coat
    //   Warm Sweater + Swimwear + Rubber Boots + Oxygen Mask  + 10 kg fiber -> EVA Suit
    // (fiber = reed/feather/rayon, see Fibers). The material that made the
    // base garments carries over for free, since the base garments are
    // themselves ingredients. The boots justify the suit's built-in footwear
    // (it fills the shoes slot while worn). Research: the coat sits with
    // Textile Production ("Clothing", same tech as the Refashionator), the
    // suit with Hazard Protection ("Suits").
    [HarmonyPatch(typeof(ClothingAlterationStationConfig), "ConfigureRecipes")]
    public static class Refashionator_Recipes
    {
        private const string STATION = "ClothingAlterationStation";
        private const string WARM_SWEATER = "Warm_Vest";
        private const string SWIMWEAR = "DrySuit";
        private const string BOOTS = "RubberBoots";
        private const string MASK = "Oxygen_Mask";

        public const string TECH_TEXTILES = "Clothing"; // Textile Production
        public const string TECH_HAZARD = "Suits";      // Hazard Protection

        public static void Postfix()
        {
            Tag[] fibers = Fibers.Options();

            AddRecipe(UpgradedWarmCoatConfig.ID, TECH_TEXTILES, new[]
            {
                new ComplexRecipe.RecipeElement(WARM_SWEATER.ToTag(), 1f),
                new ComplexRecipe.RecipeElement(fibers, 5f),
            });

            AddRecipe(EVASuitConfig.ID, TECH_HAZARD, new[]
            {
                new ComplexRecipe.RecipeElement(WARM_SWEATER.ToTag(), 1f),
                new ComplexRecipe.RecipeElement(SWIMWEAR.ToTag(), 1f),
                new ComplexRecipe.RecipeElement(BOOTS.ToTag(), 1f),
                new ComplexRecipe.RecipeElement(MASK.ToTag(), 1f),
                new ComplexRecipe.RecipeElement(fibers, 10f),
            });
        }

        private static void AddRecipe(string outputId, string tech, ComplexRecipe.RecipeElement[] input)
        {
            ComplexRecipe.RecipeElement[] output =
            {
                new ComplexRecipe.RecipeElement(outputId.ToTag(), 1f),
            };

            string id = ComplexRecipeManager.MakeRecipeID(STATION, input, output);
            string desc = "STRINGS.EQUIPMENT.PREFABS." + outputId.ToUpperInvariant() + ".RECIPE_DESC";
            new ComplexRecipe(id, input, output)
            {
                time = 60f,
                description = Strings.Get(desc),
                nameDisplay = ComplexRecipe.RecipeNameDisplay.Result,
                fabricators = new List<Tag> { (Tag)STATION },
                sortOrder = 6,
                requiredTech = tech,
            };
        }
    }
}
