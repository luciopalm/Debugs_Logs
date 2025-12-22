using UnityEngine;
using System.Collections;

public class NavalProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public int damage = 1;
    public float lifetime = 5f;
    public LayerMask collisionLayers;
    
    // Tipo de dano
    public enum DamageType { Player, Boat }
    public DamageType damageType = DamageType.Player;
    
    [Header("Physics Settings")]
    public bool applyPhysicsForce = false;
    public float physicsForceMultiplier = 0f;

    private Rigidbody2D rb;
    private Collider2D col;
    private bool isActive = true;
    private float spawnTime;
    private Vector3 startPosition;
    private float maxTravelDistance;
    private Camera mainCamera;
    private float rotationOffset = -90f; // -90° para sprite que aponta para cima

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        mainCamera = Camera.main;
        
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.mass = 0.01f;
        }
        
        if (col != null)
        {
            col.isTrigger = true;
        }
    }
    
    private void Start()
    {
        spawnTime = Time.time;
        startPosition = transform.position;
    }
    
    private void Update()
    {
        if (!isActive) return;
        
        CheckMaxDistance();
        
        if (Time.time - spawnTime > lifetime)
        {
            DestroyProjectile();
        }
    }
    
    public void Launch(Vector2 velocity, float force, float minForce, float maxForce)
    {
        if (rb == null) return;
        
        maxTravelDistance = CalculateMaxDistance(force, minForce, maxForce);
        rb.linearVelocity = velocity;
        
        RotateTowardsVelocity();
    }
    
    private float CalculateMaxDistance(float currentForce, float minForce, float maxForce)
    {
        if (mainCamera == null) 
        {
            float progress = Mathf.InverseLerp(minForce, maxForce, currentForce);
            return Mathf.Lerp(5f, 20f, progress);
        }
        
        float maxDistanceToEdge = GetMaxDistanceToScreenEdge();
        float forceProgress = Mathf.InverseLerp(minForce, maxForce, currentForce);
        float minDistance = 3f;
        float calculatedDistance = Mathf.Lerp(minDistance, maxDistanceToEdge, forceProgress);
        
        return calculatedDistance;
    }

    private float GetMaxDistanceToScreenEdge()
    {
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(transform.position);
        Vector3[] directions = { Vector3.right, Vector3.left, Vector3.up, Vector3.down };
        
        float maxDistance = 0f;
        foreach (Vector3 dir in directions)
        {
            float distance = GetDistanceToScreenEdge(viewportPos, dir);
            maxDistance = Mathf.Max(maxDistance, distance);
        }
        
        return maxDistance * mainCamera.orthographicSize * 2f * 1.2f;
    }
    
    private float GetDistanceToScreenEdge(Vector3 viewportPos, Vector3 direction)
    {
        float distanceToRight = (1f - viewportPos.x) / Mathf.Max(Mathf.Abs(direction.x), 0.001f);
        float distanceToLeft = viewportPos.x / Mathf.Max(Mathf.Abs(direction.x), 0.001f);
        float distanceToTop = (1f - viewportPos.y) / Mathf.Max(Mathf.Abs(direction.y), 0.001f);
        float distanceToBottom = viewportPos.y / Mathf.Max(Mathf.Abs(direction.y), 0.001f);
        
        float minDistance = float.MaxValue;
        
        if (direction.x > 0) minDistance = Mathf.Min(minDistance, distanceToRight);
        if (direction.x < 0) minDistance = Mathf.Min(minDistance, distanceToLeft);
        if (direction.y > 0) minDistance = Mathf.Min(minDistance, distanceToTop);
        if (direction.y < 0) minDistance = Mathf.Min(minDistance, distanceToBottom);
        
        return minDistance;
    }
    
    private void CheckMaxDistance()
    {
        float distanceTraveled = Vector3.Distance(startPosition, transform.position);
        if (distanceTraveled >= maxTravelDistance)
        {
            DestroyProjectile();
        }
    }
    
    private void RotateTowardsVelocity()
    {
        if (rb.linearVelocity != Vector2.zero)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            float correctedAngle = angle - 90f; // -90° para sprite que aponta para cima
            
            transform.rotation = Quaternion.AngleAxis(correctedAngle, Vector3.forward);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;

        // Ignora colisão com player e barco aliado (para projéteis do player)
        if (damageType == DamageType.Player)
        {
            if (other.CompareTag("Player") || 
                other.gameObject.layer == LayerMask.NameToLayer("PlayerBoat") ||
                other.gameObject.layer == LayerMask.NameToLayer("Boat"))
            {
                return;
            }
        }
        // Ignora colisão com inimigos navais (para projéteis do inimigo)
        else if (damageType == DamageType.Boat)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("NavalEnemies"))
            {
                return;
            }
        }

        // NOVO: Verifica barco inimigo e aplica dano
        if (other.gameObject.layer == LayerMask.NameToLayer("NavalEnemies"))
        {
            EnemyBoatController enemyBoat = other.GetComponent<EnemyBoatController>();
            if (enemyBoat != null && damageType == DamageType.Player)
            {
                enemyBoat.TakeDamage(damage);
                Debug.Log($"🎯 Acertou barco INIMIGO! Dano: {damage}");
                DestroyProjectile();
                return;
            }
        }

        // Verifica se acertou um barco (player)
        if (other.gameObject.layer == LayerMask.NameToLayer("Boat"))
        {
            BoatController boat = other.GetComponent<BoatController>();
            if (boat != null && damageType == DamageType.Boat)
            {
                boat.TakeBoatDamage(damage);
                Debug.Log($"💥 Acertou BARCO do player! Dano: {damage}");
                DestroyProjectile();
                return;
            }
        }

        // Sistema de dano original (para inimigos terrestres)
        if (((1 << other.gameObject.layer) & collisionLayers) != 0)
        {
            HitEnemy(other.gameObject);
            return;
        }

        // Colisão com qualquer outra coisa (água, obstáculos)
        DestroyProjectile();
    }
    
    private void HitEnemy(GameObject enemy)
    {
        EnemyController enemyController = enemy.GetComponent<EnemyController>();
        if (enemyController != null && damageType == DamageType.Player)
        {
            enemyController.TakeDamage(damage);
            Debug.Log($"🎯 Acertou inimigo terrestre: {enemy.name}");
        }
        DestroyProjectile();
    }
    
    private void DestroyProjectile()
    {
        if (!isActive) return;
        
        isActive = false;
        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.Sleep();
        }
        
        Destroy(gameObject);
    }
    
    private void OnDrawGizmos()
    {
        if (!isActive || rb == null) return;
        
        Gizmos.color = damageType == DamageType.Player ? Color.blue : Color.red;
        Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 1f);
        
        if (Application.isPlaying && maxTravelDistance > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(startPosition, maxTravelDistance);
        }
    }
}
