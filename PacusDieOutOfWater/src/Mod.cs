using HarmonyLib;
using KMod;
using System.Reflection;
using UnityEngine;

namespace PacusDieOutOfWater
{
    public class PacusDieOutOfWaterMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    // Apply to every Pacu prefab — BasePacuConfig.CreatePrefab is the common path
    // used by standard, tropical, cleaner (Gulp Fish), and prehistoric pacu variants.
    [HarmonyPatch(typeof(BasePacuConfig), nameof(BasePacuConfig.CreatePrefab))]
    public static class BasePacuConfig_CreatePrefab_AttachDrain
    {
        public static void Postfix(GameObject __result)
        {
            if (__result == null) return;
            __result.AddOrGet<OutOfWaterHealthDrain>();
        }
    }

    public class OutOfWaterHealthDrain : KMonoBehaviour, ISim1000ms
    {
        private const float DAMAGE_PER_SECOND     = 1f;       // tune-friendly: 1 HP/s; adult dies in ~100s
        private const float LIQUID_MIN_KG         = 35f;      // ONI cell mass when ~10% filled; well below the 350kg swim threshold
        private const float GRACE_PERIOD_SECONDS  = 60f;      // pacu must be out of water this long before damage starts

        // Per-pacu accumulator of continuous seconds out of water.
        // Resets to 0 whenever the pacu is back in liquid, so brief air gaps
        // (e.g. conveyor transfers) won't injure them. Once the counter exceeds
        // GRACE_PERIOD_SECONDS, the existing damage logic kicks in.
        private float secondsOutOfWater = 0f;

        public void Sim1000ms(float dt)
        {
            int cell = Grid.PosToCell(transform.GetPosition());
            if (!Grid.IsValidCell(cell)) return;

            bool inLiquid = Grid.Element[cell] != null
                            && Grid.Element[cell].IsLiquid
                            && Grid.Mass[cell] >= LIQUID_MIN_KG;
            if (inLiquid) { secondsOutOfWater = 0f; return; }

            secondsOutOfWater += dt;
            if (secondsOutOfWater < GRACE_PERIOD_SECONDS) return;

            var health = GetComponent<Health>();
            if (health == null || health.hitPoints <= 0f) return;

            health.Damage(DAMAGE_PER_SECOND * dt);
        }
    }
}
