using System.Collections.Generic;
using KSerialization;
using UnityEngine;

namespace CritterTurret
{
    // The turret's behaviour. Each Sim200ms tick it counts critters of the chosen
    // species in its room; if that count is over the population threshold (and the
    // building is powered/operational), it aims its head at the nearest matching
    // critter inside its firing area (a rotated rectangle that matches the on-screen
    // RangeVisualizer, including the turret's own level -- like the Robo-Miner) with
    // line of sight, and damages it on a cooldown. Kills drop meat normally.
    [SerializationConfig(MemberSerialization.OptIn)]
    public class CritterTurretBrain : KMonoBehaviour, ISim200ms, IThresholdSwitch
    {
        // Firing rectangle in local building space (rotates with the building).
        // y starts at 0 so it covers the turret's OWN level, like the Auto-Miner.
        public const int RANGE_MIN_X = -6, RANGE_MAX_X = 6;
        public const int RANGE_MIN_Y = 0, RANGE_MAX_Y = 8;

        private const float FIRE_INTERVAL = 1f;
        private const float DAMAGE_MIN = 8f;
        private const float DAMAGE_MAX = 12f;
        // The "gun" art's rest direction. If the head points the wrong way, change this.
        private const float ARM_REST_DEG = -90f;
        // attack_beam_fx_kanim's rest direction. If the particles spray the wrong way,
        // change this (try 90/-90/180).
        private const float FX_REST_DEG = 0f;
        // How many tiles the spray art spans unstretched; the fx is stretched along its
        // length (animWidth) by dist/FX_NATURAL_LEN so particles reach the target.
        private const float FX_NATURAL_LEN = 4f;
        private static readonly Color TURRET_RED = new Color(1f, 0.22f, 0.18f, 1f);

        // ---- persisted settings ----
        [Serialize] public float populationThreshold = 4f;
        [Serialize] public bool activateAboveThreshold = true;
        // 0 = adults only, 1 = adults + babies, 2 = babies only
        [Serialize] public int targetAge = 1;

        private float cooldown;
        private float currentCount;
        private HashSet<int> arcCells;
        private string laserLoopSound;
        private bool laserLoopPlaying;

        private KBatchedAnimController body;
        private GameObject armGo;
        private KBatchedAnimController armCtrl;
        private GameObject fxGo;
        private KBatchedAnimController fxCtrl;
        private bool fxOn;

        [MyCmpGet] private Operational operational;
        [MyCmpGet] private LoopingSounds loopingSounds;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            body = GetComponent<KBatchedAnimController>();
            if (body != null) body.TintColour = TURRET_RED;
            try { SetupArm(); }
            catch (System.Exception e) { Debug.LogWarning("[CritterTurret] arm rig setup failed (turret still fires): " + e); }

            // The dupes' attack sound. It's a LOOPING FMOD event (dupes start it when
            // their attack anim begins and stop it when it ends), so the turret runs it
            // while engaging a target rather than one-shotting it per shot.
            laserLoopSound = GlobalAssets.GetSound("AttackLaser_gun", false);

            Subscribe((int)GameHashes.RefreshUserMenu, OnRefreshUserMenu);
        }

        protected override void OnCleanUp()
        {
            SetLaserLoop(false);
            HideBeamFx();
            base.OnCleanUp();
        }

        private void SetLaserLoop(bool on)
        {
            if (loopingSounds == null || string.IsNullOrEmpty(laserLoopSound)) return;
            if (on == laserLoopPlaying) return;
            if (on) loopingSounds.StartSound(laserLoopSound);
            else loopingSounds.StopSound(laserLoopSound);
            laserLoopPlaying = on;
        }

        // One status-panel button cycling the age category the turret fires at:
        // Adults / Adults & Babies / Babies. (Per-species tick-box selection is a
        // future menu.)
        private void OnRefreshUserMenu(object data)
        {
            string ageLabel = targetAge == 0 ? "Adults" : (targetAge == 2 ? "Babies" : "Adults & Babies");
            Game.Instance.userMenu.AddButton(gameObject, new KIconButtonMenu.ButtonInfo(
                "action_mirror", "Target: " + ageLabel, OnCycleAge,
                global::Action.NumActions, null, null, null,
                "Cycle which critters this turret fires at: adults only, adults and babies, or babies only.",
                true));
        }

        private void OnCycleAge()
        {
            targetAge = (targetAge + 1) % 3;
            Game.Instance.userMenu.Refresh(gameObject);
        }

        private bool IsBaby(GameObject go)
        {
            return go.GetSMI<BabyMonitor.Instance>() != null;
        }

        private void SetupArm()
        {
            if (body == null) return;
            body.SetSymbolVisiblity("gun_target", false);
            armGo = new GameObject(body.name + ".gun");
            armGo.SetActive(false);
            armGo.transform.SetParent(transform);
            var kpid = armGo.AddComponent<KPrefabID>();
            kpid.PrefabTag = new Tag(armGo.name);
            armCtrl = armGo.AddComponent<KBatchedAnimController>();
            armCtrl.AnimFiles = body.AnimFiles;
            armCtrl.initialAnim = "gun";
            armCtrl.sceneLayer = body.sceneLayer;
            armCtrl.TintColour = TURRET_RED;
            armCtrl.isMovable = true;
            armGo.SetActive(true);
            armCtrl.Play("gun", KAnim.PlayMode.Loop);
            PositionArm();
        }

