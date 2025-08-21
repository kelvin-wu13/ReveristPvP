using UnityEngine;

public class CharacterPanel : MonoBehaviour
{
    public GameObject[] allPanels;

    public void ShowOnly(int index)
    {
        for (int i = 0; i < allPanels.Length; i++)
        {
            allPanels[i].SetActive(i == index);
        }
    }
}
