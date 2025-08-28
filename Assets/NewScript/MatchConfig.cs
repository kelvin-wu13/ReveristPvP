using UnityEngine;

[CreateAssetMenu(menuName = "Game/Match Config", fileName = "MatchConfig", order = 2)]
public class MatchConfig : ScriptableObject
{
    public int[] p1DeviceIds;
    public int[] p2DeviceIds;

    public GameObject p1Prefab;
    public GameObject p2Prefab;
    public string battleSceneName = "Battle";
    public void Clear()
    {
        p1Prefab = null;
        p2Prefab = null;
        p1DeviceIds = null;
        p2DeviceIds = null;
    }
}
