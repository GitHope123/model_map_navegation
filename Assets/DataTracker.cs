using UnityEngine;
using System.Collections;

public class DataTracker : MonoBehaviour
{
    private static DataTracker _instance;
    public static DataTracker Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<DataTracker>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("DataTracker");
                    _instance = go.AddComponent<DataTracker>();
                }
            }
            return _instance;
        }
    }

    [Header("=== CONFIGURACIÓN DE EVALUACIÓN ===")]
    [Tooltip("Cambiar a 'post-test' cuando el estudiante ya haya completado el pre-test")]
    public string currentPhase = "pre-test";  // "pre-test" o "post-test"
    
    [Header("Referencia del Jugador")]
    [Tooltip("Arrastra aquí el Transform del jugador. Si está vacío, se buscará automáticamente.")]
    public Transform playerTransform;
    
    [Header("Métricas Actuales (Solo lectura)")]
    public string currentDestinationName;
    
    // DIMENSIÓN 1: Eficiencia en la navegación
    public float timeInSeconds;
    public float distanceTraveled;
    public int deviationCount;        // Número de veces que se desvió de la ruta
    
    // DIMENSIÓN 2: Precisión en la orientación
    public int errorCount;
    public float routeMatchPercentage; // Porcentaje de coincidencia con ruta óptima
    public bool isTracking = false;
    
    // DIMENSIÓN 3: Nivel de autonomía
    public int helpInterventions;     // Veces que solicitó ayuda

    private Vector3 lastPosition;
    private float optimalDistance;    // Distancia óptima calculada al inicio
    private const float ERROR_THRESHOLD = 3.0f; // Metros de desvío para contar error

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
        
        // Buscar automáticamente al jugador si no está asignado
        if (playerTransform == null)
        {
            // Intentar encontrar por tag "Player"
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.Log("[DataTracker] Jugador encontrado automáticamente por tag 'Player'.");
            }
            else
            {
                // Buscar por nombre común
                var fps = FindFirstObjectByType<CharacterController>();
                if (fps != null)
                {
                    playerTransform = fps.transform;
                    Debug.Log("[DataTracker] Jugador encontrado automáticamente (CharacterController).");
                }
            }
        }
    }

    public void StartTracking(string destinationName, float optimalDist)
    {
        if (SessionManager.Instance != null && SessionManager.Instance.isAdmin)
        {
            isTracking = false;
            return;
        }
        
        // Verificar que tenemos referencia al jugador
        if (playerTransform == null)
        {
            Debug.LogError("[DataTracker] No hay referencia al jugador. No se puede iniciar tracking.");
            return;
        }

        // Resetear todas las métricas
        currentDestinationName = destinationName;
        optimalDistance = optimalDist;
        timeInSeconds = 0f;
        distanceTraveled = 0f;
        deviationCount = 0;
        errorCount = 0;
        routeMatchPercentage = 100f;
        helpInterventions = 0;
        
        isTracking = true;
        lastPosition = playerTransform.position;  // Usar posición del jugador
        Debug.Log($"[DataTracker] Seguimiento iniciado para destino: {destinationName} (Distancia óptima: {optimalDist:F1}m)");
    }

    public void StopTracking(bool reachedDestination)
    {
        isTracking = false;
        
        // Calcular porcentaje de coincidencia con ruta óptima
        // Si la distancia recorrida > óptima, el porcentaje baja proporcionalmente
        if (optimalDistance > 0 && distanceTraveled > 0)
        {
            routeMatchPercentage = Mathf.Clamp((optimalDistance / distanceTraveled) * 100f, 0f, 100f);
        }
        else
        {
            routeMatchPercentage = reachedDestination ? 100f : 0f;
        }
        
        Debug.Log($"[DataTracker] Seguimiento detenido.");
        Debug.Log($"  - Destino: {currentDestinationName}");
        Debug.Log($"  - Tiempo: {timeInSeconds:F1}s");
        Debug.Log($"  - Distancia recorrida: {distanceTraveled:F1}m (Óptima: {optimalDistance:F1}m)");
        Debug.Log($"  - Coincidencia ruta: {routeMatchPercentage:F1}%");
        Debug.Log($"  - Desvíos: {deviationCount}, Errores: {errorCount}");
        Debug.Log($"  - Intervenciones de ayuda: {helpInterventions}");
        Debug.Log($"  - Llegó al destino: {reachedDestination}");
        
        // No intentamos guardar si somos Admin
        if (SessionManager.Instance != null && SessionManager.Instance.isAdmin) return;

        if (SupabaseManager.Instance != null)
        {
            SupabaseManager.Instance.SaveSession(
                currentPhase,
                currentDestinationName,
                timeInSeconds, 
                distanceTraveled, 
                deviationCount,
                errorCount, 
                routeMatchPercentage,
                reachedDestination,
                helpInterventions
            );
        }
    }

    void Update()
    {
        if (!isTracking) return;
        if (playerTransform == null) return;  // Seguridad

        // Medir Tiempo
        timeInSeconds += Time.deltaTime;

        // Medir Distancia Real del JUGADOR
        Vector3 currentPlayerPos = playerTransform.position;
        float frameDist = Vector3.Distance(currentPlayerPos, lastPosition);
        if (frameDist > 0.01f) // Pequeño umbral para evitar jitter
        {
            distanceTraveled += frameDist;
            lastPosition = currentPlayerPos;
        }
    }

    public void RecordError()
    {
        errorCount++;
        Debug.Log($"[DataTracker] Error de ruta detectado. Total: {errorCount}");
    }

    /// <summary>
    /// Registra un desvío de la ruta óptima (cuando el usuario se aleja significativamente)
    /// </summary>
    public void RecordDeviation()
    {
        deviationCount++;
        Debug.Log($"[DataTracker] Desvío de ruta detectado. Total: {deviationCount}");
    }

    /// <summary>
    /// Registra una intervención de ayuda solicitada por el usuario
    /// </summary>
    public void RecordHelpIntervention()
    {
        helpInterventions++;
        Debug.Log($"[DataTracker] Intervención de ayuda registrada. Total: {helpInterventions}");
    }

    /// <summary>
    /// Permite cambiar la fase de evaluación (pre-test o post-test)
    /// </summary>
    public void SetPhase(string phase)
    {
        if (phase == "pre-test" || phase == "post-test")
        {
            currentPhase = phase;
            Debug.Log($"[DataTracker] Fase cambiada a: {phase}");
        }
        else
        {
            Debug.LogWarning($"[DataTracker] Fase inválida: {phase}. Use 'pre-test' o 'post-test'.");
        }
    }
}
