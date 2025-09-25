using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour, IDeflectableProjectile
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
        AlignVisualByDirX(direction.x >= 0 ? +1 : -1);

        currentGridPosition = spawnGridPos;

        int mid = tileGrid.gridWidth / 2;
        ownerSide = (spawnGridPos.x < mid) ? TileGrid.Side.Left : TileGrid.Side.Right;
    }

    private void Update()
    {
        if (isDestroying) return;

        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        // Cek pindah tile -> hit check
        int gridXNow = tileGrid.GetGridPosition(transform.position).x;
        if (gridXNow != currentGridPosition.x)
        {
            currentGridPosition.x = gridXNow;
            TryHitOpponentOnThisTile(currentGridPosition);
        }

        // Hitung sisi kiri-kanan area (ukuran tile = 1; GetWorldPosition memberi sudut kiri–bawah tile)
        float leftEdgeX = tileGrid.GetWorldPosition(new Vector2Int(0, currentGridPosition.y)).x;                          // tepi kiri area
        float rightEdgeX = tileGrid.GetWorldPosition(new Vector2Int(tileGrid.gridWidth - 1, currentGridPosition.y)).x + 1f; // tepi kanan area

        // Pakai perbandingan strict supaya benar2 "sudah lewat"
        if (direction.x > 0 && transform.position.x > rightEdgeX) DestroyBullet();
        if (direction.x < 0 && transform.position.x < leftEdgeX) DestroyBullet();
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

        var parry = opponent.GetComponent<ParryController>()
        ?? opponent.GetComponentInChildren<ParryController>(true)
        ?? opponent.GetComponentInParent<ParryController>();

        if (parry != null && parry.IsParryActive)
        {
            if (parry.TryDeflect(gameObject))
            {
                Debug.Log("[Bullet] DEFLECT triggered — cancel damage");
                return;
            }
        }

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
    private void AlignVisualByDirX(int dirSign)
    {
        var sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>(true);
        if (sr) { sr.flipX = dirSign < 0; return; }

        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (dirSign >= 0 ? 1f : -1f);
        transform.localScale = s;
    }

    public void DeflectTo(GameObject newOwner, float damageMult, float speedMult, int reflectCount)
    {
        // BLOCK: jangan bisa dideflect kalau sudah mulai destroy
        if (isDestroying) return;

        if (!newOwner || tileGrid == null) return;

        owner = newOwner;

        // hitung side pemilik baru
        var ownerPos = tileGrid.GetGridPosition(newOwner.transform.position);
        int mid = tileGrid.gridWidth / 2;
        ownerSide = (ownerPos.x < mid) ? TileGrid.Side.Left : TileGrid.Side.Right;

        // balik arah & scale damage/kecepatan
        direction = new Vector2(-direction.x, direction.y).normalized;
        damage = Mathf.CeilToInt(damage * damageMult);
        speed *= speedMult;

        // sesuaikan rotasi visual
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        AlignVisualByDirX(direction.x >= 0 ? +1 : -1);

        Debug.Log($"[Bullet] DEFLECT by {owner.name}: dmg x{damageMult}, spd x{speedMult}, count={reflectCount}");
    }
}
