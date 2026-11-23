using System;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Pistola de plasma del jugador (tecla Fin). Solo funciona si la partida ya la consiguió (nivel 2).
    /// Dispara hacia adelante; bajo el agua también en diagonal.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerPlasma : MonoBehaviour
    {
        [SerializeField] private PlayerProjectile shotPrefab;
        [Tooltip("Altura del cañón respecto a los pies.")]
        [SerializeField] private float muzzleHeight = 0.8f;

        private PlayerController controller;
        private float cooldownUntil;

        public PlasmaAmmo Ammo { get; private set; }
        public event Action Fired;

        private PlayerConfig Config => controller.Config;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            Ammo = new PlasmaAmmo(Config.plasmaCapacity, Config.plasmaReloadTime);
            controller.ShootPressed += OnShootPressed;
        }

        private void OnDestroy() => controller.ShootPressed -= OnShootPressed;

        private void Update() => Ammo.Tick(Time.deltaTime);   // con la pausa (timeScale 0) no avanza

        private void OnShootPressed() => TryShoot();

        public bool TryShoot()
        {
            if (!GameFlow.Run.HasPlasma || shotPrefab == null || Time.time < cooldownUntil || !Ammo.TryConsume()) return false;
            cooldownUntil = Time.time + Config.plasmaCooldown;
            Vector2 aim = controller.Aim;
            Vector2 origin = (Vector2)transform.position + Vector2.up * muzzleHeight + aim * 0.7f;
            var shot = Instantiate(shotPrefab, origin, Quaternion.identity);
            shot.Launch(aim * Config.plasmaSpeed, Config.plasmaDamage, Config.enemyMask, Config.groundMask);
            GameFeel.Play(Sfx.Shoot);
            Fired?.Invoke();
            return true;
        }
    }
}
