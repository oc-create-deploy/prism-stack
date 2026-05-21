using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(PieceBuilder))]
[RequireComponent(typeof(SpriteRenderer))]
public class DraggablePiece1 : MonoBehaviour
{
    public static event Action<DraggablePiece1> OnPiecePlaced;

    [Header("Drag Settings")]
    public float dragHeight = 0.5f;
    public int dragSortingOrder = 100;
    public float idleScale = 0.5f;
    public float dragScale = 1f;

    PieceBuilder builder;
    Camera cam;
    float zDistance;
    int originalSortingOrder;

    Vector3 offset;
    Vector3 spawnPosition;
    Vector3[] localOffsets;
    bool isPlaced;

    BlockLineClearer blockLineClearer;
    GridLayout grid;
    Tilemap piecesMap;

    void Awake()
    {
        blockLineClearer = FindObjectOfType<BlockLineClearer>();
        if (blockLineClearer == null)
            Debug.LogError("[DraggablePiece1] No s'ha trobat cap BlockLineClearer a l'escena.");
        grid = FindObjectOfType<GridLayout>();
        if (grid == null)
            Debug.LogError("[DraggablePiece1] No s'ha trobat cap GridLayout a l'escena.");
        piecesMap = FindObjectOfType<Tilemap>();
        if (piecesMap == null)
            Debug.LogError("[DraggablePiece1] No s'ha trobat cap Tilemap a l'escena.");
        builder = GetComponent<PieceBuilder>();
        cam = Camera.main;
        zDistance = Mathf.Abs(cam.transform.position.z - transform.position.z);

        originalSortingOrder = GetComponent<SpriteRenderer>().sortingOrder;
    }

    void Start()
    {
        spawnPosition = transform.position;
        builder.BuildPiece(idleScale);
        RecomputeLocalOffsets();
    }

    void RecomputeLocalOffsets()
    {
        int cnt = builder.blockOffsets.Count + 1;
        localOffsets = new Vector3[cnt];
        localOffsets[0] = Vector3.zero;
        for (int i = 0; i < builder.blockOffsets.Count; i++)
        {
            var off = builder.blockOffsets[i];
            localOffsets[i + 1] = new Vector3(
                off.x * builder.unitSize,
                off.y * builder.unitSize,
                0f
            );
        }
    }

    void OnMouseDown()
    {
        if (isPlaced) return;

        builder.BuildPiece(dragScale);
        RecomputeLocalOffsets();

        var sr = GetComponent<SpriteRenderer>();
        sr.sortingOrder = dragSortingOrder;
        foreach (var go in builder.spawnedBlocks)
            if (go.TryGetComponent<SpriteRenderer>(out var s))
                s.sortingOrder = dragSortingOrder;

        var mp = Input.mousePosition; mp.z = zDistance;
        var world = cam.ScreenToWorldPoint(mp);
        offset = transform.position - world;
    }

    void OnMouseDrag()
    {
        if (isPlaced) return;

        var mp = Input.mousePosition; mp.z = zDistance;
        var world = cam.ScreenToWorldPoint(mp);
        var np = world + offset + Vector3.up * dragHeight;
        transform.position = np;

        for (int i = 0; i < builder.spawnedBlocks.Count; i++)
            builder.spawnedBlocks[i].transform.position = np + localOffsets[i + 1];
    }

