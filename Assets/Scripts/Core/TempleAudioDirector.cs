using UnityEngine;

namespace TempleOfBoredTitans.Core
{
    /// <summary>Low-cost procedural ambience and combat effects for the temple.</summary>
    [DefaultExecutionOrder(-9000)]
    public sealed class TempleAudioDirector : MonoBehaviour
    {
        public static TempleAudioDirector Instance { get; private set; }
        [SerializeField, Range(0f, 1f)] private float ambienceVolume = .22f;
        [SerializeField, Range(0f, 1f)] private float effectsVolume = .8f;
        private AudioSource ambienceSource;
        private AudioSource effectsSource;
        private AudioClip footstepClip;
        private AudioClip notificationClip;
        private AudioClip threatClip;
        private AudioClip pistolClip;
        private AudioClip heavyClip;
        private AudioClip reloadClip;
        private AudioClip impactClip;
        private AudioClip[] titanCues;
        private AudioClip[] titanPlayerHitCues;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.playOnAwake = false;
            ambienceSource.loop = true;
            ambienceSource.spatialBlend = 0f;
            ambienceSource.ignoreListenerPause = true;
            ambienceSource.volume = Mathf.Max(ambienceVolume, .35f);
            ambienceSource.clip = MakeAmbience();
            ambienceSource.Play();

            effectsSource = gameObject.AddComponent<AudioSource>();
            effectsSource.playOnAwake = false;
            effectsSource.spatialBlend = 0f;
            effectsSource.ignoreListenerPause = true;
            effectsSource.volume = Mathf.Max(effectsVolume, .75f);
            footstepClip = MakeFootstep();
            notificationClip = MakeNotification();
            threatClip = MakeThreat();
            pistolClip = MakeGunshot(false);
            heavyClip = MakeGunshot(true);
            reloadClip = MakeReload();
            impactClip = MakeImpact();
            titanCues = new AudioClip[5];
            titanPlayerHitCues = new AudioClip[5];
            for (int i = 0; i < titanCues.Length; i++)
            {
                titanCues[i] = MakeTitanCue(i);
                titanPlayerHitCues[i] = MakeTitanPlayerHit(i);
            }
        }

        private void Update()
        {
            if (ambienceSource != null && !ambienceSource.isPlaying && !AudioListener.pause) ambienceSource.Play();
        }

        public void PlayFootstep() { if (effectsSource != null) effectsSource.PlayOneShot(footstepClip, .4f); }
        public void PlayNotification() { if (effectsSource != null) effectsSource.PlayOneShot(notificationClip, .35f); }
        public void PlayThreat() { if (effectsSource != null) effectsSource.PlayOneShot(threatClip, .75f); }
        public void PlayGunshot(bool heavy) { if (effectsSource != null) effectsSource.PlayOneShot(heavy ? heavyClip : pistolClip, 1f); }
        public void PlayReload() { if (effectsSource != null) effectsSource.PlayOneShot(reloadClip, .65f); }
        public void PlayTitanHit() { if (effectsSource != null) effectsSource.PlayOneShot(impactClip, .65f); }
        public void PlayTitanAttack(int stage)
        {
            if (effectsSource != null && titanCues != null && titanCues.Length > 0)
                effectsSource.PlayOneShot(titanCues[Mathf.Clamp(stage, 0, titanCues.Length - 1)], .9f);
        }
        public void PlayTitanPlayerHit(int stage)
        {
            if (effectsSource == null || titanPlayerHitCues == null || titanPlayerHitCues.Length == 0) return;
            effectsSource.PlayOneShot(titanPlayerHitCues[Mathf.Clamp(stage, 0, titanPlayerHitCues.Length - 1)], 1f);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private static AudioClip MakeAmbience()
        {
            const int rate = 22050, seconds = 12;
            float[] samples = new float[rate * seconds];
            System.Random random = new System.Random(177);
            float filteredNoise = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                filteredNoise += ((float)random.NextDouble() * 2f - 1f - filteredNoise) * .018f;
                float swell = .75f + .25f * Mathf.Sin(t * .55f);
                float drone = Mathf.Sin(t * Mathf.PI * 2f * 42f) * .32f + Mathf.Sin(t * Mathf.PI * 2f * 59f) * .22f + Mathf.Sin(t * Mathf.PI * 2f * 83f) * .12f;
                samples[i] = (drone * swell + filteredNoise * .9f) * .34f;
            }
            return CreateClip("Cursed Temple Wind", samples, rate);
        }

