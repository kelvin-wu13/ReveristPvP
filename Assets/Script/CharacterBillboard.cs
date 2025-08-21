using UnityEngine;

public class CharacterBillboard : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
            transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Vector3.up);
    }
}