    void OnMouseUp()
    {
        if (isPlaced) return;

        var cells = new List<(int row, int col)>();
        for (int i = 0; i < builder.spawnedBlocks.Count; i++)
        {
            Vector3 wp = builder.spawnedBlocks[i].transform.position - Vector3.up * dragHeight;
            float fx = (wp.x - blockLineClearer.startX) / blockLineClearer.step;
            float fy = (blockLineClearer.startY - wp.y) / blockLineClearer.step;
            int col = Mathf.RoundToInt(fx);
            int row = Mathf.RoundToInt(fy);

            if (col < 0 || col >= blockLineClearer.cols ||
                row < 0 || row >= blockLineClearer.rows)
            {
                Debug.LogWarning($"[DraggablePiece1] Cell ({row},{col}) fora de la graella. Reset.");
                ResetPosition();
                return;
            }

            cells.Add((row, col));
        }

        foreach (var (row, col) in cells)
        {
            Vector3 center = new Vector3(
                blockLineClearer.startX + col * blockLineClearer.step,
                blockLineClearer.startY - row * blockLineClearer.step,
                0f
            );
            var hits = Physics2D.OverlapPointAll(center);
            foreach (var hit in hits)
            {
                if (!hit.CompareTag("Block")) continue;
                if (builder.spawnedBlocks.Contains(hit.gameObject)) continue;
                ResetPosition();
                return;
            }
        }

        for (int i = 0; i < builder.spawnedBlocks.Count; i++)
        {
            var (row, col) = cells[i];
            Vector3 center = new Vector3(
                blockLineClearer.startX + col * blockLineClearer.step,
                blockLineClearer.startY - row * blockLineClearer.step,
                0f
            );
            builder.spawnedBlocks[i].transform.position = center;
        }

        var (r0, c0) = cells[0];
        Vector3 firstCenter = new Vector3(
            blockLineClearer.startX + c0 * blockLineClearer.step,
            blockLineClearer.startY - r0 * blockLineClearer.step,
            0f
        );
        transform.position = firstCenter - localOffsets[1];

        ResetSorting();
        blockLineClearer.CheckAndClearLines();

        isPlaced = true;

        foreach (var (row, col) in cells)
        {
            Vector3 center = new Vector3(
                blockLineClearer.startX + col * blockLineClearer.step,
                blockLineClearer.startY - row * blockLineClearer.step,
                0f
            );

            Debug.Log($"[DraggablePiece1] Cercant colliders a {center} per a la cel·la ({row},{col})");
            var hits = Physics2D.OverlapCircleAll(center, 0.1f);
            Debug.Log($"[DraggablePiece1] Nombre de col·lisions trobat: {hits.Length}");

            foreach (var hit in hits)
            {
                Debug.Log($"[DraggablePiece1] Hit object: '{hit.name}', tag='{hit.tag}'");

                bool overlayFound = false;
                foreach (Transform t in hit.transform.GetComponentsInChildren<Transform>(true))
                {
                    if (!t.CompareTag("Overlay"))
                        continue;

                    overlayFound = true;
                    var sbp = t.GetComponent<SpriteByPoints>();
                    if (sbp != null)
                    {
                        sbp.SetNewSprite();
                        Debug.Log($"[DraggablePiece1] Overlay '{t.name}' TROBAT! Cridant SetNewSprite()");
                    }
                    else
                    {
                        Debug.LogWarning($"[DraggablePiece1] Overlay '{t.name}' SENSE SpriteByPoints!");
                    }
                }

                if (!overlayFound)
                {
                    Debug.Log($"[DraggablePiece1] No s'ha trobat cap child amb tag 'Overlay' en '{hit.name}'");
                }
            }
        }

        OnPiecePlaced?.Invoke(this);
        enabled = false;
    }

    void ResetPosition()
    {
        transform.position = spawnPosition;
        for (int i = 0; i < builder.spawnedBlocks.Count; i++)
            builder.spawnedBlocks[i].transform.position = spawnPosition + localOffsets[i + 1];

        builder.BuildPiece(idleScale);
        RecomputeLocalOffsets();
        ResetSorting();
    }

    void ResetSorting()
    {
        var sr = GetComponent<SpriteRenderer>();
        sr.sortingOrder = originalSortingOrder;
        foreach (var go in builder.spawnedBlocks)
            if (go.TryGetComponent<SpriteRenderer>(out var s))
                s.sortingOrder = originalSortingOrder;
    }
}