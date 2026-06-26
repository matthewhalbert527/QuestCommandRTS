using UnityEngine;

namespace QuestCommandRTS
{
    public sealed class ResourceNode : MonoBehaviour
    {
        public int Amount = 3500;
        public int MaxAmount = 3500;

        public bool IsDepleted => Amount <= 0;

        private Transform visualRoot;
        private Material healthyMaterial;
        private Material depletedMaterial;

        private void Awake()
        {
            SphereCollider collider = gameObject.GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<SphereCollider>();
            }

            collider.radius = 2.4f;
            collider.center = new Vector3(0f, 0.7f, 0f);
        }

        public void Initialize(int amount, Material healthy, Material depleted)
        {
            Amount = amount;
            MaxAmount = amount;
            healthyMaterial = healthy;
            depletedMaterial = depleted;
            BuildVisuals();
            RefreshVisuals();
        }

        public int Harvest(int requested)
        {
            if (requested <= 0 || Amount <= 0)
            {
                return 0;
            }

            int taken = Mathf.Min(requested, Amount);
            Amount -= taken;
            RefreshVisuals();
            return taken;
        }

        private void BuildVisuals()
        {
            if (visualRoot != null)
            {
                Destroy(visualRoot.gameObject);
            }

            GameObject root = new GameObject("Resource Crystals");
            root.transform.SetParent(transform, false);
            visualRoot = root.transform;

            for (int i = 0; i < 9; i++)
            {
                float angle = i * 40f * Mathf.Deg2Rad;
                float radius = 0.25f + (i % 3) * 0.55f;
                GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                crystal.name = "Crystal";
                crystal.transform.SetParent(visualRoot, false);
                crystal.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, 0.25f, Mathf.Sin(angle) * radius);
                crystal.transform.localRotation = Quaternion.Euler(0f, i * 23f, 0f);
                crystal.transform.localScale = new Vector3(0.32f + (i % 2) * 0.12f, 0.55f + (i % 4) * 0.18f, 0.32f);

                Collider collider = crystal.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }
            }
        }

        private void RefreshVisuals()
        {
            if (visualRoot == null)
            {
                return;
            }

            float fullness = MaxAmount <= 0 ? 0f : Mathf.Clamp01((float)Amount / MaxAmount);
            visualRoot.localScale = Vector3.one * Mathf.Lerp(0.25f, 1f, fullness);
            Material material = IsDepleted ? depletedMaterial : healthyMaterial;

            foreach (Renderer renderer in visualRoot.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material;
            }
        }
    }
}
