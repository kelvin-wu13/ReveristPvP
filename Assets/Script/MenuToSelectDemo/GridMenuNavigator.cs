using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.EventSystems;
//using UnityEngine.UIElements;
//using static UnityEngine.Rendering.DebugUI;
//using Button = UnityEngine.UI.Button;

public class GridMenuNavigator : MonoBehaviour
//{
//    public Button[] buttons;
//    public int columns = 4;
//    private int currentIndex = 0;

//    void Start()
//    {
//        SelectButton(currentIndex);
//    }

//    void Update()
//    {
//        if (Input.GetKeyDown(KeyCode.RightArrow))
//        {
//            Move(1);
//        }
//        else if (Input.GetKeyDown(KeyCode.LeftArrow))
//        {
//            Move(-1);
//        }
//        else if (Input.GetKeyDown(KeyCode.UpArrow))
//        {
//            Move(-columns);
//        }
//        else if (Input.GetKeyDown(KeyCode.DownArrow))
//        {
//            Move(columns);
//        }
//    }

//    void Move(int offset)
//    {
//        int newIndex = (currentIndex + offset + buttons.Length) % buttons.Length;
//        currentIndex = newIndex;
//        SelectButton(currentIndex);
//    }

//    void SelectButton(int index)
//    {
//        if (buttons[index] != null)
//        {
//            buttons[index].Select();
//        }
//    }
//}



//{
//    public Button[] buttons;         // Fill in order: A B C D E F G X (X is optional dummy)
//    public int columns = 4;          // How many buttons per row

//    private int currentIndex = -1;

//    void Update()
//    {
//        if (Input.GetKeyDown(KeyCode.UpArrow))
//        {
//            Move(-columns);
//        }
//        else if (Input.GetKeyDown(KeyCode.DownArrow))
//        {
//            Move(columns);
//        }
//        else if (Input.GetKeyDown(KeyCode.LeftArrow))
//        {
//            Move(-1);
//        }
//        else if (Input.GetKeyDown(KeyCode.RightArrow))
//        {
//            Move(1);
//        }
//    }

//    void Move(int offset)
//    {
//        if (buttons.Length == 0) return;

//        // If nothing selected yet, start at 0
//        if (currentIndex == -1 || EventSystem.current.currentSelectedGameObject == null)
//        {
//            currentIndex = 0;
//            buttons[currentIndex].Select();
//            return;
//        }

//        int newIndex = currentIndex;

//        for (int i = 0; i < buttons.Length; i++) // safety loop
//        {
//            newIndex = (newIndex + offset + buttons.Length) % buttons.Length;
//            if (buttons[newIndex] != null && buttons[newIndex].interactable)
//            {
//                currentIndex = newIndex;
//                buttons[currentIndex].Select();
//                return;
//            }
//        }
//    }
//}



//{
//    public Button[] buttons;
//    public int columns = 4;

//    private int currentIndex = 0;

//    void Start()
//    {
//        if (buttons.Length > 0 && buttons[0] != null)
//        {
//            buttons[0].Select();
//        }
//    }

//    void Update()
//    {
//        if (Input.GetKeyDown(KeyCode.RightArrow)) Navigate(1, 0);
//        if (Input.GetKeyDown(KeyCode.LeftArrow)) Navigate(-1, 0);
//        if (Input.GetKeyDown(KeyCode.UpArrow)) Navigate(0, -1);
//        if (Input.GetKeyDown(KeyCode.DownArrow)) Navigate(0, 1);
//    }

//    void Navigate(int dirX, int dirY)
//    {
//        int total = buttons.Length;
//        int rows = Mathf.CeilToInt(total / (float)columns);

//        int currentRow = currentIndex / columns;
//        int currentCol = currentIndex % columns;

//        int targetRow = Mathf.Clamp(currentRow + dirY, 0, rows - 1);
//        int targetCol = Mathf.Clamp(currentCol + dirX, 0, columns - 1);

//        int newIndex = targetRow * columns + targetCol;

//        // Make sure it's a valid button
//        if (newIndex < total && buttons[newIndex] != null)
//        {
//            buttons[newIndex].Select();
//            currentIndex = newIndex;
//        }
//    }
//}



//{
//    public Button[] buttons;

//    private int currentIndex = 0;

//    void Start()
//    {
//        if (buttons.Length > 0 && buttons[0] != null)
//        {
//            buttons[0].Select();
//        }
//    }

