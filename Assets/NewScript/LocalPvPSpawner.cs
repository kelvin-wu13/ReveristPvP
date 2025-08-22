using UnityEngine;

public class LocalPvPSpawner : MonoBehaviour
{
    [SerializeField] private TileGrid grid;
    [SerializeField] private GameObject playerPrefab;

    [SerializeField] private Vector2Int p1Start = new Vector2Int(1, 1);
    [SerializeField] private Vector2Int p2Start = new Vector2Int(7, 1);

    private void Start()
    {
        var p1 = Instantiate(playerPrefab);
        var mv1 = p1.GetComponent<PlayerMovement>();
        mv1.TeleportTo(p1Start);
        typeof(PlayerMovement).GetField("side", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(mv1, TileGrid.Side.Left);

        var sh1 = p1.GetComponent<PlayerShoot>();
        typeof(PlayerShoot).GetField("shootRight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(sh1, true);

        var p2 = Instantiate(playerPrefab);
        var mv2 = p2.GetComponent<PlayerMovement>();
        mv2.TeleportTo(p2Start);
        typeof(PlayerMovement).GetField("side", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(mv2, TileGrid.Side.Right);

        var sh2 = p2.GetComponent<PlayerShoot>();
        typeof(PlayerShoot).GetField("shootRight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(sh2, false);
    }
}
