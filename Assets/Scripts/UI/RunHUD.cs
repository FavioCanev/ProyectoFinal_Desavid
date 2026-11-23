using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Muestra las vidas de reserva y las monedas de la partida.</summary>
    public class RunHUD : MonoBehaviour
    {
        [SerializeField] private Text lives;
        [SerializeField] private Text coins;

        private void OnEnable()
        {
            GameFlow.Run.Changed += Refresh;
            Refresh();
        }

        private void OnDisable() => GameFlow.Run.Changed -= Refresh;

        private void Refresh()
        {
            lives.text = "x" + GameFlow.Run.Lives;
            coins.text = GameFlow.Run.Coins.ToString();
        }
    }
}
