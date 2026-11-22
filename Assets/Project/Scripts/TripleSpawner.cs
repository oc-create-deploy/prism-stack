using System;
using System.Collections;
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
    private bool fitCheckPending;
    private bool gameEnded;

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

        if (spawnInstances.Count == 0)
            SpawnThreeRandom();
        QueueFitCheck();
    }

    public void SpawnThreeRandom()
    {
        spawnInstances.Clear();

        var pool = prefabBlocks.Where(pb => pb.prefabs!=null)
            .SelectMany(pb=>pb.prefabs).Where(p=>p!=null).Distinct().ToList();
        int maxCells = PrismJewelWorld.Level<=2 ? 3 : PrismJewelWorld.Level<=4 ? 4 : PrismJewelWorld.Level<=6 ? 5 : 9;
        var curated = pool.Where(p=>p.GetComponent<PieceBuilder>()!=null && p.GetComponent<PieceBuilder>().blockOffsets.Count<=maxCells).ToList();
        if(curated.Count==0) curated=pool;
        var chosen = new List<GameObject>();
        for(int i=0;i<3 && curated.Count>0;i++) chosen.Add(curated[Random.Range(0,curated.Count)]);

        if (chosen.Count > 0) spawnInstances.Add(SpawnAt(chosen[0], 0));
        if (chosen.Count > 1) spawnInstances.Add(SpawnAt(chosen[1], 1));
        if (chosen.Count > 2) spawnInstances.Add(SpawnAt(chosen[2], 2));
        QueueFitCheck();
    }

    private void QueueFitCheck()
    {
        if (fitCheckPending || gameEnded) return;
        fitCheckPending = true;
        StartCoroutine(CheckFitAfterPlacement());
    }

    private IEnumerator CheckFitAfterPlacement()
    {
        // Cleared blocks use Destroy (end of frame), and fresh pieces initialize
        // in Start. Never judge the next move against that transient physics state.
        yield return null;
        fitCheckPending = false;
        Physics2D.SyncTransforms();
        if (!gameEnded && spawnInstances.Count > 0 && !AnyFit()) EndGame();
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
            // The root is a drag handle, not necessarily an occupied block.
            // Use exactly the rendered shape (some L shapes omit the origin).
            var all = offs;

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
        if (gameEnded) return;
        gameEnded = true;
        Debug.Log("Game Over!");
        gameController.SelectPanel();
        if (adsManager != null) adsManager.ShowInterstitial();
    }
}