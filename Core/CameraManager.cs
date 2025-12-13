using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Transform boatTarget;
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

    private Transform currentTarget;
    
    // ✅ NOVO: Flag para forçar posição instantânea
    private bool forceInstantPosition = false;

    private void Start()
    {
        currentTarget = playerTarget;
        Debug.Log("CameraManager iniciado. Seguindo: " + currentTarget.name);
    }

    private void LateUpdate()
    {
        if (currentTarget == null) return;
        
        Vector3 desiredPosition = currentTarget.position + offset;
        
        // ✅ CORREÇÃO: Posição instantânea quando necessário
        if (forceInstantPosition)
        {
            transform.position = desiredPosition;
            forceInstantPosition = false;
            Debug.Log($"📷 Câmera forçada para: {desiredPosition}");
        }
        else
        {
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
        }
    }

    public void SwitchToBoat()
    {
        if (boatTarget != null)
        {
            currentTarget = boatTarget;
            Debug.Log("Câmera mudou para o BARCO");
        }
    }

    public void SwitchToPlayer()
    {
        if (playerTarget != null)
        {
            currentTarget = playerTarget;
            Debug.Log("Câmera mudou para o PLAYER");
        }
    }
    
    // ✅ NOVO: Método para forçar posicionamento instantâneo
    public void ForceInstantPosition()
    {
        forceInstantPosition = true;
        Debug.Log("🎯 Câmera configurada para pular instantaneamente");
    }
    
    // ✅ NOVO: Teleporta câmera imediatamente (uso em loads)
    public void TeleportToTarget()
    {
        if (currentTarget != null)
        {
            transform.position = currentTarget.position + offset;
            Debug.Log($"⚡ Câmera TELEPORTADA para: {transform.position}");
        }
    }
}