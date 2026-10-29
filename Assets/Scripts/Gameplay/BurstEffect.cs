using UnityEngine;

namespace BalloonBalance.Gameplay
{
    public sealed class BurstEffect : MonoBehaviour
    {
        private float age;
        private const float Lifetime = 0.28f;

        public void Play(Vector3 position, Color color)
        {
            transform.position = position;
            transform.localScale = Vector3.one * 0.12f;
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
                if (renderer.material.HasProperty("_BaseColor")) renderer.material.SetColor("_BaseColor", color);
                if (renderer.material.HasProperty("_EmissionColor")) renderer.material.SetColor("_EmissionColor", color * 3f);
            }
            age = 0f;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            age += Time.deltaTime;
            float progress = Mathf.Clamp01(age / Lifetime);
            transform.localScale = Vector3.one * Mathf.Lerp(0.12f, 0.85f, progress);
            if (age >= Lifetime) gameObject.SetActive(false);
        }
    }
}
