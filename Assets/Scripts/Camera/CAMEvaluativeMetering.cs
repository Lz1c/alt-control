using UnityEngine;

[DisallowMultipleComponent]
public class CAMEvaluativeMetering : CAMMeteringBase
{
    public struct ZoneDisplayData
    {
        public int Column;
        public int Row;
        public float Weight;
        public bool IsFocusZone;
    }

    [Header("Evaluative")]
    [SerializeField] private CAMFocusController focusController;
    [SerializeField] private int zoneColumns = 8;
    [SerializeField] private int zoneRows = 5;
    [SerializeField] private float centerWeight = 1.35f;
    [SerializeField] private float contrastWeight = 0.8f;
    [SerializeField] private float focusZoneBoost = 2.2f;
    [SerializeField] private float highlightProtection = 0.35f;

    private ZoneDisplayData[] lastZoneDisplayData;

    public int ZoneColumns => zoneColumns;
    public int ZoneRows => zoneRows;
    public ZoneDisplayData[] LastZoneDisplayData => lastZoneDisplayData;

    protected override void Reset()
    {
        base.Reset();
        focusController = GetComponent<CAMFocusController>();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        zoneColumns = Mathf.Clamp(zoneColumns, 2, 16);
        zoneRows = Mathf.Clamp(zoneRows, 2, 12);
        centerWeight = Mathf.Max(0f, centerWeight);
        contrastWeight = Mathf.Max(0f, contrastWeight);
        focusZoneBoost = Mathf.Max(1f, focusZoneBoost);
        highlightProtection = Mathf.Clamp01(highlightProtection);
    }

    protected override float ComputeMeteredLuminance(Color32[] pixels, int width, int height)
    {
        if (!focusController)
        {
            focusController = GetComponent<CAMFocusController>();
        }

        int zoneWidth = Mathf.Max(1, width / zoneColumns);
        int zoneHeight = Mathf.Max(1, height / zoneRows);
        Vector2 focusViewport = GetFocusViewport(targetCamera, focusController);
        lastZoneDisplayData = new ZoneDisplayData[zoneColumns * zoneRows];

        float weightedLogSum = 0f;
        float totalWeight = 0f;

        for (int row = 0; row < zoneRows; row++)
        {
            for (int column = 0; column < zoneColumns; column++)
            {
                int startX = column * zoneWidth;
                int startY = row * zoneHeight;
                int endX = column == zoneColumns - 1 ? width : Mathf.Min(width, startX + zoneWidth);
                int endY = row == zoneRows - 1 ? height : Mathf.Min(height, startY + zoneHeight);

                ZoneStats zone = ComputeZoneStats(pixels, width, startX, startY, endX, endY);
                Vector2 zoneCenter = new Vector2((column + 0.5f) / zoneColumns, (row + 0.5f) / zoneRows);
                float centerBias = 1f + centerWeight * (1f - Vector2.Distance(zoneCenter, new Vector2(0.5f, 0.5f)) / 0.7071f);
                float contrastBias = 1f + zone.contrast * contrastWeight;
                float highlightBias = Mathf.Lerp(1f, 0.65f, zone.highlightRatio * highlightProtection);
                bool isFocusZone = IsInsideFocusZone(zoneCenter, focusViewport);
                float focusBias = isFocusZone ? focusZoneBoost : 1f;
                float weight = Mathf.Max(0.01f, centerBias * contrastBias * highlightBias * focusBias);

                lastZoneDisplayData[row * zoneColumns + column] = new ZoneDisplayData
                {
                    Column = column,
                    Row = row,
                    Weight = weight,
                    IsFocusZone = isFocusZone
                };

                weightedLogSum += Mathf.Log(Mathf.Max(0.0001f, zone.luminance)) * weight;
                totalWeight += weight;
            }
        }

        return Mathf.Exp(weightedLogSum / Mathf.Max(0.0001f, totalWeight));
    }

    private static Vector2 GetFocusViewport(Camera camera, CAMFocusController focus)
    {
        if (!camera || !focus || !focus.HasFocusLock)
        {
            return new Vector2(0.5f, 0.5f);
        }

        Vector3 viewport = camera.WorldToViewportPoint(focus.FocusPoint);
        return new Vector2(Mathf.Clamp01(viewport.x), Mathf.Clamp01(viewport.y));
    }

    private bool IsInsideFocusZone(Vector2 zoneCenter, Vector2 focusViewport)
    {
        float xThreshold = 0.5f / zoneColumns;
        float yThreshold = 0.5f / zoneRows;
        return Mathf.Abs(zoneCenter.x - focusViewport.x) <= xThreshold
            && Mathf.Abs(zoneCenter.y - focusViewport.y) <= yThreshold;
    }

    private static ZoneStats ComputeZoneStats(Color32[] pixels, int width, int startX, int startY, int endX, int endY)
    {
        float luminanceSum = 0f;
        float contrastSum = 0f;
        float highlightCount = 0f;
        int count = 0;

        for (int y = startY; y < endY; y++)
        {
            int rowOffset = y * width;

            for (int x = startX; x < endX; x++)
            {
                float luminance = ComputeLuminance(pixels[rowOffset + x]);
                luminanceSum += luminance;

                if (x + 1 < endX)
                {
                    float neighbor = ComputeLuminance(pixels[rowOffset + x + 1]);
                    contrastSum += Mathf.Abs(luminance - neighbor);
                }

                if (y + 1 < endY)
                {
                    float neighbor = ComputeLuminance(pixels[rowOffset + x + width]);
                    contrastSum += Mathf.Abs(luminance - neighbor);
                }

                if (luminance > 0.8f)
                {
                    highlightCount += 1f;
                }

                count++;
            }
        }

        float averageLuminance = luminanceSum / Mathf.Max(1, count);
        float averageContrast = contrastSum / Mathf.Max(1, count);
        float highlightRatio = highlightCount / Mathf.Max(1, count);
        return new ZoneStats
        {
            luminance = averageLuminance,
            contrast = averageContrast,
            highlightRatio = highlightRatio
        };
    }

    private struct ZoneStats
    {
        public float luminance;
        public float contrast;
        public float highlightRatio;
    }
}
