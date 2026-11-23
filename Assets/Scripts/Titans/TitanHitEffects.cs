using UnityEngine;

namespace TempleOfBoredTitans.Titans
{
    /// <summary>Short, stylized crimson particle spray for a Titan hit.</summary>
    public static class TitanHitEffects
    {
        private static Material bloodMaterial;

        public static void Spawn(Vector3 position, Vector3 normal)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (bloodMaterial == null && shader != null)
            {
                bloodMaterial = new Material(shader) { name = "Crimson Titan Hit Particles" };
                if (bloodMaterial.HasProperty("_BaseColor")) bloodMaterial.SetColor("_BaseColor", Color.white);
                else if (bloodMaterial.HasProperty("_Color")) bloodMaterial.SetColor("_Color", Color.white);
            }

            GameObject spray = new GameObject("Crimson Hit Spray");
            spray.transform.position = position + normal * .025f;
            spray.transform.rotation = Quaternion.LookRotation(normal.sqrMagnitude > .01f ? normal : Vector3.forward);
            ParticleSystem particles = spray.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.duration = .3f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.28f, .72f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(.035f, .095f);
            main.startColor = new Color(.58f, .012f, .028f, .95f);
            main.gravityModifier = .65f;
            main.maxParticles = 24;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 34f;
            shape.radius = .035f;
            var renderer = spray.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && bloodMaterial != null) renderer.material = bloodMaterial;
            particles.Emit(18);
            Object.Destroy(spray, 1.4f);
        }
    }
}
