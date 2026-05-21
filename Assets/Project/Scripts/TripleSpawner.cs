using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class PrefabBlock
{
    public string blockName;
    public List<GameObject> prefabs = new List<GameObject>();
}

public class TripleSpawner : MonoBehaviour
{
    public AdsManager adsManager;
    public GameController gameController;

    [Header("Blocs de prefabs disponibles (configurables a l'Inspector)")]
    public List<PrefabBlock> prefabBlocks = new List<PrefabBlock>();

    [Header("Posicions per defecte d'spawn (local o món segons transform)")]
    public Vector3 positionA = new Vector3(5f, 0f, 0f);
    public Vector3 positionB = new Vector3(10f, 0f, 0f);
    public Vector3 positionC = new Vector3(15f, 0f, 0f);
    public bool useLocalPositions = true;

    [Header("Prefabs especials i les seves 3 posicions relatives (offsets) A, B, C")]
    public List<GameObject> prefabsEspecials = new List<GameObject>();
    public List<Vector3> posAEspecials = new List<Vector3>();
    public List<Vector3> posBEspecials = new List<Vector3>();
    public List<Vector3> posCEspecials = new List<Vector3>();

    private Dictionary<GameObject, Vector3[]> mapaEspecial = new Dictionary<GameObject, Vector3[]>();
    private List<DraggablePiece1> spawnInstances = new List<DraggablePiece1>();
    private BlockLineClearer blockLineClearer;

    void Awake()
    {
        int count = Mathf.Min(prefabsEspecials.Count,
                              posAEspecials.Count,
                              posBEspecials.Count,
                              posCEspecials.Count);
        for (int i = 0; i < count; i++)
        {
            mapaEspecial[prefabsEspecials[i]] = new Vector3[3] {
                posAEspecials[i],
                posBEspecials[i],
                posCEspecials[i]
            };
        }
    }

    void Start()
    {
        blockLineClearer = FindObjectOfType<BlockLineClearer>();
        DraggablePiece1.OnPiecePlaced += HandlePiecePlaced;
        SpawnThreeRandom();
    }

    void OnDestroy()
    {
        DraggablePiece1.OnPiecePlaced -= HandlePiecePlaced;
    }

    private void HandlePiecePlaced(DraggablePiece1 piece)
    {
        spawnInstances.Remove(piece);

        if (spawnInstances.Count > 0)
        {
            if (!AnyFit())
                EndGame();
        }
        else
        {
            SpawnThreeRandom();
        }
    }

    public void SpawnThreeRandom()
    {
        spawnInstances.Clear();

        var probeBlock = prefabBlocks
            .FirstOrDefault(pb =>
                string.Equals(pb.blockName, "BlockProbes", StringComparison.OrdinalIgnoreCase)
                && pb.prefabs != null && pb.prefabs.Count > 0);

        List<GameObject> chosen = new List<GameObject>();
        int target = 3;

        if (probeBlock != null)
        {
            var pool = new List<GameObject>(probeBlock.prefabs);
            int count = Mathf.Min(target, pool.Count);
            for (int i = 0; i < count; i++)
            {
                int idx = Random.Range(0, pool.Count);
                chosen.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
        }
        else
        {
            var nonEmpty = prefabBlocks
                .Where(pb => pb.prefabs != null && pb.prefabs.Count > 0)
                .ToList();

            if (nonEmpty.Count == 0)
                return;

            int maxTries = target * 10;
            for (int i = 0; i < target; i++)
            {
                GameObject pick = null;
                int tries = 0;

                while (tries < maxTries)
                {
                    tries++;
                    var block = nonEmpty[Random.Range(0, nonEmpty.Count)];
                    var candidate = block.prefabs[Random.Range(0, block.prefabs.Count)];
                    if (candidate != null && !chosen.Contains(candidate))
                    {
                        pick = candidate;
                        break;
                    }
                }

                if (pick == null)
                {
                    Debug.LogWarning(
                        $"[TripleSpawner] no s'ha trobat prefab nou per al slot {i} " +
                        $"després de {maxTries} intents.");
                    break;
                }

                chosen.Add(pick);
            }
        }

        if (chosen.Count > 0) spawnInstances.Add(SpawnAt(chosen[0], 0));
        if (chosen.Count > 1) spawnInstances.Add(SpawnAt(chosen[1], 1));
        if (chosen.Count > 2) spawnInstances.Add(SpawnAt(chosen[2], 2));
    }

    private DraggablePiece1 SpawnAt(GameObject prefab, int index)
    {
        if (prefab == null) return null;

        Vector3 basePos = (index == 0 ? positionA :
                           index == 1 ? positionB :
                                        positionC);

        Vector3 localPos;
        if (mapaEspecial.TryGetValue(prefab, out var offsets))
        {
            var offset = offsets[Mathf.Clamp(index, 0, 2)];
            localPos = basePos + offset;
        }
        else
        {
            localPos = basePos;
        }

        Vector3 worldPos = useLocalPositions
            ? transform.TransformPoint(localPos)
            : localPos;

        var inst = Instantiate(prefab, worldPos, Quaternion.identity);
        return inst.GetComponent<DraggablePiece1>();
    }

    private bool AnyFit()
    {
        int rows = blockLineClearer.rows;
        int cols = blockLineClearer.cols;
        float sx = blockLineClearer.startX;
        float sy = blockLineClearer.startY;
        float step = blockLineClearer.step;
        float rad = blockLineClearer.detectRadius;

        bool[,] occ = new bool[rows, cols];
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            var pos = new Vector2(sx + c * step, sy - r * step);
            foreach (var h in Physics2D.OverlapCircleAll(pos, rad))
                if (h.CompareTag("Block"))
                {
                    occ[r, c] = true;
                    break;
                }
        }

        foreach (var piece in spawnInstances)
        {
            var offs = piece.GetComponent<PieceBuilder>().blockOffsets;
            var all = new List<Vector2Int> { Vector2Int.zero };
            foreach (var o in offs)
                all.Add(new Vector2Int(Mathf.RoundToInt(o.x), Mathf.RoundToInt(o.y)));

            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                bool fits = true;
                foreach (var o in all)
                {
                    int rr = r - o.y, cc = c + o.x;
                    if (rr < 0 || rr >= rows ||
                        cc < 0 || cc >= cols ||
                        occ[rr, cc])
                    {
                        fits = false;
                        break;
                    }
                }
                if (fits) return true;
            }
        }
        return false;
    }

    private void EndGame()
    {
        Debug.Log("Game Over!");
        gameController.SelectPanel();
        adsManager.ShowInterstitial();
    }
}