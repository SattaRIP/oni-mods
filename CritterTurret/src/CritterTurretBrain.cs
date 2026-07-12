using System.Collections.Generic;
using KSerialization;
using UnityEngine;

namespace CritterTurret
{
    // The turret's behaviour. Each Sim200ms tick it counts checked-species critters
    // in its room; if that count is over the population threshold (and the building
    // is powered/operational), it locks onto a matching critter inside its firing
    // area (a rotated rectangle that matches the on-screen RangeVisualizer,
    // including the turret's own level -- like the Robo-Miner) with line of sight,
    // and damages it on a cooldown until it dies or escapes. Kills drop meat
    // normally.
    [SerializationConfig(MemberSerialization.OptIn)]
    public class CritterTurretBrain : KMonoBehaviour, ISim200ms, IThresholdSwitch
    {
        // Firing rectangle in local building space (rotates with the building).
        // y starts at 0 so it covers the turret's OWN level, like the Auto-Miner.
        public const int RANGE_MIN_X = -6, RANGE_MAX_X = 6;
        public const int RANGE_MIN_Y = 0, RANGE_MAX_Y = 8;

        private const float FIRE_INTERVAL = 1f;
        // How long the shot flash (particles + sound) stays on after each damage
        // tick. Sim runs at 200ms, so 0.3f shows the fx for two sim ticks (~0.4s).
        private const float BURST_DURATION = 0.3f;
        // Damage is a fraction of the target's max HP so every critter dies in the
        // same number of shots (~3), whether it's a 5 HP Shine Bug or a 400 HP
        // Smooth Hatch. Floored at DAMAGE_MIN so tiny critters still register hits.
        private const float DAMAGE_PCT_MIN = 0.35f;
        private const float DAMAGE_PCT_MAX = 0.45f;
        private const float DAMAGE_MIN = 2f;
        // The "gun" art's rest direction. If the head points the wrong way, change this.
        private const float ARM_REST_DEG = -90f;
        // attack_beam_fx_kanim's rest direction. If the particles spray the wrong way,
        // change this (try 90/-90/180).
        private const float FX_REST_DEG = 0f;
        // How many tiles the spray art spans unstretched; the fx is stretched along its
        // length (animWidth) by dist/FX_NATURAL_LEN so particles reach the target.
        private const float FX_NATURAL_LEN = 4f;

        // ---- persisted settings ----
        [Serialize] public float populationThreshold = 4f;
        [Serialize] public bool activateAboveThreshold = true;

        private float cooldown;
        private float burstRemaining;
        private float currentCount;
        // The critter currently being fired at. Held until it dies or leaves the
        // firing area / line of sight, instead of re-picking nearest every tick.
        private GameObject lockedTarget;
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
        [MyCmpGet] private TreeFilterable speciesFilter;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            body = GetComponent<KBatchedAnimController>();
            try { SetupArm(); }
            catch (System.Exception e) { Debug.LogWarning("[CritterTurret] arm rig setup failed (turret still fires): " + e); }

            // The dupes' attack sound. It's a LOOPING FMOD event (dupes start it when
            // their attack anim begins and stop it when it ends), so the turret runs it
            // while engaging a target rather than one-shotting it per shot.
            laserLoopSound = GlobalAssets.GetSound("AttackLaser_gun", false);

            // Make sure the Other Critters checklist category exists before the
            // side screen can target this turret. A new turret's checklist starts
            // with every box unchecked -- it holds fire until told what to shoot.
            CritterTurretTags.EnsureOtherCrittersRegistered();
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
            // TransferArm renders in front of Building/BuildingFront -- it's the
            // layer the Auto-Sweeper's rotating arm uses. On the body's own layer
            // the draw order ties and the turret's neck can draw over the gun.
            armCtrl.sceneLayer = Grid.SceneLayer.TransferArm;
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
            // Must match the controller's scene layer, like the beam fx does with
            // FXFront; keeping the body's z would put the gun back behind the neck.
            p.z = Grid.GetLayerZ(Grid.SceneLayer.TransferArm);
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

            cooldown -= dt;
            if (!armed)
            {
                if (operational != null) operational.SetActive(false);
                SetLaserLoop(false); HideBeamFx(); return;
            }

            GameObject target = AcquireTarget(turretCell);
            // Active (= 120 W draw + self-heat) only while actually engaging a
            // target, like the Robo-Miner while digging; armed-but-idle is free.
            if (operational != null) operational.SetActive(target != null);
            if (target == null) { SetLaserLoop(false); HideBeamFx(); return; }

            Vector3 aimPos = TargetCenter(target);
            AimArmAt(aimPos);

            if (cooldown <= 0f)
            {
                FireAt(target);
                cooldown = FIRE_INTERVAL;
                burstRemaining = BURST_DURATION;
            }

            // Particles + laser sound only flash for a short burst around each
            // damage tick instead of running continuously while engaging.
            burstRemaining -= dt;
            if (burstRemaining > 0f)
            {
                SetLaserLoop(true);
                UpdateBeamFx(aimPos);
            }
            else
            {
                SetLaserLoop(false);
                HideBeamFx();
            }
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

        // Checked in the species checklist = shot. The side screen stores
        // individual species prefab tags (babies are their own rows, e.g.
        // "Hatchling"); the category-tag fallbacks cover the whole-category
        // checkbox state some codepaths write instead of the leaves.
        private bool Matches(KPrefabID kpid)
        {
            // Robots aren't critters: never shot, never counted, no checkbox.
            if (kpid.HasTag(GameTags.Robot)) return false;
            if (speciesFilter == null) return true;
            if (speciesFilter.ContainsTag(kpid.PrefabTag)) return true;
            bool bagable = kpid.HasTag(GameTags.BagableCreature);
            bool swimming = kpid.HasTag(GameTags.SwimmingCreature);
            if (bagable && speciesFilter.ContainsTag(GameTags.BagableCreature)) return true;
            if (swimming && speciesFilter.ContainsTag(GameTags.SwimmingCreature)) return true;
            return !bagable && !swimming && speciesFilter.ContainsTag(CritterTurretTags.OtherCritters);
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

        // Lock-on: keep hammering the same critter until it dies or escapes the
        // firing area / line of sight; only then pick the nearest valid target.
        private GameObject AcquireTarget(int turretCell)
        {
            if (arcCells == null) BuildArcCells();
            if (StillValidTarget(lockedTarget, turretCell)) return lockedTarget;
            lockedTarget = null;

            var list = RoomCreatures(turretCell);
            if (list == null) return null;

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
            lockedTarget = best;
            return best;
        }

        private bool StillValidTarget(GameObject go, int turretCell)
        {
            if (go == null) return false; // includes Unity-destroyed objects
            var kpid = go.GetComponent<KPrefabID>();
            if (kpid == null || kpid.HasTag(GameTags.Dead) || !Matches(kpid)) return false;
            var health = go.GetComponent<Health>();
            if (health == null || health.hitPoints <= 0f) return false;
            int cell = Grid.PosToCell(go);
            return arcCells.Contains(cell) && HasLineOfSight(turretCell, cell);
        }

        private void FireAt(GameObject target)
        {
            Health health = target.GetComponent<Health>();
            if (health == null) return;
            float dmg = health.maxHitPoints * Random.Range(DAMAGE_PCT_MIN, DAMAGE_PCT_MAX);
            health.Damage(Mathf.Max(dmg, DAMAGE_MIN));
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
        public float RangeMax { get { return 200f; } }
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
