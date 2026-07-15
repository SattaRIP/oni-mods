using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using KMod;
using UnityEngine;

namespace ProtectiveWear
{
    public class ProtectiveWearMod : UserMod2
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
                "A Warm Sweater refashioned into a heavier insulated coat with markedly better cold protection.",
                "Refashion a Warm Sweater into a thicker insulated coat that shrugs off the cold far better.",
                "Coat");

            Add(EVASuitConfig.ID, "Soft Suit",
                "A lightweight sealed suit. No checkpoint required. Protection from cold, heat, radiation and airborne germs; keeps its wearer dry (no Sopping Wet or Soggy Feet) and shrugs off eye irritation; a large breath reserve and fewer bathroom breaks -- but no air tank, so it only delays suffocation.",
                "Combine a Warm Sweater, Swimwear and reed fiber into a checkpoint-free Soft Suit: cold/heat/radiation/germ shielding, immunity to wet and eye-irritation effects, a big breath reserve, and slower bladder fill.",
                "Suit");
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

    // Register the upgrade recipes at the Clothing Refashionator:
    //   Warm Sweater             + 5 kg Reed Fiber              -> Upgraded Warm Coat
    //   Warm Sweater + Swimwear  + 10 kg Reed Fiber             -> EVA Suit
    // The material that made the base garments carries over for free, since the
    // base garments are themselves ingredients.
    [HarmonyPatch(typeof(ClothingAlterationStationConfig), "ConfigureRecipes")]
    public static class Refashionator_Recipes
    {
        private const string STATION = "ClothingAlterationStation";
        private const string FIBER = "BasicFabric"; // Reed Fiber
        private const string WARM_SWEATER = "Warm_Vest";
        private const string SWIMWEAR = "DrySuit";

        public static void Postfix()
        {
            AddRecipe(UpgradedWarmCoatConfig.ID, new[]
            {
                new ComplexRecipe.RecipeElement(WARM_SWEATER.ToTag(), 1f),
                new ComplexRecipe.RecipeElement(FIBER.ToTag(), 5f),
            });

            AddRecipe(EVASuitConfig.ID, new[]
            {
                new ComplexRecipe.RecipeElement(WARM_SWEATER.ToTag(), 1f),
                new ComplexRecipe.RecipeElement(SWIMWEAR.ToTag(), 1f),
                new ComplexRecipe.RecipeElement(FIBER.ToTag(), 10f),
            });
        }

        private static void AddRecipe(string outputId, ComplexRecipe.RecipeElement[] input)
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
            };
        }
    }
}