        private static AudioClip MakeFootstep()
        {
            const int rate = 22050;
            float[] samples = new float[(int)(rate * .18f)];
            System.Random random = new System.Random(19);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                float envelope = Mathf.Exp(-t * 28f);
                float grit = ((float)random.NextDouble() * 2f - 1f) * .22f;
                samples[i] = (Mathf.Sin(2f * Mathf.PI * (92f - 35f * t) * t) * .65f + grit) * envelope * .48f;
            }
            return CreateClip("Stone Footstep", samples, rate);
        }

        private static AudioClip MakeNotification()
        {
            const int rate = 22050;
            float[] samples = new float[(int)(rate * .22f)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                float envelope = Mathf.Exp(-t * 18f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * (420f - 180f * t) * t) * envelope * .18f;
            }
            return CreateClip("Distant Stone Chime", samples, rate);
        }

        private static AudioClip MakeThreat()
        {
            const int rate = 22050;
            float[] samples = new float[(int)(rate * .85f)];
            System.Random random = new System.Random(443);
            float lowNoise = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                float envelope = Mathf.Sin(Mathf.PI * t / .85f);
                lowNoise += ((float)random.NextDouble() * 2f - 1f - lowNoise) * .05f;
                float pitch = Mathf.Lerp(88f, 35f, t / .85f);
                samples[i] = (Mathf.Sin(2f * Mathf.PI * pitch * t) * .55f + lowNoise * .9f) * envelope * .4f;
            }
            return CreateClip("Titan Roar", samples, rate);
        }

        private static AudioClip MakeGunshot(bool heavy)
        {
            const int rate = 22050;
            int length = (int)(rate * (heavy ? .32f : .19f));
            float[] samples = new float[length];
            System.Random random = new System.Random(heavy ? 725 : 329);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                float noise = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * (heavy ? 15f : 25f));
                float low = Mathf.Sin(2f * Mathf.PI * (heavy ? 78f : 110f) * t) * Mathf.Exp(-t * (heavy ? 18f : 26f));
                samples[i] = Mathf.Clamp((noise * .42f + low * .58f) * (heavy ? .9f : .72f), -.95f, .95f);
            }
            return CreateClip(heavy ? "Shotgun Blast" : "Pistol Shot", samples, rate);
        }

        private static AudioClip MakeReload()
        {
            const int rate = 22050;
            float[] samples = new float[(int)(rate * .24f)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                float click = (Mathf.Sin(2f * Mathf.PI * 950f * t) + Mathf.Sin(2f * Mathf.PI * 1420f * t) * .4f) * Mathf.Exp(-t * 24f);
                samples[i] = click * .12f;
            }
            return CreateClip("Weapon Reload", samples, rate);
        }

        private static AudioClip MakeImpact()
        {
            const int rate = 22050;
            float[] samples = new float[(int)(rate * .16f)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                samples[i] = Mathf.Sin(2f * Mathf.PI * (180f - 90f * t) * t) * Mathf.Exp(-t * 22f) * .36f;
            }
            return CreateClip("Stone Impact", samples, rate);
        }

        private static AudioClip MakeTitanCue(int stage)
        {
            const int rate = 22050;
            float duration = stage == 1 || stage == 3 ? 1.25f : .95f;
            float[] samples = new float[Mathf.CeilToInt(rate * duration)];
            System.Random random = new System.Random(612 + stage * 37);
            float filteredNoise = 0f;
            float[] starts = { 70f, 190f, 95f, 420f, 54f };
            float[] ends = { 38f, 520f, 62f, 115f, 29f };
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                float phase = t / duration;
                float envelope = Mathf.Pow(Mathf.Sin(Mathf.PI * phase), stage == 2 ? .55f : .8f);
                filteredNoise += ((float)random.NextDouble() * 2f - 1f - filteredNoise) * (stage == 2 ? .11f : .032f);
                float frequency = Mathf.Lerp(starts[stage], ends[stage], phase);
                float growl = Mathf.Sin(2f * Mathf.PI * frequency * t) * .6f + Mathf.Sin(2f * Mathf.PI * frequency * 1.47f * t) * .22f;
                float texture = stage == 1 || stage == 3 ? filteredNoise * .75f : filteredNoise * .34f;
                float pulse = stage == 2 ? .55f + .45f * Mathf.Sin(2f * Mathf.PI * 13f * t) : 1f;
                samples[i] = Mathf.Clamp((growl + texture) * envelope * pulse * .62f, -.9f, .9f);
            }
            string[] names = { "Ashen Dragon Fire Roar", "Wailing Skeleton Cry", "Plague Brute Wet Growl", "Winged Reaper Shriek", "Stone Colossus Grinding Roar" };
            return CreateClip(names[stage], samples, rate);
        }

        private static AudioClip MakeTitanPlayerHit(int stage)
        {
            const int rate = 22050;
            float duration = .34f;
            float[] samples = new float[(int)(rate * duration)];
            System.Random random = new System.Random(1907 + stage * 53);
            float[] pitches = { 58f, 82f, 46f, 125f, 36f };
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                float envelope = Mathf.Exp(-t * 13f) * Mathf.Clamp01(t * 90f);
                float noise = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 22f);
                float body = Mathf.Sin(2f * Mathf.PI * (pitches[stage] - t * 45f) * t);
                samples[i] = Mathf.Clamp((body * .72f + noise * .5f) * envelope * .72f, -.95f, .95f);
            }
            string[] names = { "Dragon Claw Impact", "Skeleton Strike Impact", "Plague Brute Slam Impact", "Reaper Pounce Impact", "Colossus Fist Impact" };
            return CreateClip(names[stage], samples, rate);
        }

        private static AudioClip CreateClip(string name, float[] samples, int rate)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
