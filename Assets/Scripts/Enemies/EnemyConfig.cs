using System;
using UnityEngine;

namespace Game
{
    /// <summary>Datos de un tipo de enemigo: movimiento, reacción a golpes y sus ataques.</summary>
    [CreateAssetMenu(menuName = "Game/Enemy Config", fileName = "EnemyConfig")]
    public class EnemyConfig : ScriptableObject
    {
        [Header("Movimiento")]
        [Min(0f)] public float patrolSpeed = 1.8f;
        [Min(0f)] public float chaseSpeed = 3.6f;
        [Tooltip("Distancia horizontal a la que detecta al jugador.")]
        [Min(0f)] public float detectRange = 7f;
        [Tooltip("Diferencia de altura máxima para detectar al jugador.")]
        [Min(0f)] public float detectHeight = 2.5f;
        [Tooltip("0 = se acerca hasta el cuerpo a cuerpo. Mayor que 0 = intenta mantener esta distancia (enemigo a distancia).")]
        [Min(0f)] public float preferredDistance = 0f;
        [Min(0.05f)] public float bodyHalfWidth = 0.45f;

        [Header("Golpes recibidos")]
        public Vector2 hurtKnockback = new Vector2(5f, 3f);
        [Min(0f)] public float hurtTime = 0.3f;
        [Tooltip("Si está activo, los golpes no lo interrumpen ni lo empujan (jefes).")]
        public bool superArmor;
        [Min(0f)] public float corpseTime = 1.2f;

        [Header("Ataques")]
        [Tooltip("Pausa mínima entre el final de un ataque y el inicio del siguiente.")]
        [Min(0f)] public float attackPause = 0.4f;
        [Tooltip("Color del aviso antes de atacar.")]
        public Color windupColor = new Color(1f, 0.35f, 0.3f);
        [Tooltip("Furia: con la mitad de la vida o menos, los avisos y descansos duran esta fracción (1 = sin furia).")]
        [Range(0.3f, 1f)] public float enragedTimeScale = 1f;
        public SwipeAttack swipe = new SwipeAttack();
        public ChargeAttack charge = new ChargeAttack();
        public SpitAttack spit = new SpitAttack();
        public LeapAttack leap = new LeapAttack();
    }

    /// <summary>Lo común a todos los ataques: si está activo, a qué distancia lo usa y sus tiempos.</summary>
    [Serializable]
    public class AttackTimings
    {
        public bool enabled;
        [Min(0f)] public float minRange;
        [Min(0f)] public float maxRange = 2f;
        [Tooltip("Aviso antes de atacar (brilla en rojo).")]
        [Min(0f)] public float windup = 0.4f;
        [Tooltip("Tiempo quieto y vulnerable después de atacar.")]
        [Min(0f)] public float recover = 0.5f;
        [Tooltip("Segundos antes de poder repetir este ataque.")]
        [Min(0f)] public float cooldown = 1.5f;
    }

    /// <summary>Zarpazo: golpe cuerpo a cuerpo en una zona delante del enemigo.</summary>
    [Serializable]
    public class SwipeAttack : AttackTimings
    {
        [Min(0f)] public float activeTime = 0.2f;
        public Vector2 hitOffset = new Vector2(0.9f, 0.8f);
        public Vector2 hitSize = new Vector2(1.4f, 1.2f);
    }

    /// <summary>Embestida: carrera rápida en línea recta; se detiene ante muros y bordes.</summary>
    [Serializable]
    public class ChargeAttack : AttackTimings
    {
        [Min(0f)] public float speed = 9f;
        [Min(0f)] public float duration = 0.65f;
    }

    /// <summary>Escupitajo: proyectiles de ácido en arco hacia el jugador.</summary>
    [Serializable]
    public class SpitAttack : AttackTimings
    {
        [Min(1)] public int count = 1;
        [Tooltip("Tiempo de vuelo del proyectil. Con varios, cada uno vuela un poco más (abanico).")]
        [Min(0.1f)] public float flightTime = 0.8f;
        [Min(0f)] public float spreadTime = 0.25f;
        public Vector2 mouthOffset = new Vector2(0.8f, 1.2f);
    }

    /// <summary>Salto hacia el jugador; al caer lanza dos ondas de choque por el suelo.</summary>
    [Serializable]
    public class LeapAttack : AttackTimings
    {
        [Min(0.1f)] public float flightTime = 0.75f;
        [Min(0f)] public float shockwaveSpeed = 7f;
    }
}
