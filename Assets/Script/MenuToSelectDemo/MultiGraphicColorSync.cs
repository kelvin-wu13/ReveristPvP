using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(Button))]
public class MultiGraphicColorSync : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    public List<Graphic> graphicsToColor;

    private Button btn;
    private bool isHovered = false;
    private bool isPressed = false;
    private bool isSelected = false;

    void Awake()
    {
        btn = GetComponent<Button>();
    }

    void Update()
    {
        if (btn == null || graphicsToColor == null) return;

        ColorBlock colors = btn.colors;
        Color targetColor = colors.normalColor;

        if (!btn.interactable)
        {
            targetColor = colors.disabledColor;
        }
        else if (isPressed)
        {
            targetColor = colors.pressedColor;
        }
        else if (isHovered)
        {
            targetColor = colors.highlightedColor;
        }
        else if (isSelected)
        {
            targetColor = colors.selectedColor;
        }

        foreach (var g in graphicsToColor)
        {
            if (g != null)
                g.color = targetColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
    }
}
