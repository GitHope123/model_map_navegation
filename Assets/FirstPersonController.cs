using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    public Camera fpCamera;
    public float sensitivity = 0.5f; 
    public float walkSpeed = 5f;
    public float gravity = -9.81f;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float verticalRotation = 0f;
    private Vector3 velocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    void Update()
    {
        // Si el cursor no está bloqueado (ej: Login o Menú abierto), no permitimos movimiento ni rotación
        if (UnityEngine.Cursor.lockState != CursorLockMode.Locked) 
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            return;
        }

        if (fpCamera == null) return;

        // Mirar
        transform.Rotate(Vector3.up * lookInput.x * sensitivity);
        verticalRotation -= lookInput.y * sensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, -85f, 85f);
        fpCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);

        // Mover
        if (controller.isGrounded && velocity.y < 0) velocity.y = -2f;

        Vector3 moveDir = transform.forward * moveInput.y + transform.right * moveInput.x;
        controller.Move(moveDir * walkSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
