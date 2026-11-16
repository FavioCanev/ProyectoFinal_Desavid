using UnityEngine;

namespace Game
{
    /// <summary>
    /// Las animaciones importadas del proyecto anterior llaman a estos eventos.
    /// Aquí no hacen nada (el estado lo lleva el código); existen para que Unity no muestre errores.
    /// </summary>
    public class AnimationEventSink : MonoBehaviour
    {
        public void StopAttack() { }
        public void StopDamage() { }
    }
}
