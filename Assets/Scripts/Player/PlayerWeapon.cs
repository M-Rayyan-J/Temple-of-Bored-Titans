using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TempleOfBoredTitans.Core;
using TempleOfBoredTitans.Titans;
using TempleOfBoredTitans.UI;

namespace TempleOfBoredTitans.Player
{
    /// <summary>Handles the hunter's single firearm throughout the campaign.</summary>
    public sealed class PlayerWeapon : MonoBehaviour
    {
        [SerializeField] private Transform viewCamera;
        [SerializeField] private Transform weaponMount;
        [SerializeField] private Vector3 weaponModelRotationOffset = new Vector3(0f, -90f, 0f);
        [SerializeField, Min(1)] private int weaponTier;
        [SerializeField] private GameObject[] weaponModelPrefabs;
        private int ammunition;
        private int reserve;
        private int magazineSize;
        private int damage;
        private int pellets;
        private float spread;
        private float range;
        private float fireInterval;
        private float nextShotTime;
        private float firingRecoil;
        private Transform viewModel;
        private Light muzzleFlash;

        public string WeaponName { get; private set; }
        public int Ammunition { get { return ammunition; } }
        public int Reserve { get { return reserve; } }

        private void Start()
        {
            if (viewCamera == null)
            {
                Camera playerCamera = GetComponentInChildren<Camera>(true);
                if (playerCamera != null) viewCamera = playerCamera.transform;
                else if (Camera.main != null) viewCamera = Camera.main.transform;
                else
                {
                    Camera[] cameras = FindObjectsOfType<Camera>(true);
                    if (cameras.Length > 0) viewCamera = cameras[0].transform;
                }
            }
            ConfigureWeapon(weaponTier, true);
            BuildViewModel();
            RefreshHud();
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
            if (Mouse.current == null || Keyboard.current == null) return;
            if (Mouse.current.leftButton.wasPressedThisFrame) Fire();
        }

        private void LateUpdate()
        {
            if (weaponMount == null || viewCamera == null || viewModel == null) return;
            // Aim the held firearm along the same direction as the camera/crosshair,
            // even if the imported hand socket is authored pointing toward the floor.
            Quaternion aim = Quaternion.LookRotation(viewCamera.forward, Vector3.up);
            viewModel.rotation = Quaternion.Slerp(viewModel.rotation, aim, Time.deltaTime * 24f);
            firingRecoil = Mathf.MoveTowards(firingRecoil, 0f, Time.deltaTime * .5f);
            viewModel.position = weaponMount.position - viewCamera.forward * firingRecoil;
        }

        private void ConfigureWeapon(int tier, bool refill)
        {
            switch (tier)
            {
                case 0: WeaponName = "Rusted Sidearm"; magazineSize = 8; damage = 24; pellets = 1; spread = .4f; range = 32f; fireInterval = .45f; break;
                case 1: WeaponName = "Pistol"; magazineSize = 12; damage = 32; pellets = 1; spread = .25f; range = 40f; fireInterval = .32f; break;
                case 2: WeaponName = "Shotgun"; magazineSize = 7; damage = 13; pellets = 6; spread = 4.5f; range = 19f; fireInterval = .8f; break;
                case 3: WeaponName = "Repeater Rifle"; magazineSize = 20; damage = 22; pellets = 1; spread = .6f; range = 55f; fireInterval = .22f; break;
                case 4: WeaponName = "Rune Carbine"; magazineSize = 16; damage = 30; pellets = 1; spread = .2f; range = 60f; fireInterval = .3f; break;
                default: WeaponName = "Relic Cannon"; magazineSize = 5; damage = 80; pellets = 1; spread = .15f; range = 50f; fireInterval = 1f; break;
            }
            if (refill)
            {
                ammunition = magazineSize;
                reserve = Mathf.Max(reserve, magazineSize * 3);
            }
        }

