using UnityEngine;
using UnityEngine.InputSystem;
using GameplayIngredients.Controllers;
using GameOptionsUtility;
using GameplayIngredients;
using GameplayIngredients.Interactions;


public class SpaceshipFPSPlayerInput : GameplayIngredients.Controllers.PlayerInput
{
    [Header("Behaviour")]
    public float LookExponent = 2.0f;
    [Range(0.0f, 0.7f)]
    public float MovementDeadZone = 0.15f;
    [Range(0.0f, 0.7f)]
    public float LookDeadZone = 0.15f;

    Vector2 m_Movement;
    Vector2 m_Look;

    public override Vector2 Look => m_Look;
    public override Vector2 Movement => m_Movement;
    public override ButtonState Jump => ButtonState.Released;

    private SpaceshipOptions options;
    private FirstPersonController fps;

    void Start()
    {
        options = GameOption.Get<SpaceshipOptions>();
        fps = FindObjectOfType<FirstPersonController>();
    }

    public override void UpdateInput()
    {
        if (options == null)
            options = GameOption.Get<SpaceshipOptions>();

        SpaceshipOptions.FPSKeys keys = options.fpsKeys;

        Vector2 gamepadMove = Vector2.zero;
        if (Gamepad.current != null)
            gamepadMove = Gamepad.current.leftStick.ReadValue();

        m_Movement = gamepadMove;

        if (Keyboard.current != null)
        {
            if (Keyboard.current[(UnityEngine.InputSystem.Key)keys.left].isPressed) m_Movement.x -= 1f;
            if (Keyboard.current[(UnityEngine.InputSystem.Key)keys.right].isPressed) m_Movement.x += 1f;
            if (Keyboard.current[(UnityEngine.InputSystem.Key)keys.back].isPressed) m_Movement.y -= 1f;
            if (Keyboard.current[(UnityEngine.InputSystem.Key)keys.forward].isPressed) m_Movement.y += 1f;
        }

        if (m_Movement.magnitude < MovementDeadZone)
            m_Movement = Vector2.zero;
        if (m_Movement.sqrMagnitude > 1)
            m_Movement.Normalize();

        Vector2 gamepadLook = Vector2.zero;
        if (Gamepad.current != null)
            gamepadLook = Gamepad.current.rightStick.ReadValue();

        float magnitude = Mathf.Clamp01(gamepadLook.magnitude - LookDeadZone) / (1f - LookDeadZone);
        Vector2 processedGamepadLook = gamepadLook.normalized * Mathf.Pow(magnitude, LookExponent);

        Vector2 mouseLook = Vector2.zero;
        if (Mouse.current != null)
        {
            mouseLook = Mouse.current.delta.ReadValue();
            mouseLook *= 0.02f;
        }

        m_Look = processedGamepadLook + mouseLook;

        var user = FindObjectOfType<InteractiveUser>();
        if (user != null)
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                user.Interact();

            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
                user.Interact();
        }
    }
}
