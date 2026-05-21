using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class BlockLineClearer : MonoBehaviour
{
    public float detectRadius = 0.1f;
    public int rows = 8;
    public int cols = 8;
    public float startX = -6.1f;
    public float startY = 3.8f;
    public float step = 1.1f;

    [HideInInspector]
    public List<int> clearedLineColorCounts = new List<int>();

    [Header("GameController Reference")]
    public GameController gameController;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector3 worldPos = new Vector3(
                    startX + c * step,
                    startY - r * step,
                    0f
                );
                Gizmos.DrawWireSphere(worldPos, detectRadius);
            }
        }

        Vector3 topLeft = new Vector3(startX, startY, 0f);
        Vector3 topRight = new Vector3(startX + (cols - 1) * step, startY, 0f);
        Vector3 bottomLeft = new Vector3(startX, startY - (rows - 1) * step, 0f);
        Vector3 bottomRight = new Vector3(startX + (cols - 1) * step, startY - (rows - 1) * step, 0f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }

    public void CheckAndClearLines()
    {
        Vector2[,] grid = new Vector2[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                grid[r, c] = new Vector2(startX + c * step, startY - r * step);

        GameObject[,] blockGrid = new GameObject[rows, cols];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(grid[r, c], detectRadius);
                float minDistSqr = float.MaxValue;
                GameObject closest = null;
                foreach (var hit in hits)
                {
                    if (!hit.CompareTag("Block")) continue;
                    float dSqr = ((Vector2)hit.transform.position - grid[r, c]).sqrMagnitude;
                    if (dSqr < minDistSqr)
                    {
                        minDistSqr = dSqr;
                        closest = hit.gameObject;
                    }
                }
                blockGrid[r, c] = closest;
            }
        }
        var toDestroy = new HashSet<GameObject>();

        for (int r = 0; r < rows; r++)
        {
            bool fullRow = true;
            for (int c = 0; c < cols; c++)
                if (blockGrid[r, c] == null) { fullRow = false; break; }

            if (fullRow)
            {
                var colors = new HashSet<Color>();
                for (int c = 0; c < cols; c++)
                {
                    var sr = blockGrid[r, c].GetComponent<SpriteRenderer>();
                    if (sr != null) colors.Add(sr.color);
                }

                int distinctCount = colors.Count;
                clearedLineColorCounts.Add(distinctCount);

                int points = (cols + 1 - distinctCount) * 10;
                gameController?.IncrementValue(points);

                Debug.Log($"Fila {r} eliminada amb {distinctCount} colors. Punts: {points}");

                for (int c = 0; c < cols; c++)
                    toDestroy.Add(blockGrid[r, c]);
            }
        }

        for (int c = 0; c < cols; c++)
        {
            bool fullCol = true;
            for (int r = 0; r < rows; r++)
                if (blockGrid[r, c] == null) { fullCol = false; break; }

            if (fullCol)
            {
                var colors = new HashSet<Color>();
                for (int r = 0; r < rows; r++)
                {
                    var sr = blockGrid[r, c].GetComponent<SpriteRenderer>();
                    if (sr != null) colors.Add(sr.color);
                }

                int distinctCount = colors.Count;
                clearedLineColorCounts.Add(distinctCount);

                int points = (rows + 1 - distinctCount) * 10;
                gameController?.IncrementValue(points);

                Debug.Log($"Columna {c} eliminada amb {distinctCount} colors. Punts: {points}");

                for (int r = 0; r < rows; r++)
                    toDestroy.Add(blockGrid[r, c]);
            }
        }
        foreach (var blk in toDestroy)
            if (blk != null)
                Destroy(blk);
    }
}