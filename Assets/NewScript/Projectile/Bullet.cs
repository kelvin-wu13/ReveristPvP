using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private int damage;
    private TileGrid tileGrid;

    [SerializeField] private float fadeOutTime = 0.1f;
    [SerializeField] private GameObject hitEffectPrefab;

    private Vector2Int currentGridPosition;
    private bool isDestroying = false;

    private GameObject owner;
    private TileGrid.Side ownerSide;

    public void Initialize(Vector2 dir, float spd, int dmg, TileGrid grid, Vector2Int spawnGridPos, GameObject ownerGO)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        tileGrid = grid;
        owner = ownerGO;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        currentGridPosition = spawnGridPos;

        int mid = tileGrid.gridWidth / 2;
        ownerSide = (spawnGridPos.x < mid) ? TileGrid.Side.Left : TileGrid.Side.Right;
    }

    private void Update()
    {
        if (isDestroying) return;

        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        int gridXNow = tileGrid.GetGridPosition(transform.position).x;
        if (gridXNow != currentGridPosition.x)
        {
            currentGridPosition.x = gridXNow;

            TryHitOpponentOnThisTile(currentGridPosition);

            if (direction.x > 0 && currentGridPosition.x >= tileGrid.gridWidth - 1) DestroyBullet();
            if (direction.x < 0 && currentGridPosition.x <= 0) DestroyBullet();
        }
    }

    private void TryHitOpponentOnThisTile(Vector2Int gridPos)
    {
        if (tileGrid == null || !tileGrid.IsValidGridPosition(gridPos)) return;

        var players = FindObjectsOfType<PlayerMovement>();
        if (players == null || players.Length == 0) return;

        PlayerMovement opponent = null;
        foreach (var p in players)
        {
            if (p == null || p.gameObject == owner) continue;
            if (p.GetCurrentGridPosition() == gridPos)
            {
                opponent = p;
                break;
            }
        }

        if (opponent == null) return;

        var stat = opponent.GetComponent<PlayerStats>();
        var oppPos = opponent.GetCurrentGridPosition();

        int finalDamage = GridDamageCalculator.Calculate(new GridDamageCalculator.Ctx
        {
            grid = tileGrid,
            attackerSide = ownerSide,
            attackerPos = currentGridPosition,
            defenderPos = oppPos,
            baseDamage = damage
        });

        Debug.Log($"[Bullet] HIT {opponent.name} @ {oppPos} for {finalDamage} (base {damage})");

        if (stat != null) stat.TakeDamage(finalDamage, owner);
        HitEvents.NotifyOwnerHit(owner, opponent.gameObject, false, "Bullet");

        SpawnHitEffect(transform.position);
        DestroyBullet();
    }


    private void SpawnHitEffect(Vector3 pos)
    {
        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, pos, Quaternion.identity);
    }


    private void DestroyBullet()
    {
        if (isDestroying) return;
        isDestroying = true;

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = false;

        StartCoroutine(FadeOutAndDestroy());
    }

    private IEnumerator FadeOutAndDestroy()
    {
        float startAlpha = 1f;
        float elapsedTime = 0;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeOutTime);
            if (sr != null)
            {
                Color color = sr.color;
                color.a = alpha;
                sr.color = color;
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}
