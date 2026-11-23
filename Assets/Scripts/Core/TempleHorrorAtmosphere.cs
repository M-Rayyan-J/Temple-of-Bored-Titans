using UnityEngine;
using UnityEngine.Rendering;

namespace TempleOfBoredTitans.Core
{
    /// <summary>Applies a dim, torch-lit burial-temple look to the loaded scene.</summary>
    public static class TempleHorrorAtmosphere
    {
        public static void Apply()
        {
            DisableOverlappingPrototypeGeometry();
            EnforceTempleBlockColliders();

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.115f, .09f, .062f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.012f, .008f, .006f);
            RenderSettings.fogDensity = .0042f;

            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light == null) continue;
                if (light.type == LightType.Directional)
                {
                    light.color = new Color(.38f, .4f, .5f);
                    light.intensity = Mathf.Min(light.intensity, .22f);
                }
                else if (light.name.Contains("Torch"))
                {
                    light.color = new Color(1f, .72f, .28f);
                    light.intensity = Mathf.Max(light.intensity, 3.7f);
                    light.range = Mathf.Max(light.range, 13f);
                    light.shadows = LightShadows.None;
                    if (light.GetComponent<TempleTorchFlicker>() == null)
                        light.gameObject.AddComponent<TempleTorchFlicker>();
                }
            }

            if (GameObject.Find("Runtime Crypt Glow") == null)
            {
                GameObject root = new GameObject("Runtime Crypt Glow");
                AddGlow(root.transform, new Vector3(-8f, 2.1f, 2f), new Color(.62f, .025f, .035f), 1.1f, 8f);
                AddGlow(root.transform, new Vector3(8f, 2.1f, 8f), new Color(.035f, .12f, .62f), .8f, 7f);
                AddGlow(root.transform, new Vector3(0f, 3.2f, 22f), new Color(.55f, .02f, .045f), 1.4f, 10f);
            }

        }

        private static void DisableOverlappingPrototypeGeometry()
        {
            string[] obsoleteRoots =
            {
                "Legacy Temple Floors", "Legacy Temple Walls", "Legacy Temple Ceilings", "Legacy Temple Props",
                "Expanded Temple Floors", "Expanded Temple Walls", "Expanded High Ceilings", "Expanded Temple Roof",
                "Temple Floors", "Temple Walls", "Temple Ceilings"
            };
            for (int i = 0; i < obsoleteRoots.Length; i++)
            {
                GameObject root = GameObject.Find(obsoleteRoots[i]);
                if (root != null && root.activeSelf) root.SetActive(false);
            }
        }

        private static void EnforceTempleBlockColliders()
        {
            GameObject layout = GameObject.Find("Storyboard Layout");
            if (layout == null) return;
            Transform[] pieces = layout.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < pieces.Length; i++)
            {
                Transform piece = pieces[i];
                string name = piece.name;
                if (!(name.Contains("Wall") || name.Contains("Ashlar") || name.Contains("Pier") ||
                      name.Contains("Buttress") || name.Contains("Jamb") || name.Contains("Floor") ||
                      name.Contains("Vault Stone") || name.Contains("Lintel") || name.Contains("Safety Boundary") ||
                      name.Contains("Foundation"))) continue;

                Collider[] colliders = piece.GetComponents<Collider>();
                if (colliders.Length == 0) colliders = new Collider[] { piece.gameObject.AddComponent<BoxCollider>() };
                for (int c = 0; c < colliders.Length; c++)
                {
                    if (colliders[c] == null) continue;
                    colliders[c].enabled = true;
                    colliders[c].isTrigger = false;
                }
            }
        }

        private static void AddGlow(Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            GameObject glow = new GameObject("Cursed Chamber Light");
            glow.transform.SetParent(parent, false);
            glow.transform.position = position;
            Light light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }
    }
}
