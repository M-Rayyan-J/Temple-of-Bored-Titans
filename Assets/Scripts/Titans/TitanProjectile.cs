using UnityEngine;
using TempleOfBoredTitans.Player;

namespace TempleOfBoredTitans.Titans
{
    /// <summary>Fast visible projectile used by ranged Titan attacks.</summary>
    public sealed class TitanProjectile : MonoBehaviour
    {
        public enum ShotStyle { Fireball, Bone, PlagueBubble, SoulLightning, IceBolt }
        private Transform target;
        private Transform owner;
        private int damage;
        private float speed;
        private float life = 6f;

        public static void Launch(Vector3 origin, Transform target, Transform owner, int damage, ShotStyle style, float speed)
        {
            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = style + " Titan Projectile";
            orb.transform.position = origin;
            float size = style == ShotStyle.Bone ? .52f : style == ShotStyle.IceBolt ? .22f : .38f;
            orb.transform.localScale = Vector3.one * size;
            Collider collider = orb.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            Renderer renderer = orb.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader);
            Color color = ColorFor(style);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.color = color;
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2.6f);
            }
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            AddStyleVisuals(orb.transform, style, material, color);
            TrailRenderer trail = orb.AddComponent<TrailRenderer>();
            trail.time = style == ShotStyle.SoulLightning ? .22f : .3f;
            trail.startWidth = style == ShotStyle.IceBolt ? .11f : .2f;
            trail.endWidth = 0f;
            trail.material = material;
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            TitanProjectile projectile = orb.AddComponent<TitanProjectile>();
            projectile.target = target;
            projectile.owner = owner;
            projectile.damage = damage;
            projectile.speed = speed;
        }

        private static Color ColorFor(ShotStyle style)
        {
            switch (style)
            {
                case ShotStyle.Fireball: return new Color(1f, .045f, .02f);
                case ShotStyle.Bone: return new Color(.92f, .91f, .78f);
                case ShotStyle.PlagueBubble: return new Color(.83f, 1f, .08f);
                case ShotStyle.SoulLightning: return new Color(.72f, .18f, 1f);
                default: return new Color(.035f, .12f, .48f);
            }
        }

        private static void AddStyleVisuals(Transform shot, ShotStyle style, Material material, Color color)
        {
            if (style == ShotStyle.Bone)
            {
                // A long ivory shard with knobby ends reads clearly as a thrown bone.
                shot.localScale = new Vector3(.16f, .16f, .72f);
                for (int side = -1; side <= 1; side += 2)
                {
                    GameObject knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    knob.name = "Bone End";
                    knob.transform.SetParent(shot, false);
                    knob.transform.localPosition = new Vector3(side * .12f, 0f, 0f);
                    knob.transform.localScale = new Vector3(.42f, .42f, .3f);
                    Collider knobCollider = knob.GetComponent<Collider>();
                    if (knobCollider != null) Object.Destroy(knobCollider);
                    knob.GetComponent<Renderer>().sharedMaterial = material;
                }
            }
            else if (style == ShotStyle.SoulLightning)
            {
                // Several short jagged, emissive lines form a visible purple lightning bolt.
                for (int strand = 0; strand < 3; strand++)
                {
                    GameObject bolt = new GameObject("Lightning Arc");
                    bolt.transform.SetParent(shot, false);
                    LineRenderer line = bolt.AddComponent<LineRenderer>();
                    line.useWorldSpace = false;
                    line.positionCount = 5;
                    line.startWidth = .045f;
                    line.endWidth = .018f;
                    line.material = material;
                    line.startColor = Color.white;
                    line.endColor = color;
                    float offset = (strand - 1) * .12f;
                    line.SetPosition(0, new Vector3(offset, 0f, -.38f));
                    line.SetPosition(1, new Vector3(offset + .11f, .08f, -.18f));
                    line.SetPosition(2, new Vector3(offset - .08f, -.06f, .02f));
                    line.SetPosition(3, new Vector3(offset + .09f, .07f, .2f));
                    line.SetPosition(4, new Vector3(offset, 0f, .38f));
                    line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            else
            {
                // Plague bubbles are larger and wobbly; the others are compact fire/energy shots.
                shot.localScale = style == ShotStyle.PlagueBubble ? Vector3.one * .48f :
                    style == ShotStyle.Fireball ? Vector3.one * .4f : Vector3.one * .22f;
                if (style == ShotStyle.PlagueBubble) shot.localScale = new Vector3(.48f, .4f, .48f);
            }
        }

        private void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f || target == null) { Destroy(gameObject); return; }
            Vector3 destination = target.position + Vector3.up * 1f;
            Vector3 direction = destination - transform.position;
            float step = speed * Time.deltaTime;
            if (direction.sqrMagnitude > .001f)
            {
                direction.Normalize();
                if (Physics.SphereCast(transform.position, .17f, direction, out RaycastHit hit, step,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (owner != null && hit.transform.IsChildOf(owner))
                    {
                        transform.position += direction * step;
                        return;
                    }
                    PlayerHealth health = hit.collider.GetComponentInParent<PlayerHealth>();
                    if (health != null) health.TakeDamage(damage, "the Titan projectile", owner);
                    Destroy(gameObject);
                    return;
                }
                transform.position += direction * step;
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void OnDestroy()
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null) Destroy(renderer.sharedMaterial);
        }
    }
}
