//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

//public class MenuNavigator : MonoBehaviour
//{
//    public Button[] buttons;
//    private int currentIndex = -1;

//    void Update()
//    {
//        if (Input.GetKeyDown(KeyCode.UpArrow))
//        {
//            Navigate(-1);
//        }
//        else if (Input.GetKeyDown(KeyCode.DownArrow))
//        {
//            Navigate(1);
//        }
//    }

//    void Navigate(int direction)
//    {
//        // If nothing selected yet, select first
//        if (currentIndex == -1 || EventSystem.current.currentSelectedGameObject == null)
//        {
//            currentIndex = 0;
//            buttons[currentIndex].Select();
//            return;
//        }

//        // Move index with wraparound
//        currentIndex = (currentIndex + direction + buttons.Length) % buttons.Length;
//        buttons[currentIndex].Select();
//    }
//}



//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

//public class MenuNavigator : MonoBehaviour
//{
//    public Button[] buttons;
//    private int currentIndex = -1;

//    void Update()
//    {
//        if (Input.GetKeyDown(KeyCode.UpArrow))
//        {
//            Navigate(-1);
//        }
//        else if (Input.GetKeyDown(KeyCode.DownArrow))
//        {
//            Navigate(1);
//        }
//    }

//    void Navigate(int direction)
//    {
//        if (buttons.Length == 0) return;

//        // If nothing selected yet
//        if (currentIndex == -1 || EventSystem.current.currentSelectedGameObject == null)
//        {
//            currentIndex = 0;
//        }
//        else
//        {
//            currentIndex = (currentIndex + direction + buttons.Length) % buttons.Length;
//        }

//        buttons[currentIndex].Select();
//    }
//}



using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuNavigator : MonoBehaviour
{
    public Button[] buttons;
    private int currentIndex = -1;

    public float initialDelay = 0.3f;
    public float repeatRate = 0.1f;

    private float holdTimer = 0f;
    private int heldDirection = 0;
    private bool isHolding = false;

    void Update()
    {
        int direction = 0;

        if (Input.GetKey(KeyCode.DownArrow)) direction = 1;
        else if (Input.GetKey(KeyCode.UpArrow)) direction = -1;

        if (direction != 0)
        {
            if (!isHolding || direction != heldDirection)
            {
                Navigate(direction);
                holdTimer = Time.unscaledTime + initialDelay;
                isHolding = true;
                heldDirection = direction;
            }
            else if (Time.unscaledTime >= holdTimer)
            {
                Navigate(direction);
                holdTimer = Time.unscaledTime + repeatRate;
            }
        }
        else
        {
            isHolding = false;
            heldDirection = 0;
            holdTimer = 0f;
        }
    }

    void Navigate(int direction)
    {
        if (buttons.Length == 0) return;

        // First time selection
        if (currentIndex == -1 || EventSystem.current.currentSelectedGameObject == null)
        {
            currentIndex = 0;
        }
        else
        {
            currentIndex = (currentIndex + direction + buttons.Length) % buttons.Length;
        }

        buttons[currentIndex].Select();
    }
}
