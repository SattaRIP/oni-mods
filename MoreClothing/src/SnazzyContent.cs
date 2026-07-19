using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using KMod;
using UnityEngine;

namespace SnazzySwimwear
{
    // (Snazzy Swimwear's UserMod2 was removed in the More Clothing merge --
    // MoreClothingMod in ProtectiveWearContent.cs does the single PatchAll.)

    // Names/descriptions for the two new equippables and their recipes.
    // Added before the DB is built so equipment lookup finds them.
    [HarmonyPatch(typeof(Db), "Initialize")]
    public static class Db_Initialize_Strings
    {
        private static bool _added;

        public static void Prefix()
        {
            if (_added) return;
            _added = true;

            Add(SnazzySwimwearConfig.ID, "Snazzy Swimwear",
                "The same practical wetsuit, refashioned with gold sequin trim.",
                "A refashioned Swimwear. Shrugs off the soggy morale hit from wading through liquid, and looks a great deal better doing it.",
                "Swimwear");
            Add(SnazzyRubberBootsConfig.ID, "Snazzy Rubber Boots",
                "Rubber boots given a gold sequin finish at the Refashionator.",
                "Refashioned Rubber Boots. The same sure footing, now with sparkle.",
                "Boots");
            Add(SnazzyShoesConfig.ID, "Shoes",
                "Sharp black-and-gold dress shoes. Pure flourish -- all decor, no protection.",
                "Spin fiber into sharp black-and-gold dress shoes at the Textile Loom, purely to lift the room's mood.",
                "Shoes");
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

    // Register the two upgrade recipes at the Clothing Refashionator:
    //   Swimwear     + 5 kg Reed Fiber -> Snazzy Swimwear
    //   Rubber Boots + 3 kg Reed Fiber -> Snazzy Rubber Boots
    // The rubber that made the base item carries over for free, since the base
    // item is itself an ingredient.
    [HarmonyPatch(typeof(ClothingAlterationStationConfig), "ConfigureRecipes")]
    public static class Refashionator_Recipes
    {
        private const string STATION = "ClothingAlterationStation";

        public static void Postfix()
        {
            AddUpgrade("Swimwear", "DrySuit", SnazzySwimwearConfig.ID, 5f);
            AddUpgrade("RubberBoots", "RubberBoots", SnazzyRubberBootsConfig.ID, 3f);
        }

        private static void AddUpgrade(string label, string baseId, string snazzyId, float fiber)
        {
            ComplexRecipe.RecipeElement[] input =
            {
                new ComplexRecipe.RecipeElement(baseId.ToTag(), 1f),
                new ComplexRecipe.RecipeElement(ProtectiveWear.Fibers.Options(), fiber),
            };
            ComplexRecipe.RecipeElement[] output =
            {
                new ComplexRecipe.RecipeElement(snazzyId.ToTag(), 1f),
            };

            string id = ComplexRecipeManager.MakeRecipeID(STATION, input, output);
            string desc = "STRINGS.EQUIPMENT.PREFABS." + snazzyId.ToUpperInvariant() + ".RECIPE_DESC";
            new ComplexRecipe(id, input, output)
            {
                time = 40f,
                description = Strings.Get(desc),
                nameDisplay = ComplexRecipe.RecipeNameDisplay.Result,
                fabricators = new List<Tag> { (Tag)STATION },
                sortOrder = 5,
                requiredTech = ProtectiveWear.Refashionator_Recipes.TECH_TEXTILES,
            };
        }
    }

    // Shoes are spun from reed fiber at the Textile Loom (ClothingFabricator),
    // not refashioned from an existing garment.
    [HarmonyPatch(typeof(ClothingFabricatorConfig), "ConfigureRecipes")]
    public static class TextileLoom_Recipes
    {
        private const string LOOM = "ClothingFabricator"; // Textile Loom

        public static void Postfix()
        {
            ComplexRecipe.RecipeElement[] input =
            {
                new ComplexRecipe.RecipeElement(ProtectiveWear.Fibers.Options(), 8f),
            };
            ComplexRecipe.RecipeElement[] output =
            {
                new ComplexRecipe.RecipeElement(SnazzyShoesConfig.ID.ToTag(), 1f),
            };

            string id = ComplexRecipeManager.MakeRecipeID(LOOM, input, output);
            string desc = "STRINGS.EQUIPMENT.PREFABS." + SnazzyShoesConfig.ID.ToUpperInvariant() + ".RECIPE_DESC";
            new ComplexRecipe(id, input, output)
            {
                time = 40f,
                description = Strings.Get(desc),
                nameDisplay = ComplexRecipe.RecipeNameDisplay.Result,
                fabricators = new List<Tag> { (Tag)LOOM },
                sortOrder = 6,
                requiredTech = ProtectiveWear.Refashionator_Recipes.TECH_TEXTILES,
            };
        }
    }
}
