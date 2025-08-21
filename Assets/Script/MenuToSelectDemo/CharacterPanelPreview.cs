using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterPanelPreview : MonoBehaviour, ISelectHandler
{
    public CharacterPanel panelManager;
    public int index;

    public void OnSelect(BaseEventData eventData)
    {
        if (panelManager != null)
        {
            panelManager.ShowOnly(index);
        }
        else
        {
            Debug.LogWarning("CharacterPanelPreview: panelManager not assigned.");
        }
    }
}