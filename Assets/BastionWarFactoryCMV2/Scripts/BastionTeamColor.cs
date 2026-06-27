
using UnityEngine;

namespace BastionWarFactoryCMV2
{
    public class BastionTeamColor : MonoBehaviour
    {
        public Renderer[] teamColorRenderers;
        public Color teamColor = Color.white;
        private MaterialPropertyBlock block;

        private void Awake()
        {
            ApplyTeamColor(teamColor);
        }

        public void ApplyTeamColor(Color color)
        {
            teamColor = color;
            if (block == null) block = new MaterialPropertyBlock();
            if (teamColorRenderers == null) return;
            foreach (Renderer r in teamColorRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                r.SetPropertyBlock(block);
            }
        }
    }
}
