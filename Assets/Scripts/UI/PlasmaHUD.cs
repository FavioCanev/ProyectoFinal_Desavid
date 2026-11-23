using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Cargas de plasma bajo el dash: una pastilla por disparo y una barra durante la recarga. Oculto hasta conseguir la pistola.</summary>
    public class PlasmaHUD : MonoBehaviour
    {
        [SerializeField] private PlayerPlasma plasma;
        [SerializeField] private GameObject root;
        [SerializeField] private Image[] pips;
        [SerializeField] private Image reload;
        [SerializeField] private Color fullColor = new Color(0.4f, 1f, 1f);
        [SerializeField] private Color emptyColor = new Color(0.2f, 0.25f, 0.3f, 0.6f);

        private void Update()
        {
            bool visible = GameFlow.Run.HasPlasma && plasma != null;
            if (root.activeSelf != visible) root.SetActive(visible);
            if (!visible) return;
            PlasmaAmmo ammo = plasma.Ammo;
            for (int i = 0; i < pips.Length; i++) pips[i].color = i < ammo.Ammo ? fullColor : emptyColor;
            reload.fillAmount = ammo.Reloading ? ammo.Progress : 0f;
        }
    }
}
