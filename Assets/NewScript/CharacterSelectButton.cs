using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CharacterSelectButton : MonoBehaviour
{
    public GameObject characterPrefab;
    CharacterSelectManager mgr;
    void Awake()
    {
        mgr = FindObjectOfType<CharacterSelectManager>(true);
        GetComponent<Button>().onClick.AddListener(() => {
            if (mgr && characterPrefab) mgr.OnCharacterClicked(characterPrefab);
        });
    }
}
