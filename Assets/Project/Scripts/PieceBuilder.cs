using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class PieceBuilder : MonoBehaviour
{
    public enum PieceType
    {
        _1_1,
        _2_1,
        _2_2,
        _3_1,
        _3_2,
        _4_1,
        _5_1,
        _5_2,
        _5_3,
        _5_4,
        _6_1,
        _6_2,
        _6_3,
        _6_4,
        _6_5,
        _6_6,
        _6_7,
        _6_8,
        _7_1,
        _8_1,
        _8_2,
        _9_1,
        _9_2,
        _9_3,
        _9_4,
        _10_1,
        _10_2,
        _11_1,
        _11_2
    }

    public PieceType pieceType = PieceType._1_1;
    public GameObject blockPrefab;
    public float unitSize = 1.1f;
    [HideInInspector] public List<Vector2Int> blockOffsets = new List<Vector2Int>();
    [HideInInspector] public List<GameObject> spawnedBlocks = new List<GameObject>();
    public float idleScale = 0.5f;
    public float dragScale = 1.2f;

    void Start()
    {
        BuildPiece(idleScale);
    }

    void OnValidate()
    {
        blockOffsets.Clear();

        switch (pieceType)
        {
            case PieceType._1_1:
                blockOffsets.Add(new Vector2Int(0, 0));
                break;
            case PieceType._2_1:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(1, 0));
                break;
            case PieceType._2_2:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(0, 1));
                break;
            case PieceType._3_1:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(1, 0));
                blockOffsets.Add(new Vector2Int(2, 0));
                break;
            case PieceType._3_2:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(0, 1));
                blockOffsets.Add(new Vector2Int(0, 2));
                break;
            case PieceType._7_1:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(0, 1));
                blockOffsets.Add(new Vector2Int(0, 2));
                blockOffsets.Add(new Vector2Int(1, 0));
                blockOffsets.Add(new Vector2Int(1, 1));
                blockOffsets.Add(new Vector2Int(1, 2));
                blockOffsets.Add(new Vector2Int(2, 0));
                blockOffsets.Add(new Vector2Int(2, 1));
                blockOffsets.Add(new Vector2Int(2, 2));
                break;
            case PieceType._4_1:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(1, 0));
                blockOffsets.Add(new Vector2Int(0, 1));
                blockOffsets.Add(new Vector2Int(1, 1));
                break;
            case PieceType._5_1:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(1, 0));
                blockOffsets.Add(new Vector2Int(0, 1));
                break;
            case PieceType._5_2:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(0, 1));
                blockOffsets.Add(new Vector2Int(1, 1));
                break;
            case PieceType._5_3:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(1, 0));
                blockOffsets.Add(new Vector2Int(1, 1));
                break;
            case PieceType._5_4:
                blockOffsets.Add(new Vector2Int(1, 0));
                blockOffsets.Add(new Vector2Int(1, 1));
                blockOffsets.Add(new Vector2Int(0, 1));
                break;
            case PieceType._6_1:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(0, 1));
                blockOffsets.Add(new Vector2Int(0, 2));
                blockOffsets.Add(new Vector2Int(1, 0));
                break;
            case PieceType._6_2:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(0, 1));
                blockOffsets.Add(new Vector2Int(0, 2));
                blockOffsets.Add(new Vector2Int(1, 2));
                break;
            case PieceType._6_3:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(0, 1));
                blockOffsets.Add(new Vector2Int(1, 0));
                blockOffsets.Add(new Vector2Int(2, 0));
                break;
            case PieceType._6_4:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(0, 1));
                blockOffsets.Add(new Vector2Int(1, 1));
                blockOffsets.Add(new Vector2Int(2, 1));
                break;
            case PieceType._6_5:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(1, 0));
                blockOffsets.Add(new Vector2Int(1, 1));
                blockOffsets.Add(new Vector2Int(1, 2));
                break;
            case PieceType._6_6:
                blockOffsets.Add(new Vector2Int(1, 0));
                blockOffsets.Add(new Vector2Int(1, 1));
                blockOffsets.Add(new Vector2Int(1, 2));
                blockOffsets.Add(new Vector2Int(0, 2));
                break;
            case PieceType._6_7:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(2, 1));
                blockOffsets.Add(new Vector2Int(1, 0));
                blockOffsets.Add(new Vector2Int(2, 0));
                break;
            case PieceType._6_8:
                blockOffsets.Add(new Vector2Int(2, 0));
                blockOffsets.Add(new Vector2Int(2, 1));
                blockOffsets.Add(new Vector2Int(1, 1));
                blockOffsets.Add(new Vector2Int(0, 1));
                break;
            case PieceType._8_1:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(0, 1));
                blockOffsets.Add(new Vector2Int(0, 2));
                blockOffsets.Add(new Vector2Int(1, 0));
                blockOffsets.Add(new Vector2Int(1, 1));
                blockOffsets.Add(new Vector2Int(1, 2));
                break;
            case PieceType._8_2:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(1, 0));
                blockOffsets.Add(new Vector2Int(2, 0));
                blockOffsets.Add(new Vector2Int(0, 1));
                blockOffsets.Add(new Vector2Int(1, 1));
                blockOffsets.Add(new Vector2Int(2, 1));
                break;
            case PieceType._9_1:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(0, 1));
                blockOffsets.Add(new Vector2Int(0, 2));
                blockOffsets.Add(new Vector2Int(1, 0));
                blockOffsets.Add(new Vector2Int(2, 0));
                break;
            case PieceType._9_2:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(0, 1));
                blockOffsets.Add(new Vector2Int(0, 2));
                blockOffsets.Add(new Vector2Int(1, 2));
                blockOffsets.Add(new Vector2Int(2, 2));
                break;
            case PieceType._9_3:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(2, 1));
                blockOffsets.Add(new Vector2Int(2, 2));
                blockOffsets.Add(new Vector2Int(1, 0));
                blockOffsets.Add(new Vector2Int(2, 0));
                break;
            case PieceType._9_4:
                blockOffsets.Add(new Vector2Int(2, 0));
                blockOffsets.Add(new Vector2Int(2, 1));
                blockOffsets.Add(new Vector2Int(2, 2));
                blockOffsets.Add(new Vector2Int(1, 2));
                blockOffsets.Add(new Vector2Int(0, 2));
                break;
            case PieceType._10_1:
                blockOffsets.Add(new Vector2Int(1, 0));
                blockOffsets.Add(new Vector2Int(0, 1));
                break;
            case PieceType._10_2:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(1, 1));
                break;
            case PieceType._11_1:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(1, 0));
                blockOffsets.Add(new Vector2Int(2, 0));
                blockOffsets.Add(new Vector2Int(3, 0));
                break;
            case PieceType._11_2:
                blockOffsets.Add(new Vector2Int(0, 0));
                blockOffsets.Add(new Vector2Int(0, 1));
                blockOffsets.Add(new Vector2Int(0, 2));
                blockOffsets.Add(new Vector2Int(0, 3));
                break;
        }
    }

    public void BuildPiece(float scaleFactor)
    {
        if (blockPrefab == null) return;

        foreach (var go in spawnedBlocks)
            if (go != null)
                DestroyImmediate(go);

        spawnedBlocks.Clear();

        Vector3 pivot = transform.position;
        float spacing = unitSize * scaleFactor;

        foreach (var off in blockOffsets)
        {
            Vector3 pos = pivot + new Vector3(off.x * spacing, off.y * spacing, 0f);
            var clone = Instantiate(blockPrefab, pos, Quaternion.identity);
            clone.transform.localScale = Vector3.one * scaleFactor;
            spawnedBlocks.Add(clone);
        }
    }
}