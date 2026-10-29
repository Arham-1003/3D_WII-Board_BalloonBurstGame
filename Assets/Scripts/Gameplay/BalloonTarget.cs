using UnityEngine;

namespace BalloonBalance.Gameplay
{
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public sealed class BalloonTarget : MonoBehaviour
    {
        private BalloonGameManager owner;
        private float speed;
        private float missZ;
        private bool activeTarget;

        public int BalloonId { get; private set; }
        public float ApproachSpeed => speed;
        public Color BalloonColor { get; private set; }

        public void Launch(BalloonGameManager gameManager, int balloonId, Vector3 position, float approachSpeed, float missBoundaryZ, Color color)
        {
            owner = gameManager;
            BalloonId = balloonId;
            speed = approachSpeed;
            missZ = missBoundaryZ;
            BalloonColor = color;
            activeTarget = true;
            transform.position = position;
            transform.rotation = Quaternion.identity;
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
                if (renderer.material.HasProperty("_BaseColor")) renderer.material.SetColor("_BaseColor", color);
                if (renderer.material.HasProperty("_EmissionColor")) renderer.material.SetColor("_EmissionColor", color * 0.35f);
            }
            gameObject.SetActive(true);
        }

        public void ReturnToPool()
        {
            activeTarget = false;
            owner = null;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!activeTarget) return;
            transform.position += Vector3.back * (speed * Time.deltaTime);
            transform.Rotate(0f, 22f * Time.deltaTime, 0f, Space.World);
            if (transform.position.z <= missZ)
            {
                BalloonGameManager gameManager = owner;
                activeTarget = false;
                gameManager?.RegisterMiss(this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!activeTarget || other.GetComponent<PointerController>() == null) return;
            BalloonGameManager gameManager = owner;
            activeTarget = false;
            gameManager?.RegisterHit(this, transform.position);
        }
    }
}
