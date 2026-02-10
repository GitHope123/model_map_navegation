using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System.Collections;

public class NavigationUIController : MonoBehaviour
{
    [Header("Referencias de Navegación")]
    public RouteDrawer playerRoute;
    public Transform area1;
    public Transform area2;

    [Header("Miniaturas (Render Textures)")]
    public RenderTexture textureRecepcion;
    public RenderTexture textureOficinas;

    private VisualElement root;
    private VisualElement mainNavContainer;
    private VisualElement menuContainer;
    private VisualElement imgArea1;
    private VisualElement imgArea2;
    private VisualElement cardArea1;
    private VisualElement cardArea2;
    private VisualElement infoPanel;
    private Label lblStatus;
    private Label lblDistance;
    private Label lblETA;
    private VisualElement progressBarFill;
    private VisualElement iconArrow;
    private Button btnCancelNav;
    private TextField searchField;
    
    // Login UI
    private VisualElement loginScreen;
    private TextField studentIdField;
    private Toggle groupToggle;
    private Button btnStartSession;
    private Label lblNotification;

    // Success UI
    private VisualElement successPanel;
    private Label lblSuccessTarget;
    private Button btnCloseSuccess;

    private bool isOpen = false;
    private bool wrongDestinationLogged = false;

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        // Referencias Navegación
        mainNavContainer = root.Q<VisualElement>("nav-container");
        menuContainer = root.Q<VisualElement>("menuContainer");
        var toggleBtn = root.Q<Button>("btnToggleMenu");
        
        infoPanel = root.Q<VisualElement>("infoPanel");
        lblStatus = root.Q<Label>("lblStatus");
        lblDistance = root.Q<Label>("lblDistance");
        lblETA = root.Q<Label>("lblETA");
        progressBarFill = root.Q<VisualElement>("progressBarFill");
        iconArrow = root.Q<VisualElement>("iconArrow");
        btnCancelNav = root.Q<Button>("btnCancelNav");
        searchField = root.Q<TextField>("searchField");

        imgArea1 = root.Q<VisualElement>("imgArea1");
        imgArea2 = root.Q<VisualElement>("imgArea2");
        cardArea1 = root.Q<VisualElement>("cardArea1");
        cardArea2 = root.Q<VisualElement>("cardArea2");

        // Referencias Login
        loginScreen = root.Q<VisualElement>("loginScreen");
        studentIdField = root.Q<TextField>("studentIdField");
        groupToggle = root.Q<Toggle>("groupToggle");
        btnStartSession = root.Q<Button>("btnStartSession");
        lblNotification = root.Q<Label>("loginNotification");

        // Referencias Success
        successPanel = root.Q<VisualElement>("successPanel");
        lblSuccessTarget = root.Q<Label>("lblSuccessTarget");
        btnCloseSuccess = root.Q<Button>("btnCloseSuccess");

        // Inicializar RenderTextures
        if (imgArea1 != null && textureRecepcion != null)
            imgArea1.style.backgroundImage = Background.FromRenderTexture(textureRecepcion);
        if (imgArea2 != null && textureOficinas != null)
            imgArea2.style.backgroundImage = Background.FromRenderTexture(textureOficinas);

        // Configurar Eventos
        if (toggleBtn != null) toggleBtn.clicked += ToggleMenu;
        if (searchField != null) searchField.RegisterValueChangedCallback(evt => FilterDestinations(evt.newValue));
        if (btnStartSession != null) btnStartSession.clicked += OnLoginClicked;

        var btnArea1 = root.Q<Button>("btnArea1");
        if (btnArea1 != null) btnArea1.clicked += () => SetDestination(area1);

        var btnArea2 = root.Q<Button>("btnArea2");
        if (btnArea2 != null) btnArea2.clicked += () => SetDestination(area2);

        if (btnCloseSuccess != null)
            btnCloseSuccess.clicked += HideSuccess;

        if (playerRoute != null)
            playerRoute.OnDestinationReached += ShowSuccess;

        if (btnCancelNav != null)
            btnCancelNav.clicked += () => { if (playerRoute != null) playerRoute.ClearPath(); };

