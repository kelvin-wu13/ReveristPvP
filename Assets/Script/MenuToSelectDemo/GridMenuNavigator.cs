using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GridMenuNavigator : MonoBehaviour
{
    [Header("Buttons (urut kiri→kanan, atas→bawah)")]
    public Button[] buttons;
    [Min(1)] public int columns = 4;

    [Header("Repeat")]
    public float initialDelay = 0.30f;
    public float repeatRate = 0.10f;
    [Range(0.1f, 0.9f)] public float stickDeadzone = 0.5f;

    [Header("Input System (opsional)")]
    public PlayerInput playerInput;                
    public InputActionReference navigateActionRef;
    public InputActionReference submitActionRef;

    private InputAction _navigate;
    private InputAction _submit;

    private int currentIndex = 0;
    private float nextMoveTime = 0f;
    private Vector2 heldInput = Vector2.zero;
    private bool hasMoved = false;

    private void Awake()
    {
        if (!playerInput) TryGetComponent(out playerInput);

        if (navigateActionRef && navigateActionRef.action != null)
            _navigate = navigateActionRef.action;
        else if (playerInput && playerInput.actions)
            _navigate = playerInput.actions.FindAction("UI/Navigate", false)
                     ?? playerInput.actions.FindAction("Navigate", false)
                     ?? playerInput.actions.FindAction("Move", false);

        if (submitActionRef && submitActionRef.action != null)
            _submit = submitActionRef.action;
        else if (playerInput && playerInput.actions)
            _submit = playerInput.actions.FindAction("UI/Submit", false)
                    ?? playerInput.actions.FindAction("Submit", false);
    }

    private void OnEnable()
    {
        _navigate?.Enable();
        _submit?.Enable();
    }

    private void OnDisable()
    {
        _navigate?.Disable();
        _submit?.Disable();
    }

    private void Start()
    {
        if (buttons != null && buttons.Length > 0 && buttons[0] != null)
        {
            currentIndex = 0;
            buttons[0].Select();
        }
    }

    private void Update()
    {
        // 1) Baca input arah (Vector2) dari Input System
        Vector2 raw = Vector2.zero;

        if (_navigate != null)
        {
            raw = _navigate.ReadValue<Vector2>();
        }
        else
        {
            // Fallback langsung dari device (tanpa PlayerInput)
            if (Keyboard.current != null)
            {
                if (Keyboard.current.rightArrowKey.isPressed) raw.x = 1;
                else if (Keyboard.current.leftArrowKey.isPressed) raw.x = -1;
                if (Keyboard.current.downArrowKey.isPressed) raw.y = 1;
                else if (Keyboard.current.upArrowKey.isPressed) raw.y = -1;
            }
            if (Gamepad.current != null)
            {
                // pakai input terbesar antara Dpad & stick kiri
                Vector2 d = Gamepad.current.dpad.ReadValue();
                Vector2 s = Gamepad.current.leftStick.ReadValue();
                raw = (d.sqrMagnitude > s.sqrMagnitude) ? d : s;
            }
        }

        // 2) Diskretkan (deadzone + prioritas axis dominan)
        Vector2 input = Vector2.zero;
        if (Mathf.Abs(raw.x) > Mathf.Abs(raw.y))
            input.x = Mathf.Abs(raw.x) > stickDeadzone ? Mathf.Sign(raw.x) : 0f;
        else
            input.y = Mathf.Abs(raw.y) > stickDeadzone ? Mathf.Sign(raw.y) : 0f;

        // 3) Repeat logic (initial delay → repeat)
        if (input != Vector2.zero)
        {
            if (!hasMoved || input != heldInput)
            {
                ProcessInput(input);
                nextMoveTime = Time.unscaledTime + initialDelay;
                hasMoved = true;
                heldInput = input;
            }
            else if (Time.unscaledTime >= nextMoveTime)
            {
                ProcessInput(input);
                nextMoveTime = Time.unscaledTime + repeatRate;
            }
        }
        else
        {
            hasMoved = false;
            heldInput = Vector2.zero;
        }

        // 4) Submit (Enter / A / South button)
        if (_submit != null)
        {
            if (_submit.WasPerformedThisFrame())
                ClickCurrent();
        }
        else
        {
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                ClickCurrent();
            else if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
                ClickCurrent();
        }
    }

    private void ProcessInput(Vector2 input)
    {
        if (input.x != 0) MoveHorizontal((int)input.x);
        else if (input.y != 0) MoveVertical((int)input.y);
    }

    private void MoveHorizontal(int dir)
    {
        int rows = Mathf.CeilToInt(buttons.Length / (float)columns);
        int curRow = currentIndex / columns;
        int curCol = currentIndex % columns;

        int targetCol = curCol + dir;
        // wrap di baris yang sama
        if (targetCol >= columns) targetCol = 0;
        if (targetCol < 0) targetCol = columns - 1;

        int newIndex = curRow * columns + targetCol;
        // pastikan index valid (ada tombolnya)
        newIndex = ClampToExisting(newIndex, curRow);
        TryMoveTo(newIndex);
    }

    private void MoveVertical(int dir)
    {
        int rows = Mathf.CeilToInt(buttons.Length / (float)columns);
        int curRow = currentIndex / columns;
        int curCol = currentIndex % columns;

        int targetRow = curRow + dir; // down = +1, up = -1
        if (targetRow >= rows) targetRow = 0;           // wrap
        if (targetRow < 0) targetRow = rows - 1;

        int newIndex = targetRow * columns + curCol;
        newIndex = ClampToExisting(newIndex, targetRow);
        TryMoveTo(newIndex);
    }

    private int ClampToExisting(int index, int row)
    {
        int start = row * columns;
        int end = Mathf.Min(start + columns - 1, buttons.Length - 1);
        return Mathf.Clamp(index, start, end);
    }

    private void TryMoveTo(int newIndex)
    {
        if (newIndex >= 0 && newIndex < buttons.Length && buttons[newIndex] != null)
        {
            currentIndex = newIndex;
            buttons[newIndex].Select();
        }
    }

    private void ClickCurrent()
    {
        if (currentIndex >= 0 && currentIndex < buttons.Length && buttons[currentIndex] != null)
        {
            var btn = buttons[currentIndex];
            btn.onClick?.Invoke();
            ExecuteEvents.Execute(btn.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }
    }
    public void RebindTo(PlayerInput pi)
    {
        _navigate?.Disable();
        _submit?.Disable();

        playerInput = pi;

        if (playerInput && playerInput.actions)
        {
            _navigate = playerInput.actions.FindAction("UI/Navigate", false)
                       ?? playerInput.actions.FindAction("Navigate", false)
                       ?? playerInput.actions.FindAction("Move", false);

            _submit = playerInput.actions.FindAction("UI/Submit", false)
                       ?? playerInput.actions.FindAction("Submit", false);
        }
        else
        {
            _navigate = navigateActionRef ? navigateActionRef.action : null;
            _submit = submitActionRef ? submitActionRef.action : null;
        }

        _navigate?.Enable();
        _submit?.Enable();
    }

}