        private void Fire()
        {
            if (Time.time < nextShotTime) return;
            if (viewCamera == null) return;
            nextShotTime = Time.time + fireInterval;
            firingRecoil = .09f;
            if (TempleAudioDirector.Instance != null) TempleAudioDirector.Instance.PlayGunshot(weaponTier == 2 || weaponTier == 5);
            if (muzzleFlash != null) StartCoroutine(FlashMuzzle());

            for (int i = 0; i < pellets; i++)
            {
                Vector3 direction = viewCamera.forward;
                if (spread > 0f)
                    direction = Quaternion.Euler(Random.Range(-spread, spread), Random.Range(-spread, spread), 0f) * direction;
                RaycastHit[] hits = Physics.RaycastAll(viewCamera.position, direction, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, delegate(RaycastHit a, RaycastHit b) { return a.distance.CompareTo(b.distance); });
                for (int hitIndex = 0; hitIndex < hits.Length; hitIndex++)
                {
                    if (hits[hitIndex].transform.IsChildOf(transform)) continue;
                    TitanBossHealth titan = hits[hitIndex].collider.GetComponentInParent<TitanBossHealth>();
                    if (titan == null)
                    {
                        TitanAI titanAI = hits[hitIndex].collider.GetComponentInParent<TitanAI>();
                        if (titanAI != null)
                            titan = titanAI.GetComponent<TitanBossHealth>() ?? titanAI.gameObject.AddComponent<TitanBossHealth>();
                    }
                    if (titan != null)
                    {
                        titan.TakeDamage(damage, hits[hitIndex].point, hits[hitIndex].normal);
                    }
                    break;
                }
            }
            RefreshHud();
        }

