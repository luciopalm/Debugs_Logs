using UnityEngine;

public class LandingZone : MonoBehaviour
{
    [Header("Landing Zone Settings")]
    public string zoneName = "Porto Principal";
    public float activationRadius = 3f;
    
    [Header("Exit Positions")]
    public Transform[] exitPoints;
    
    private void OnDrawGizmos()
    {
        // Gizmo visual da zona
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
        
        // Gizmo dos pontos de saída
        if (exitPoints != null && exitPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            foreach (Transform exitPoint in exitPoints)
            {
                if (exitPoint != null)
                {
                    Gizmos.DrawWireCube(exitPoint.position, Vector3.one * 0.5f);
                    Gizmos.DrawLine(transform.position, exitPoint.position);
                }
            }
        }
    }
    
    public bool IsBoatInRange(Vector3 boatPosition)
    {
        float distance = Vector3.Distance(boatPosition, transform.position);
        return distance <= activationRadius;
    }
    
    public Vector3 GetRandomExitPosition()
    {
        if (exitPoints == null || exitPoints.Length == 0)
        {
            Debug.LogWarning($"⚠️ {zoneName} não tem pontos de saída definidos!");
            return transform.position;
        }
        
        Transform selectedExit = exitPoints[Random.Range(0, exitPoints.Length)];
        return selectedExit.position;
    }
}