//    void Update()
//    {
//        if (Input.GetKeyDown(KeyCode.RightArrow)) MoveRight();
//        if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveLeft();
//        if (Input.GetKeyDown(KeyCode.UpArrow)) MoveUp();
//        if (Input.GetKeyDown(KeyCode.DownArrow)) MoveDown();
//    }

//    void MoveRight()
//    {
//        int newIndex = currentIndex;

//        // Custom cases
//        if (currentIndex == 3) newIndex = 0;         // D ? A
//        else if (currentIndex == 6) newIndex = 3;    // G ? D
//        else newIndex = currentIndex + 1;

//        TryMoveTo(newIndex);
//    }

//    void MoveLeft()
//    {
//        int newIndex = currentIndex;

//        // Custom cases
//        if (currentIndex == 0) newIndex = 3;         // A ? D
//        else if (currentIndex == 4) newIndex = 6;    // E ? G
//        else newIndex = currentIndex - 1;

//        TryMoveTo(newIndex);
//    }

//    void MoveDown()
//    {
//        int newIndex = currentIndex;

//        // Custom case
//        if (currentIndex == 3) newIndex = 6;         // D ? G
//        else newIndex = currentIndex + 4;

//        TryMoveTo(newIndex);
//    }

//    void MoveUp()
//    {
//        int newIndex = currentIndex - 4;
//        TryMoveTo(newIndex);
//    }

//    void TryMoveTo(int newIndex)
//    {
//        if (newIndex >= 0 && newIndex < buttons.Length && buttons[newIndex] != null)
//        {
//            buttons[newIndex].Select();
//            currentIndex = newIndex;
//        }
//    }
//}



{
    public Button[] buttons;
    private int currentIndex = 0;

    public float initialDelay = 0.3f;
    public float repeatRate = 0.1f;

    private float nextMoveTime = 0f;
    private Vector2 heldInput = Vector2.zero;
    private bool hasMoved = false;

    void Start()
    {
        if (buttons.Length > 0 && buttons[0] != null)
        {
            buttons[0].Select();
        }
    }

    void Update()
    {
        Vector2 input = Vector2.zero;

        if (Input.GetKey(KeyCode.RightArrow)) input.x = 1;
        else if (Input.GetKey(KeyCode.LeftArrow)) input.x = -1;

        if (Input.GetKey(KeyCode.DownArrow)) input.y = 1;
        else if (Input.GetKey(KeyCode.UpArrow)) input.y = -1;

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
    }

    void ProcessInput(Vector2 input)
    {
        if (input.x != 0) MoveHorizontal((int)input.x);
        else if (input.y != 0) MoveVertical((int)input.y);
    }

    void MoveHorizontal(int direction)
    {
        int newIndex = currentIndex + direction;

        if (currentIndex <= 3) // top row
        {
            if (newIndex > 3) newIndex = 0;
            if (newIndex < 0) newIndex = 3;
        }
        else // bottom row (4–6)
        {
            if (newIndex > 6) newIndex = 4;
            if (newIndex < 4) newIndex = 6;
        }

        TryMoveTo(newIndex);
    }

    void MoveVertical(int direction)
    {
        int newIndex = currentIndex;

        if (direction == 1) // down
        {
            if (currentIndex >= 0 && currentIndex <= 3) // top row
            {
                switch (currentIndex)
                {
                    case 0: newIndex = 4; break;
                    case 1: newIndex = 5; break;
                    case 2: newIndex = 6; break;
                    case 3: newIndex = 6; break;
                }
            }
            else // wrap to top
            {
                switch (currentIndex)
                {
                    case 4: newIndex = 0; break;
                    case 5: newIndex = 1; break;
                    case 6: newIndex = 2; break;
                }
            }
        }
        else if (direction == -1) // up
        {
            if (currentIndex >= 4 && currentIndex <= 6) // bottom row
            {
                switch (currentIndex)
                {
                    case 4: newIndex = 0; break;
                    case 5: newIndex = 1; break;
                    case 6: newIndex = 2; break;
                }
            }
            else // wrap to bottom
            {
                switch (currentIndex)
                {
                    case 0: newIndex = 4; break;
                    case 1: newIndex = 5; break;
                    case 2: newIndex = 6; break;
                    case 3: newIndex = 6; break;
                }
            }
        }

        TryMoveTo(newIndex);
    }

    void TryMoveTo(int newIndex)
    {
        if (newIndex >= 0 && newIndex < buttons.Length && buttons[newIndex] != null)
        {
            buttons[newIndex].Select();
            currentIndex = newIndex;
        }
    }
}