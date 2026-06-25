using UnityEngine;

namespace QuestCommandRTS
{
    public sealed class RtsTimedDestroy : MonoBehaviour
    {
        public float Lifetime = 1f;

        private void Update()
        {
            float deltaTime = RtsGame.HasInstance ? RtsGame.Instance.Clock.DeltaTime : Time.deltaTime;
            if (RtsGame.HasInstance && RtsGame.Instance.Clock.IsPaused)
            {
                return;
            }

            Lifetime -= deltaTime;
            if (Lifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    public sealed class FloatingText : MonoBehaviour
    {
        public float Lifetime = 1.3f;
        public Vector3 Velocity = new Vector3(0f, 1.1f, 0f);

        private TextMesh textMesh;
        private Color startColor;

        private void Awake()
        {
            textMesh = GetComponent<TextMesh>();
            startColor = textMesh != null ? textMesh.color : Color.white;
        }

        private void Update()
        {
            float deltaTime = RtsGame.HasInstance ? RtsGame.Instance.Clock.DeltaTime : Time.deltaTime;
            if (RtsGame.HasInstance && RtsGame.Instance.Clock.IsPaused)
            {
                return;
            }

            Lifetime -= deltaTime;
            transform.position += Velocity * deltaTime;

            if (Camera.main != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position, Vector3.up);
            }

            if (textMesh != null)
            {
                Color color = startColor;
                color.a = Mathf.Clamp01(Lifetime / 1.3f);
                textMesh.color = color;
            }

            if (Lifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