        private void PositionArm()
        {
            if (body == null || armGo == null) return;
            Matrix4x4 m = body.GetSymbolTransform("gun_target", out bool _);
            Vector3 p = m.GetColumn(3);
            p.z = body.transform.position.z;
            armGo.transform.position = p;
        }

        private Vector3 Muzzle() { return armGo != null ? armGo.transform.position : transform.position; }

        private void AimArmAt(Vector3 targetPos)
        {
            if (armGo == null) return;
            PositionArm();
            Vector3 from = armGo.transform.position;
            Vector2 d = new Vector2(targetPos.x - from.x, targetPos.y - from.y);
            if (d.sqrMagnitude < 0.0001f) return;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            armGo.transform.rotation = Quaternion.Euler(0f, 0f, ang + ARM_REST_DEG);
        }

        public void Sim200ms(float dt)
        {
            int turretCell = OriginCell();
            if (!Grid.IsValidCell(turretCell)) return;

            currentCount = CountMatchingCrittersInRoom(turretCell);
            bool overThreshold = activateAboveThreshold
                ? currentCount > populationThreshold
                : currentCount < populationThreshold;
            bool armed = operational != null && operational.IsOperational && overThreshold;
            if (operational != null) operational.SetActive(armed);

            cooldown -= dt;
            if (!armed) { SetLaserLoop(false); HideBeamFx(); return; }

            GameObject target = AcquireTarget(turretCell);
            if (target == null) { SetLaserLoop(false); HideBeamFx(); return; }

            SetLaserLoop(true);
            Vector3 aimPos = TargetCenter(target);
            AimArmAt(aimPos);
            UpdateBeamFx(aimPos);
            if (cooldown > 0f) return;
            FireAt(target);
            cooldown = FIRE_INTERVAL;
        }

        private int OriginCell()
        {
            Building b = GetComponent<Building>();
            return b != null ? b.GetCell() : Grid.PosToCell(gameObject);
        }

        // The firing area: the local rectangle rotated by the building's orientation.
        // Built once (geometry is static); line of sight is still checked live.
        private void BuildArcCells()
        {
            arcCells = new HashSet<int>();
            int origin = OriginCell();
            Rotatable rot = GetComponent<Rotatable>();
            for (int y = RANGE_MIN_Y; y <= RANGE_MAX_Y; y++)
                for (int x = RANGE_MIN_X; x <= RANGE_MAX_X; x++)
                {
                    CellOffset off = new CellOffset(x, y);
                    if (rot != null) off = rot.GetRotatedCellOffset(off);
                    int c = Grid.OffsetCell(origin, off);
                    if (Grid.IsValidCell(c)) arcCells.Add(c);
                }
        }

        private List<KPrefabID> RoomCreatures(int cell)
        {
            if (Game.Instance == null || Game.Instance.roomProber == null) return null;
            CavityInfo cavity = Game.Instance.roomProber.GetCavityForCell(cell);
            return cavity?.creatures;
        }

        private bool Matches(KPrefabID kpid)
        {
            bool isBaby = IsBaby(kpid.gameObject);
            if (targetAge == 0 && isBaby) return false;   // adults only
            if (targetAge == 2 && !isBaby) return false;  // babies only
            return true;                                  // all species, matching age
        }

        private float CountMatchingCrittersInRoom(int cell)
        {
            var list = RoomCreatures(cell);
            if (list == null) return 0f;
            int n = 0;
            foreach (var kpid in list)
                if (kpid != null && Matches(kpid)) n++;
            return n;
        }

        private GameObject AcquireTarget(int turretCell)
        {
            var list = RoomCreatures(turretCell);
            if (list == null) return null;
            if (arcCells == null) BuildArcCells();

            Vector3 from = Muzzle();
            GameObject best = null;
            float bestDist = float.MaxValue;

            foreach (var kpid in list)
            {
                if (kpid == null || !Matches(kpid)) continue;
                GameObject go = kpid.gameObject;
                if (go.GetComponent<CreatureBrain>() == null) continue; // critters only, never dupes
                if (go.GetComponent<Health>() == null) continue;

                int targetCell = Grid.PosToCell(go);
                if (!arcCells.Contains(targetCell)) continue;
                if (!HasLineOfSight(turretCell, targetCell)) continue;

                float dist = Vector3.Distance(from, go.transform.position);
                if (dist < bestDist) { bestDist = dist; best = go; }
            }
            return best;
        }

        private void FireAt(GameObject target)
        {
            Health health = target.GetComponent<Health>();
            if (health == null) return;
            health.Damage(Random.Range(DAMAGE_MIN, DAMAGE_MAX));
            // Sound comes from the AttackLaser_gun loop (SetLaserLoop); only if that
            // event failed to resolve do we fall back to a per-shot metallic one-shot.
            if (string.IsNullOrEmpty(laserLoopSound))
            {
                string s = GlobalAssets.GetSound("Building_Dmg_Metal", false);
                if (!string.IsNullOrEmpty(s)) KFMOD.PlayOneShot(s, Muzzle());
            }
        }

