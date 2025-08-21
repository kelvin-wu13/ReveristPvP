using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class CharacterConfirmHandler : MonoBehaviour
{
    [System.Serializable]
    public class CharacterEntry
    {
        public GameObject iconButton;       // Character icon button in grid
        public Button confirmButton;        // Confirm button in panel
    }

    public List<CharacterEntry> characterEntries;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            GameObject current = EventSystem.current.currentSelectedGameObject;

            foreach (var entry in characterEntries)
            {
                if (current == entry.iconButton && entry.confirmButton != null)
                {
                    if (entry.confirmButton.gameObject.activeInHierarchy)
                    {
                        entry.confirmButton.onClick.Invoke();
                        break;
                    }
                }
            }
        }
    }
}