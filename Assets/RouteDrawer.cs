using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(LineRenderer))]
public class RouteDrawer : MonoBehaviour
{
    private NavMeshAgent agent;
    private LineRenderer line;
    private Transform currentTarget;
    private NavMeshPath path;
    private bool isOffPath = false;

    public System.Action<string> OnDestinationReached;

    // Getters para la interfaz
    public float RemainingDistance { get; private set; } = 0f;
    public float TotalDistance { get; private set; } = 0f;
    public Transform CurrentTarget => currentTarget;
    public string DestinationName => currentTarget != null ? currentTarget.name : "Sin destino";

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        line = GetComponent<LineRenderer>();
        path = new NavMeshPath();

        // CONFIGURACIÓN PARA CONTROL MANUAL
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.acceleration = 0;
        agent.speed = 0;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

        // Configuración visual básica por defecto si no está configurado
        line.useWorldSpace = true;
        line.positionCount = 0;
        
        if (line.startWidth == 0) line.startWidth = 0.3f;
        if (line.endWidth == 0) line.endWidth = 0.3f;
        
        // Asignar un material por defecto si no tiene uno
        if (line.sharedMaterial == null)
        {
            line.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            line.startColor = Color.cyan;
            line.endColor = Color.blue;
        }

        // Configuración visual extra para visibilidad
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
    }

    void Update()
    {
        // Solo intentamos sincronizar si el agente está habilitado y en el NavMesh
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.nextPosition = transform.position;
        }

        DrawPath();
    }

    private bool hasLoggedPartialPath = false;

    public void SetDestination(Transform target)
    {
        if (target == null)
        {
            Debug.Log("[RouteDrawer] Destino cancelado.");
            ClearPath();
            return;
        }


        currentTarget = target;
        hasLoggedPartialPath = false; // Reiniciamos el log para el nuevo destino
        Debug.Log($"[RouteDrawer] Nuevo destino: {target.name} en {target.position}");
        
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.SetDestination(target.position);
            
            // CÁLCULO CIENTÍFICO DE L_sug (Longitud Sugerida / Ruta Óptima)
            // Usamos la distancia del camino real en el NavMesh, no línea recta.
            NavMeshPath pathCalc = new NavMeshPath();
            NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, pathCalc);
            
            float pathDist = 0f;
            if (pathCalc.status == NavMeshPathStatus.PathComplete || pathCalc.status == NavMeshPathStatus.PathPartial)
            {
                if (pathCalc.corners.Length >= 2)
                {
                    for (int i = 0; i < pathCalc.corners.Length - 1; i++)
                    {
                        pathDist += Vector3.Distance(pathCalc.corners[i], pathCalc.corners[i + 1]);
                    }
                }
                else
                {
                     pathDist = Vector3.Distance(transform.position, target.position);
                }
            }
            else
            {
                pathDist = Vector3.Distance(transform.position, target.position); // Fallback
            }

            TotalDistance = pathDist;
            
            // Iniciar seguimiento de datos para la tesis (con nombre destino y distancia óptima L_sug)
            if (DataTracker.Instance != null) 
                DataTracker.Instance.StartTracking(target.name, TotalDistance);
            isOffPath = false;
        }
    }

    void DrawPath()
    {
        if (currentTarget == null)
        {
            line.positionCount = 0;
            return;
        }

        // 1. Determinar inicio (Jugador) - Radio muy pequeño para evitar saltos de piso
        Vector3 startPos = transform.position;
        NavMeshHit startHit;
        if (NavMesh.SamplePosition(transform.position, out startHit, 0.5f, NavMesh.AllAreas))
            startPos = startHit.position;
        else if (NavMesh.SamplePosition(transform.position, out startHit, 5.0f, NavMesh.AllAreas))
            startPos = startHit.position;

        // 2. Determinar fin (Objetivo)
        Vector3 targetPos = currentTarget.position;
        NavMeshHit targetHit;
        if (NavMesh.SamplePosition(targetPos, out targetHit, 0.5f, NavMesh.AllAreas))
            targetPos = targetHit.position;
        else if (NavMesh.SamplePosition(targetPos, out targetHit, 10.0f, NavMesh.AllAreas))
            targetPos = targetHit.position;

        // 3. Cálculo de la ruta
        if (NavMesh.CalculatePath(startPos, targetPos, NavMesh.AllAreas, path))
        {
            Vector3[] corners = path.corners;

            // 4. Limpieza por cercanía (Usamos distancia física real al objetivo)
            Vector3 diff = transform.position - targetPos;
            diff.y = 0; 
            float distXZ = diff.magnitude;

            if (distXZ < 1.5f)
            {
                Debug.Log($"[RouteDrawer] Destino {currentTarget.name} alcanzado.");
                if (DataTracker.Instance != null) DataTracker.Instance.StopTracking(true);
                
                string targetName = currentTarget.name;
                RemainingDistance = 0f;
                ClearPath();
                
                OnDestinationReached?.Invoke(targetName);
                return;
            }

            // Detección de errores de desvío (Si el jugador se aleja > 4 metros del punto más cercano de la ruta)
            if (DataTracker.Instance != null && DataTracker.Instance.isTracking)
            {
                float minPathDist = float.MaxValue;
                for (int i = 0; i < corners.Length; i++)
                {
                    float d = Vector3.Distance(transform.position, corners[i]);
                    if (d < minPathDist) minPathDist = d;
                }

                if (minPathDist > 4.0f && !isOffPath)
                {
                    // Solo registramos el desvío (Error se maneja si llega al destino incorrecto)
                    DataTracker.Instance.RecordDeviation(); 
                    isOffPath = true;
                }
                else if (minPathDist < 2.0f)
                {
                    isOffPath = false;
                }
            }

            if (path.status == NavMeshPathStatus.PathPartial && !hasLoggedPartialPath)
            {
                // Solo logueamos si el final de la ruta calculada está realmente lejos del objetivo (ej: > 1m)
                float endThreshold = Vector3.Distance(corners[corners.Length - 1], targetPos);
                if (endThreshold > 1.0f)
                {
                    Debug.LogWarning($"[RouteDrawer] Ruta parcial: El destino está a {endThreshold:F1}m del área navegable.");
                    hasLoggedPartialPath = true;
                }
            }

            // Actualizamos la distancia para la UI
            float remainingDist = (agent.isActiveAndEnabled && agent.isOnNavMesh && agent.hasPath) ? agent.remainingDistance : float.MaxValue;
            
            // Si el agente devuelve Infinito o MaxValue, usamos la distancia física directa (distXZ)
            if (float.IsInfinity(remainingDist) || remainingDist >= float.MaxValue || path.status != NavMeshPathStatus.PathComplete)
            {
                RemainingDistance = distXZ;
            }
            else
            {
                RemainingDistance = remainingDist;
            }

            if (corners.Length < 2)
            {
                line.positionCount = 0;
                return;
            }

            // 5. Dibujar la línea con ALTA subdivisión
            System.Collections.Generic.List<Vector3> detailedPoints = new System.Collections.Generic.List<Vector3>();
            
            for (int i = 0; i < corners.Length - 1; i++)
            {
                detailedPoints.Add(corners[i]);
                float segmentDist = Vector3.Distance(corners[i], corners[i + 1]);
                
                int subdivisions = Mathf.CeilToInt(segmentDist / 0.3f);
                for (int j = 1; j < subdivisions; j++)
                {
                    Vector3 lerpPos = Vector3.Lerp(corners[i], corners[i + 1], (float)j / subdivisions);
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(lerpPos, out hit, 1.0f, NavMesh.AllAreas))
                    {
                        detailedPoints.Add(hit.position);
                    }
                }
            }
            detailedPoints.Add(corners[corners.Length - 1]);

            line.positionCount = detailedPoints.Count;
            for (int i = 0; i < detailedPoints.Count; i++)
            {
                line.SetPosition(i, detailedPoints[i] + Vector3.up * 0.5f);
            }
        }
        else
        {
            Debug.LogError("[RouteDrawer] No se pudo calcular ninguna ruta al destino.");
            ClearPath();
        }
    }

    public void ClearPath()
    {
        if (DataTracker.Instance != null && DataTracker.Instance.isTracking)
            DataTracker.Instance.StopTracking(false);

        currentTarget = null;
        line.positionCount = 0;
        hasLoggedPartialPath = false;
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
    }
}
