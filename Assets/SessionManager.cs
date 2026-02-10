using UnityEngine;

public class SessionManager : MonoBehaviour
{
    private static SessionManager _instance;
    public static SessionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SessionManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SessionManager");
                    _instance = go.AddComponent<SessionManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Session Data")]
    public string studentID;
    public string estudianteUUID;
    public bool isExperimentalGroup;
    public bool isSessionActive = false;
    public bool isAdmin = false;

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
    }

    public void StartSession(string id, bool experimental, System.Action<bool, string> callback)
    {
        studentID = id;
        isExperimentalGroup = experimental;
        
        string grupoStr = isExperimentalGroup ? "experimental" : "control";
        if (SupabaseManager.Instance != null)
        {
            SupabaseManager.Instance.RegisterEstudiante(id, grupoStr, (success, message) => {
                if (success)
                {
                    isSessionActive = true;
                    Debug.Log($"[SessionManager] Sesion iniciada para: {studentID} - Grupo: {grupoStr}");
                }
                callback?.Invoke(success, message);
            });
        }
    }

    public void StartAdminSession()
    {
        studentID = "ADMIN_MODE";
        isAdmin = true;
        isExperimentalGroup = true; // Activa todas las guías
        isSessionActive = true;
        Debug.Log("[SessionManager] Iniciada sesion de ADMINISTRADOR libre.");
    }

    public void SetEstudianteUUID(string uuid)
    {
        estudianteUUID = uuid;
    }
}
