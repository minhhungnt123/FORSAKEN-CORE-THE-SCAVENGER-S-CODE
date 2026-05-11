using UnityEngine;

public class FarmGridManager : MonoBehaviour
{
    [Header("Grid N x N Settings")]
    [SerializeField] private int gridSizeN = 5;
    [SerializeField] private float plotSize = 2.0f;
    [SerializeField] private float spacing = 0.1f;
    [SerializeField] private float yOffset = 0.05f;

    [Header("Ground Auto-Scale (Trapezoid Setup)")]
    [Tooltip("Khoảng cách viền lề tính từ mép ô đất đến mép của mặt trên bãi đất")]
    [SerializeField] private float groundPadding = 0.5f;
    [Range(0.1f, 1f)]
    [SerializeField] private float topSurfaceRatio = 0.8f;
    [SerializeField] private bool uniformScale = true;

    [Header("References")]
    [SerializeField] private GameObject groundBackdropPrefab;
    [SerializeField] private SoilPlot soilPlotPrefab;

    private SoilPlot[,] gardenGrid;

    // Properties tính toán dùng chung, tránh lặp code (DRY)
    private float CellSize => plotSize + spacing;
    private float TotalGridWidth => (gridSizeN * plotSize) + ((gridSizeN - 1) * spacing);

    private void Start()
    {
        SetupGroundBackdrop();
        GenerateNxNGrid();
    }

    private void SetupGroundBackdrop()
    {
        if (groundBackdropPrefab == null) return;

        GameObject ground = Instantiate(groundBackdropPrefab, transform.position, Quaternion.identity, transform);
        Renderer groundRenderer = ground.GetComponentInChildren<Renderer>();

        if (groundRenderer != null)
        {
            ground.transform.localScale = Vector3.one;
            Vector3 baseSize = groundRenderer.bounds.size;

            float targetTopSize = TotalGridWidth + (groundPadding * 2);
            float requiredBaseSize = targetTopSize / topSurfaceRatio;

            if (baseSize.x > 0 && baseSize.z > 0)
            {
                float scaleX = requiredBaseSize / baseSize.x;
                float scaleZ = requiredBaseSize / baseSize.z;

                if (uniformScale)
                {
                    float finalScale = Mathf.Max(scaleX, scaleZ);
                    ground.transform.localScale = new Vector3(finalScale, finalScale, finalScale);
                }
                else
                {
                    ground.transform.localScale = new Vector3(scaleX, ground.transform.localScale.y, scaleZ);
                }
            }
        }
    }

    private void GenerateNxNGrid()
    {
        if (soilPlotPrefab == null) return;

        gardenGrid = new SoilPlot[gridSizeN, gridSizeN];
        Vector3 startPos = CalculateBottomLeftStartPoint();

        for (int x = 0; x < gridSizeN; x++)
        {
            for (int z = 0; z < gridSizeN; z++)
            {
                Vector3 spawnPos = new Vector3(
                    startPos.x + (x * CellSize),
                    transform.position.y + yOffset,
                    startPos.z + (z * CellSize)
                );

                SoilPlot newPlot = Instantiate(soilPlotPrefab, spawnPos, Quaternion.identity, transform);
                newPlot.Init(x, z);
                gardenGrid[x, z] = newPlot;
            }
        }
    }

    private Vector3 CalculateBottomLeftStartPoint()
    {
        float offset = (gridSizeN * CellSize) / 2f - (CellSize / 2f);
        return transform.position - new Vector3(offset, 0, offset);
    }

    private void OnDrawGizmos()
    {
        Vector3 startPos = CalculateBottomLeftStartPoint();

        Gizmos.color = new Color(0.1f, 0.5f, 0.8f, 0.8f);
        for (int x = 0; x < gridSizeN; x++)
        {
            for (int z = 0; z < gridSizeN; z++)
            {
                Vector3 pos = new Vector3(
                    startPos.x + (x * CellSize),
                    transform.position.y + yOffset,
                    startPos.z + (z * CellSize)
                );
                Gizmos.DrawWireCube(pos, new Vector3(plotSize, 0.1f, plotSize));
            }
        }

        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
        float targetTopSize = TotalGridWidth + (groundPadding * 2);
        Gizmos.DrawWireCube(transform.position + new Vector3(0, yOffset - 0.01f, 0), new Vector3(targetTopSize, 0.05f, targetTopSize));
    }
}