        // The shot graphic: the dupes' own attack-beam particle spray
        // (attack_beam_fx_kanim / "loop" -- the white particles the multitool shoots
        // in attack mode). Runs from the muzzle, aimed at the target, while engaging.
        private void EnsureBeamFx()
        {
            if (fxGo != null) return;
            KAnimFile anim = Assets.GetAnim("attack_beam_fx_kanim");
            if (anim == null) return;
            fxGo = new GameObject(name + ".beam_fx");
            fxGo.SetActive(false);
            fxGo.transform.SetParent(transform);
            var kpid = fxGo.AddComponent<KPrefabID>();
            kpid.PrefabTag = new Tag(fxGo.name);
            fxCtrl = fxGo.AddComponent<KBatchedAnimController>();
            fxCtrl.AnimFiles = new KAnimFile[] { anim };
            fxCtrl.initialAnim = "loop";
            fxCtrl.sceneLayer = Grid.SceneLayer.FXFront;
            fxCtrl.isMovable = true;
        }

        // Aim at the critter's visual center (its collider), not its transform origin
        // (which sits at its feet and makes the spray point low).
        private static Vector3 TargetCenter(GameObject go)
        {
            var col = go.GetComponent<KBoxCollider2D>();
            if (col != null) return go.transform.position + (Vector3)col.offset;
            return go.transform.position + new Vector3(0f, 0.3f, 0f);
        }

        private void UpdateBeamFx(Vector3 targetPos)
        {
            EnsureBeamFx();
            if (fxGo == null) return;

            Vector3 from = Muzzle();
            from.z = Grid.GetLayerZ(Grid.SceneLayer.FXFront);
            fxGo.transform.position = from;
            Vector2 d = new Vector2(targetPos.x - from.x, targetPos.y - from.y);
            if (d.sqrMagnitude > 0.0001f)
            {
                float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                fxGo.transform.rotation = Quaternion.Euler(0f, 0f, ang + FX_REST_DEG);
                // Stretch the spray along its length so it reaches the target
                // (animWidth scales the anim's local X before rotation).
                fxCtrl.animWidth = Mathf.Max(0.5f, d.magnitude / FX_NATURAL_LEN);
                fxCtrl.SetDirty();
            }

            if (!fxOn)
            {
                fxGo.SetActive(true);
                fxCtrl.Play("loop", KAnim.PlayMode.Loop);
                fxOn = true;
            }
        }

        private void HideBeamFx()
        {
            if (!fxOn) return;
            if (fxGo != null) fxGo.SetActive(false);
            fxOn = false;
        }

        private bool HasLineOfSight(int a, int b)
        {
            Grid.CellToXY(a, out int x0, out int y0);
            Grid.CellToXY(b, out int x1, out int y1);
            int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx - dy, cx = x0, cy = y0;
            while (true)
            {
                bool isEndpoint = (cx == x0 && cy == y0) || (cx == x1 && cy == y1);
                if (!isEndpoint)
                {
                    int c = Grid.XYToCell(cx, cy);
                    if (Grid.IsValidCell(c) && Grid.Solid[c]) return false;
                }
                if (cx == x1 && cy == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; cx += sx; }
                if (e2 < dx) { err += dx; cy += sy; }
            }
            return true;
        }

        // ---- IThresholdSwitch (population-threshold side screen) ----
        public float Threshold { get { return populationThreshold; } set { populationThreshold = value; } }
        public bool ActivateAboveThreshold { get { return activateAboveThreshold; } set { activateAboveThreshold = value; } }
        public float CurrentValue { get { return currentCount; } }
        public float RangeMin { get { return 0f; } }
        public float RangeMax { get { return 32f; } }
        public LocString Title { get { return (LocString)"STRINGS.UI.UISIDESCREENS.CRITTERTURRET.OPEN_FIRE"; } }
        public LocString ThresholdValueName { get { return (LocString)"STRINGS.UI.UISIDESCREENS.CRITTERTURRET.VALUE_NAME"; } }
        public string AboveToolTip { get { return "Fire when the room's critter count is above {0}"; } }
        public string BelowToolTip { get { return "Fire when the room's critter count is below {0}"; } }
        public ThresholdScreenLayoutType LayoutType { get { return (ThresholdScreenLayoutType)0; } }
        public int IncrementScale { get { return 1; } }
        public NonLinearSlider.Range[] GetRanges { get { return NonLinearSlider.GetDefaultRange(RangeMax); } }
        public float GetRangeMinInputField() { return RangeMin; }
        public float GetRangeMaxInputField() { return RangeMax; }
        public LocString ThresholdValueUnits() { return ""; }
        public string Format(float value, bool units) { return value.ToString(); }
        public float ProcessedSliderValue(float input) { return Mathf.Round(input); }
        public float ProcessedInputValue(float input) { return Mathf.Round(input); }
    }
}
