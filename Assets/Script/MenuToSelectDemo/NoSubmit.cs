using UnityEngine;
using UnityEngine.EventSystems;

public class NoSubmit : MonoBehaviour, ISubmitHandler
{
    public void OnSubmit(BaseEventData eventData)
    {
        // Block Unity's default Submit behavior (Enter key)
        Debug.Log("Submit blocked on: " + gameObject.name);
        // Do nothing — intentionally block it
    }
}