        private void BuildViewModel()
        {
            if (viewCamera == null) return;
            if (viewModel != null) Destroy(viewModel.gameObject);
            bool mountedOnCharacter = weaponMount != null;
            GameObject root = new GameObject((mountedOnCharacter ? "Held " : "First Person ") + WeaponName);
            viewModel = root.transform;
            viewModel.SetParent(mountedOnCharacter ? weaponMount : viewCamera, false);
            viewModel.localPosition = mountedOnCharacter ? Vector3.zero : new Vector3(.30f, -.28f, .62f);
            viewModel.localScale = mountedOnCharacter ? Vector3.one : Vector3.one * 1.35f;
            viewModel.localRotation = Quaternion.identity;
            if (weaponModelPrefabs != null && weaponModelPrefabs.Length > 0)
            {
                int modelIndex = Mathf.Clamp(weaponTier, 0, weaponModelPrefabs.Length - 1);
                GameObject prefab = weaponModelPrefabs[modelIndex];
                if (prefab != null)
                {
                    GameObject model = Instantiate(prefab, viewModel, false);
                    model.name = WeaponName + " Asset Model";
                    model.transform.localPosition = Vector3.zero;
                    // These imported gun meshes use +X as their barrel axis;
                    // rotate that axis onto the camera/crosshair's +Z direction.
                    model.transform.localRotation = Quaternion.Euler(weaponModelRotationOffset);
                    model.transform.localScale = mountedOnCharacter ? Vector3.one : Vector3.one * .85f;
                    foreach (var collider in model.GetComponentsInChildren<Collider>(true)) Destroy(collider);
                    foreach (var renderer in model.GetComponentsInChildren<Renderer>(true)) renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    GameObject assetFlash = new GameObject("Muzzle Flash");
                    assetFlash.transform.SetParent(viewModel, false);
                    assetFlash.transform.localPosition = mountedOnCharacter ? Vector3.forward * .55f : new Vector3(.30f, -.23f, 1.12f);
                    muzzleFlash = assetFlash.AddComponent<Light>();
                    muzzleFlash.type = LightType.Point;
                    muzzleFlash.color = new Color(1f, .46f, .12f);
                    muzzleFlash.intensity = 0f;
                    muzzleFlash.range = 3f;
                    muzzleFlash.shadows = LightShadows.None;
                    return;
                }
            }
            Material metal = MakeViewMaterial(new Color(.16f, .18f, .2f));
            Material edge = MakeViewMaterial(new Color(.32f, .34f, .36f));
            Material grip = MakeViewMaterial(new Color(.075f, .065f, .055f));
            Material leather = MakeViewMaterial(new Color(.12f, .09f, .065f));
            float barrelLength = weaponTier == 2 || weaponTier == 5 ? .72f : (weaponTier == 3 ? .55f : .34f);
            bool longWeapon = weaponTier == 2 || weaponTier == 3 || weaponTier == 5;
            float bodyLength = longWeapon ? .72f : .43f;
            Part("Receiver", PrimitiveType.Cube, new Vector3(0f, .035f, .08f), new Vector3(.19f, .18f, bodyLength), metal);
            Part("Steel Slide", PrimitiveType.Cube, new Vector3(0f, .105f, .09f), new Vector3(.205f, .095f, bodyLength * .78f), edge);
            Part("Barrel", PrimitiveType.Cylinder, new Vector3(0f, .04f, .12f + bodyLength * .53f), new Vector3(.041f, barrelLength, .041f), metal, Quaternion.Euler(90f, 0f, 0f));
            Part("Muzzle", PrimitiveType.Cylinder, new Vector3(0f, .04f, .19f + bodyLength * .53f + barrelLength * .45f), new Vector3(.06f, .055f, .06f), edge, Quaternion.Euler(90f, 0f, 0f));
            Part("Grip", PrimitiveType.Cube, new Vector3(0f, -.16f, -.08f), new Vector3(.145f, .31f, .17f), grip, Quaternion.Euler(0f, 0f, -9f));
            Part("Grip Heel", PrimitiveType.Cube, new Vector3(0f, -.315f, -.085f), new Vector3(.17f, .035f, .18f), edge);
            Part("Grip Panel", PrimitiveType.Cube, new Vector3(0f, -.16f, -.17f), new Vector3(.105f, .19f, .014f), leather, Quaternion.Euler(0f, 0f, -9f));
            Part("Trigger Guard Top", PrimitiveType.Cube, new Vector3(0f, -.075f, .07f), new Vector3(.13f, .025f, .12f), edge);
            Part("Trigger Guard Front", PrimitiveType.Cube, new Vector3(0f, -.13f, .13f), new Vector3(.13f, .11f, .025f), edge);
            Part("Trigger Guard Rear", PrimitiveType.Cube, new Vector3(0f, -.13f, .015f), new Vector3(.13f, .11f, .025f), edge);
            Part("Trigger", PrimitiveType.Cube, new Vector3(0f, -.105f, .06f), new Vector3(.018f, .07f, .02f), metal, Quaternion.Euler(0f, 0f, -12f));
            Part("Rear Sight", PrimitiveType.Cube, new Vector3(0f, .172f, -.12f), new Vector3(.07f, .035f, .035f), metal);
            Part("Front Sight", PrimitiveType.Cube, new Vector3(0f, .172f, bodyLength * .37f), new Vector3(.045f, .03f, .035f), metal);
            Part("Ejection Port", PrimitiveType.Cube, new Vector3(.106f, .105f, .12f), new Vector3(.009f, .035f, .14f), grip);
            Part("Gloved Hand", PrimitiveType.Capsule, new Vector3(0f, -.13f, -.015f), new Vector3(.18f, .15f, .2f), grip);
            Part("Forearm Glove", PrimitiveType.Capsule, new Vector3(.04f, -.31f, -.24f), new Vector3(.19f, .24f, .32f), grip, Quaternion.Euler(-12f, 0f, -12f));
            if (longWeapon)
            {
                Part("Foregrip", PrimitiveType.Cube, new Vector3(0f, -.07f, bodyLength * .27f), new Vector3(.15f, .13f, .2f), grip);
                Part("Top Rail", PrimitiveType.Cube, new Vector3(0f, .17f, .12f), new Vector3(.11f, .035f, bodyLength * .45f), metal);
            }
            GameObject flash = new GameObject("Muzzle Flash");
            flash.transform.SetParent(viewModel, false);
            flash.transform.localPosition = new Vector3(0f, .03f, .48f + barrelLength * .5f);
            muzzleFlash = flash.AddComponent<Light>();
            muzzleFlash.type = LightType.Point;
            muzzleFlash.color = new Color(1f, .46f, .12f);
            muzzleFlash.intensity = 0f;
            muzzleFlash.range = 3f;
            muzzleFlash.shadows = LightShadows.None;
        }

        private void Part(string partName, PrimitiveType shape, Vector3 position, Vector3 scale, Material material, Quaternion? rotation = null)
        {
            GameObject part = GameObject.CreatePrimitive(shape);
            part.name = partName;
            part.transform.SetParent(viewModel, false);
            part.transform.localPosition = position;
            part.transform.localRotation = rotation.HasValue ? rotation.Value : Quaternion.identity;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }

        private static Material MakeViewMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.color = color;
            return material;
        }

        private IEnumerator FlashMuzzle()
        {
            muzzleFlash.intensity = 4f;
            yield return new WaitForSeconds(.045f);
            if (muzzleFlash != null) muzzleFlash.intensity = 0f;
        }

        private void RefreshHud()
        {
            if (UIManager.Instance != null) UIManager.Instance.SetWeaponStatus(WeaponName, ammunition, reserve);
        }
    }
}




