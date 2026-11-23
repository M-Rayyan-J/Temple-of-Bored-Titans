using UnityEngine;

namespace TempleOfBoredTitans.Core
{
    /// <summary>Soft, low-cost flame flicker for the temple's point lights.</summary>
    [RequireComponent(typeof(Light))]
    public sealed class TempleTorchFlicker : MonoBehaviour
    {
        [SerializeField] private float baseIntensity = 1.55f;
        [SerializeField] private float variation = .28f;
        [SerializeField] private float baseRange = 7f;
        private Light flame;
        private float seed;

        private void Awake()
        {
            flame = GetComponent<Light>();
            seed = Random.value * 100f;
            baseIntensity = flame.intensity;
            baseRange = flame.range;
        }

        private void Update()
        {
            if (flame == null) return;
            float wave = Mathf.PerlinNoise(seed, Time.time * 8f) * 2f - 1f;
            flame.intensity = Mathf.Max(.2f, baseIntensity + wave * variation);
            flame.range = baseRange + wave * .45f;
        }
    }
}
