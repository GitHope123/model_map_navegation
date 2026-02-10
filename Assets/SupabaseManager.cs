using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

public class SupabaseManager : MonoBehaviour
{
    private static SupabaseManager _instance;
    public static SupabaseManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SupabaseManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SupabaseManager");
                    _instance = go.AddComponent<SupabaseManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Supabase Config")]
    public string supabaseUrl = "https://uvfwbutfmaybivuusnfl.supabase.co";
    public string supabaseKey = "sb_publishable_ToMV88s8TXhni1GQXRjWKw_oi7nFklu";

    [System.Serializable]
    public class EstudianteData {
        public string codigo_estudiante;
        public string grupo;
    }

    [System.Serializable]
    public class SesionData {
        public string estudiante_id;
        
        // Control metodológico
        public string fase;           // "pre-test" o "post-test"
        public string nombre_destino;
        
        // DIMENSIÓN 1: Eficiencia en la navegación
        public float tiempo_segundos;
        public float distancia_metros;
        public int desvios_ruta;
        
        // DIMENSIÓN 2: Precisión en la orientación
        public int errores_ruta;
        public float coincidencia_ruta_porcentaje;
        public bool llego_destino;
        
        // DIMENSIÓN 3: Nivel de autonomía
        public int intervenciones_ayuda;
        public bool recorrido_independiente; // Nuevo campo acorde a la tesis
    }

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

    public void RegisterEstudiante(string codigo, string grupo, Action<bool, string> callback)
    {
        EstudianteData data = new EstudianteData { codigo_estudiante = codigo, grupo = grupo };
        string url = supabaseUrl + "/rest/v1/estudiantes";
        StartCoroutine(PostRequest(url, JsonUtility.ToJson(data), true, callback));
    }

    public void SaveSession(string fase, string destino, float tiempo, float distancia, int desvios, int errores, float coincidencia, bool llego, int ayudas)
    {
        // Assuming SessionManager.Instance.estudianteUUID is the equivalent of currentEstudianteID
        if (string.IsNullOrEmpty(SessionManager.Instance.estudianteUUID))
        {
            Debug.LogError("[Supabase] No hay UUID de estudiante para guardar la sesion.");
            return;
        }

        // Definición de Recorrido Independiente según Matriz:
        // Si no hubo intervenciones de ayuda (ayudas == 0), se considera independiente.
        bool esIndependiente = (ayudas == 0);

        var sessionData = new SesionData
        {
            estudiante_id = SessionManager.Instance.estudianteUUID,
            fase = fase,
            nombre_destino = destino,
            tiempo_segundos = tiempo,
            distancia_metros = distancia,
            desvios_ruta = desvios,
            errores_ruta = errores,
            coincidencia_ruta_porcentaje = coincidencia,
            llego_destino = llego,
            intervenciones_ayuda = ayudas,
            recorrido_independiente = esIndependiente // Nuevo campo
        };
        StartCoroutine(PostRequest(supabaseUrl + "/rest/v1/sesiones_evaluacion", JsonUtility.ToJson(sessionData), false, null));
    }

    IEnumerator PostRequest(string url, string json, bool isEstudiante, Action<bool, string> callback)
    {
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", "Bearer " + supabaseKey);
            request.SetRequestHeader("Prefer", "return=representation"); 

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMsg = request.downloadHandler.text;
                long responseCode = request.responseCode;

                // Si es un error 409 (Duplicado), lo manejamos de forma "silenciosa" en consola
                if (responseCode == 409 || errorMsg.Contains("23505"))
                {
                    Debug.Log("[Supabase] Intento de registro duplicado detectado (Comportamiento esperado).");
                    if (isEstudiante) callback?.Invoke(false, "duplicate");
                }
                else
                {
                    // Solo mostramos error real en la consola si es algo grave (conexión, permisos, etc.)
                    Debug.LogError($"[Supabase] Error {responseCode}: {request.error} | {errorMsg}");
                    if (isEstudiante) callback?.Invoke(false, "error");
                }
            }
            else
            {
                Debug.Log("[Supabase] Datos enviados con exito.");
                if (isEstudiante)
                {
                    string response = request.downloadHandler.text;
                    if (response.Contains("\"id\":\""))
                    {
                        string id = response.Split(new string[] { "\"id\":\"" }, StringSplitOptions.None)[1].Split('"')[0];
                        SessionManager.Instance.SetEstudianteUUID(id);
                        Debug.Log("[Supabase] UUID Estudiante recibido: " + id);
                        callback?.Invoke(true, "success");
                    }
                }
            }
        }
    }
}