        // Estado inicial
        ShowLogin(true);
        if (lblNotification != null) lblNotification.style.display = DisplayStyle.None;
        LiberarMouse();
    }

    void Update()
    {
        if (SessionManager.Instance == null || !SessionManager.Instance.isSessionActive) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            ToggleMenu();

        UpdateNavigationInfo();
        CheckWrongDestination();
    }

    private void CheckWrongDestination()
    {
        if (playerRoute == null || !DataTracker.Instance.isTracking || wrongDestinationLogged) return;
        
        string currentDest = playerRoute.DestinationName;
        Vector3 playerPos = playerRoute.transform.position;
        
        // Lógica simple para detectar "Identificación Incorrecta" (Error)
        // Si estoy yendo a Area1 pero llego a Area2 (o viceversa)
        
        if (area1 != null && area2 != null)
        {
            // Caso: Voy a Area1 (Recepción) pero me acerco a Area2 (Oficinas)
            if (currentDest == area1.name && Vector3.Distance(playerPos, area2.position) < 4.0f)
            {
                DataTracker.Instance.RecordError();
                wrongDestinationLogged = true;
                return;
            }
            
            // Caso: Voy a Area2 pero me acerco a Area1
            if (currentDest == area2.name && Vector3.Distance(playerPos, area1.position) < 4.0f)
            {
                DataTracker.Instance.RecordError();
                wrongDestinationLogged = true;
                return;
            }
        }
    }

    private void OnLoginClicked()
    {
        string id = studentIdField?.value;
        if (string.IsNullOrEmpty(id))
        {
            ShowNotification("Por favor, ingrese su Código o DNI.");
            return;
        }

        // --- MODO ADMINISTRADOR (Master Key) ---
        if (id.ToUpper() == "ADMIN2026")
        {
            SessionManager.Instance.StartAdminSession();
            ShowLogin(false);
            BloquearMouse();
            Debug.Log("[MODO ADMIN] Acceso total concedido.");
            return;
        }

        if (SessionManager.Instance == null)
        {
            ShowNotification("Error de sistema: Falta SessionManager.");
            return;
        }

        btnStartSession.SetEnabled(false);
        btnStartSession.text = "VERIFICANDO...";

        bool isExperimental = groupToggle.value;
        
        SessionManager.Instance.StartSession(id, isExperimental, (success, message) => {
            btnStartSession.SetEnabled(true);
            btnStartSession.text = "COMENZAR EVALUACIÓN";

            if (success)
            {
                // La fase se controla desde el Inspector de DataTracker
                Debug.Log($"[Fase actual] {DataTracker.Instance?.currentPhase ?? "pre-test"}");
                
                ShowLogin(false);
                BloquearMouse();
            }
            else
            {
                if (message == "duplicate")
                    ShowNotification("Este código ya ha sido registrado para evaluación.");
                else
                    ShowNotification("Error de conexión con el servidor.");
            }
        });
    }

    private void ShowSuccess(string targetName)
    {
        if (lblSuccessTarget != null)
            lblSuccessTarget.text = $"Has llegado con éxito a: {targetName}";
        
        if (successPanel != null)
            successPanel.style.display = DisplayStyle.Flex;

        LiberarMouse();
    }

    private void HideSuccess()
    {
        if (successPanel != null)
            successPanel.style.display = DisplayStyle.None;

        BloquearMouse();
    }

    private void ShowNotification(string message)
    {
        if (lblNotification != null)
        {
            lblNotification.text = message;
            lblNotification.style.display = DisplayStyle.Flex;
            
            // Auto ocultar después de 4 segundos
            StopAllCoroutines();
            StartCoroutine(HideNotificationAfterDelay(4f));
        }
    }

    private IEnumerator HideNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (lblNotification != null) lblNotification.style.display = DisplayStyle.None;
    }

    private void ShowLogin(bool show)
    {
        if (loginScreen != null) loginScreen.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        if (mainNavContainer != null) mainNavContainer.style.display = show ? DisplayStyle.None : DisplayStyle.Flex;
        
        if (show && infoPanel != null)
            infoPanel.style.display = DisplayStyle.None;

        if (!show && SessionManager.Instance != null && !SessionManager.Instance.isExperimentalGroup)
        {
            if (playerRoute != null) playerRoute.GetComponent<LineRenderer>().enabled = false;
        }
    }

    private void UpdateNavigationInfo()
    {
        if (playerRoute != null)
        {
            float dist = playerRoute.RemainingDistance;
            bool hasDestination = playerRoute.DestinationName != "Sin destino" && dist > 1.5f;
            bool showAids = SessionManager.Instance.isExperimentalGroup;

            if (infoPanel != null)
                infoPanel.style.display = (hasDestination && showAids) ? DisplayStyle.Flex : DisplayStyle.None;

            if (lblStatus != null) lblStatus.text = playerRoute.DestinationName;
            if (lblDistance != null) lblDistance.text = dist > 0 ? $"{dist:F1} m" : "-- m";

            if (lblETA != null)
            {
                float etaSeconds = dist / 1.5f;
                lblETA.text = dist > 1.5f ? $"{etaSeconds:F0} seg" : "Llegando...";
            }

            if (progressBarFill != null && playerRoute.TotalDistance > 0)
            {
                float progress = 1.0f - (dist / playerRoute.TotalDistance);
                progressBarFill.style.width = Length.Percent(Mathf.Clamp(progress * 100f, 0, 100f));
            }

            if (iconArrow != null)
            {
                iconArrow.style.display = (hasDestination && showAids) ? DisplayStyle.Flex : DisplayStyle.None;
                if (playerRoute.CurrentTarget != null)
                {
                    Vector3 directionToTarget = playerRoute.CurrentTarget.position - Camera.main.transform.position;
                    directionToTarget.y = 0;
                    float angle = Vector3.SignedAngle(Camera.main.transform.forward, directionToTarget, Vector3.up);
                    iconArrow.style.rotate = new Rotate(angle);
                }
            }
        }
    }

    private void FilterDestinations(string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            if (cardArea1 != null) cardArea1.style.display = DisplayStyle.Flex;
            if (cardArea2 != null) cardArea2.style.display = DisplayStyle.Flex;
            return;
        }
        string lowerSearch = searchText.ToLower();
        if (cardArea1 != null) cardArea1.style.display = area1.name.ToLower().Contains(lowerSearch) || "recepción".Contains(lowerSearch) ? DisplayStyle.Flex : DisplayStyle.None;
        if (cardArea2 != null) cardArea2.style.display = area2.name.ToLower().Contains(lowerSearch) || "oficinas".Contains(lowerSearch) ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void SetDestination(Transform target)
    {
        if (playerRoute != null && target != null)
        {
            playerRoute.SetDestination(target);
            wrongDestinationLogged = false; // Reiniciar flag de error para el nuevo viaje
        }
        CloseMenu();
    }

    public void ToggleMenu()
    {
        isOpen = !isOpen;
        ActualizarEstadoVisibilidad();
        if (isOpen) 
        {
            LiberarMouse();
            // Registrar intervención de ayuda al abrir el menú (Consultar mapa/lista)
            if (DataTracker.Instance != null && DataTracker.Instance.isTracking)
            {
                DataTracker.Instance.RecordHelpIntervention();
            }
        } 
        else 
        {
            BloquearMouse();
        }
    }

    private void CloseMenu()
    {
        isOpen = false;
        ActualizarEstadoVisibilidad();
        BloquearMouse();
    }

    private void ActualizarEstadoVisibilidad()
    {
        var display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;
        if (menuContainer != null) menuContainer.style.display = display;
        if (searchField != null)
        {
            searchField.style.display = display;
            if (!isOpen) searchField.Blur();
        }
    }

    private void BloquearMouse() { UnityEngine.Cursor.lockState = CursorLockMode.Locked; UnityEngine.Cursor.visible = false; }
    private void LiberarMouse() { UnityEngine.Cursor.lockState = CursorLockMode.None; UnityEngine.Cursor.visible = true; }
}
