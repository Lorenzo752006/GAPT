using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerSlashAttack : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GridPlayerControllerTask6 playerController;

    [Header("Attack")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float cooldown = 0.25f;
    private float cooldownTimer;

    [Header("Slash Visual")]
    [SerializeField] private GameObject slashPrefab;
    [SerializeField] private float slashLifetime = 0.15f;

    [Header("Placement")]
    [SerializeField] private float halfTileOffset = 0.5f;

    void Awake()
    {
        if (!playerController)
            playerController = GetComponent<GridPlayerControllerTask6>();
    }

    void Update()
    {
        cooldownTimer -= Time.deltaTime;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.spaceKey.wasPressedThisFrame && cooldownTimer <= 0f)
        {
            cooldownTimer = cooldown;
            DoSlash();
        }
    }

    private void DoSlash()
    {
        if (!playerController) return;

        Vector2Int origin = playerController.GetGridPosition();
        Vector2Int f = playerController.FacingDirection;
        if (f == Vector2Int.zero) f = Vector2Int.right;

        Vector2Int left = new Vector2Int(-f.y, f.x);
        Vector2Int right = new Vector2Int(f.y, -f.x);

        SpawnSlashFX(origin, f);

        Vector2Int[] cells =
        {
            origin + f * 1,
            origin + f * 2,
            origin + f * 3,
            origin + f * 1 + left,
            origin + f * 1 + right
        };

        foreach (var c in cells)
        {
            if (!GridManager.Instance.IsInBounds(c.x, c.y))
                continue;

            DamageEnemiesInCell(c);
        }
    }

    private void SpawnSlashFX(Vector2Int originCell, Vector2Int facing)
    {
        if (!slashPrefab) return;

        Vector2Int slashCell = originCell + facing;
        Vector3 worldPos = GridManager.Instance.GridToWorld(slashCell.x, slashCell.y);

        worldPos += new Vector3(facing.x, facing.y, 0f) * halfTileOffset;

        Quaternion rot = RotationFromFacing(facing);

        GameObject fx = Instantiate(slashPrefab, worldPos, rot);
        Destroy(fx, slashLifetime);
    }

    private Quaternion RotationFromFacing(Vector2Int f)
    {
        if (f == Vector2Int.right) return Quaternion.Euler(0, 0, 0);
        if (f == Vector2Int.up) return Quaternion.Euler(0, 0, 90);
        if (f == Vector2Int.left) return Quaternion.Euler(0, 0, 180);
        if (f == Vector2Int.down) return Quaternion.Euler(0, 0, 270);
        return Quaternion.identity;
    }

    private void DamageEnemiesInCell(Vector2Int cell)
    {
        List<EnemyHealth> snapshot = new List<EnemyHealth>(EnemyHealth.Active);

        for (int i = 0; i < snapshot.Count; i++)
        {
            EnemyHealth eh = snapshot[i];
            if (!eh) continue;

            Vector2Int enemyCell = GridManager.Instance.WorldToGrid(eh.transform.position);
            if (enemyCell == cell)
                eh.TakeDamage(damage);
        }
    }
}