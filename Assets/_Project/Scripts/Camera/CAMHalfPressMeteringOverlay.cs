using UnityEngine;

[DisallowMultipleComponent]
public class CAMHalfPressMeteringOverlay : MonoBehaviour
{
    private struct DisplayZone
    {
        public Rect Rect;
        public float Weight;
        public bool IsFocusZone;
    }

    [Header("References")]
    [SerializeField] private CAMPhotoCapture targetCapture;

    [Header("Overlay")]
    [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color focusZoneColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private float activeFrameWidthRatio = 0.72f;
    [SerializeField] private float activeFrameHeightRatio = 0.5f;
    [SerializeField] private float lineThickness = 1.25f;
    [SerializeField] private float boxWidth = 28f;
    [SerializeField] private float boxHeight = 20f;
    [SerializeField] private float activeBoxScale = 1.2f;
    [SerializeField] private float cornerLength = 6f;
    [SerializeField] private int maxDisplayedZones = 9;
    [SerializeField] private float minimumDisplayedWeight = 0.18f;
    [SerializeField] private bool drawCenterCross = true;
    [SerializeField] private float crossSize = 8f;
    [SerializeField] private bool drawMeteringCircle = true;
    [SerializeField] private float meteringCircleRadius = 22f;
    [SerializeField] private float meteringCircleThickness = 1.25f;
    [SerializeField] private bool drawOuterGuideFrame = false;

    private Texture2D lineTexture;

    private void Reset()
    {
        targetCapture = FindObjectOfType<CAMPhotoCapture>();
    }

    private void OnValidate()
    {
        activeFrameWidthRatio = Mathf.Clamp(activeFrameWidthRatio, 0.2f, 0.98f);
        activeFrameHeightRatio = Mathf.Clamp(activeFrameHeightRatio, 0.2f, 0.98f);
        lineThickness = Mathf.Max(1f, lineThickness);
        boxWidth = Mathf.Max(8f, boxWidth);
        boxHeight = Mathf.Max(8f, boxHeight);
        activeBoxScale = Mathf.Max(1f, activeBoxScale);
        cornerLength = Mathf.Max(2f, cornerLength);
        maxDisplayedZones = Mathf.Clamp(maxDisplayedZones, 1, 32);
        minimumDisplayedWeight = Mathf.Clamp01(minimumDisplayedWeight);
        crossSize = Mathf.Max(2f, crossSize);
        meteringCircleRadius = Mathf.Max(4f, meteringCircleRadius);
        meteringCircleThickness = Mathf.Max(1f, meteringCircleThickness);
    }

    private void OnGUI()
    {
        if (!targetCapture || !targetCapture.IsHalfPressActive)
        {
            return;
        }

        EnsureLineTexture();
        Color previousColor = GUI.color;

        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        float frameWidth = Screen.width * activeFrameWidthRatio;
        float frameHeight = Screen.height * activeFrameHeightRatio;
        Rect frameRect = new Rect(
            center.x - frameWidth * 0.5f,
            center.y - frameHeight * 0.5f,
            frameWidth,
            frameHeight);

        CAMEvaluativeMetering evaluativeMetering = targetCapture.Metering as CAMEvaluativeMetering;
        if (evaluativeMetering && evaluativeMetering.LastZoneDisplayData != null && evaluativeMetering.LastZoneDisplayData.Length > 0)
        {
            DrawEvaluativeHud(frameRect, evaluativeMetering);
        }
        else
        {
            DrawFallbackHud(frameRect);
        }

        if (drawOuterGuideFrame)
        {
            DrawFrame(frameRect, new Color(lineColor.r, lineColor.g, lineColor.b, 0.35f), lineThickness);
        }

        if (drawMeteringCircle)
        {
            DrawMeteringCircle(center, meteringCircleRadius, meteringCircleThickness, new Color(lineColor.r, lineColor.g, lineColor.b, 0.85f));
        }

        if (drawCenterCross)
        {
            GUI.color = new Color(lineColor.r, lineColor.g, lineColor.b, 0.85f);
            DrawLine(center.x - crossSize, center.y, crossSize * 2f, lineThickness);
            DrawLine(center.x, center.y - crossSize, lineThickness, crossSize * 2f);
        }

        GUI.color = previousColor;
    }

    private void DrawFallbackHud(Rect frameRect)
    {
        Vector2 center = frameRect.center;
        DrawBracketBox(new Rect(center.x - boxWidth * 0.5f, center.y - boxHeight * 0.5f, boxWidth, boxHeight), lineColor, lineThickness);
    }

    private void DrawEvaluativeHud(Rect frameRect, CAMEvaluativeMetering evaluativeMetering)
    {
        CAMEvaluativeMetering.ZoneDisplayData[] zones = evaluativeMetering.LastZoneDisplayData;
        float maxWeight = 0.01f;

        for (int i = 0; i < zones.Length; i++)
        {
            maxWeight = Mathf.Max(maxWeight, zones[i].Weight);
        }

        int columns = evaluativeMetering.ZoneColumns;
        int rows = evaluativeMetering.ZoneRows;
        float cellWidth = frameRect.width / columns;
        float cellHeight = frameRect.height / rows;
        DisplayZone[] displayZones = new DisplayZone[zones.Length];
        int displayCount = 0;

        for (int i = 0; i < zones.Length; i++)
        {
            CAMEvaluativeMetering.ZoneDisplayData zone = zones[i];
            float normalizedWeight = Mathf.Clamp01(zone.Weight / maxWeight);
            if (normalizedWeight < minimumDisplayedWeight && !zone.IsFocusZone)
            {
                continue;
            }

            Vector2 center = new Vector2(
                frameRect.xMin + (zone.Column + 0.5f) * cellWidth,
                frameRect.yMin + (zone.Row + 0.5f) * cellHeight);
            float scale = zone.IsFocusZone ? activeBoxScale : Mathf.Lerp(0.9f, 1.1f, normalizedWeight);
            Rect rect = new Rect(
                center.x - boxWidth * scale * 0.5f,
                center.y - boxHeight * scale * 0.5f,
                boxWidth * scale,
                boxHeight * scale);

            displayZones[displayCount++] = new DisplayZone
            {
                Rect = rect,
                Weight = normalizedWeight,
                IsFocusZone = zone.IsFocusZone
            };
        }

        SortDisplayZones(displayZones, displayCount);
        int shown = Mathf.Min(displayCount, maxDisplayedZones);

        for (int i = 0; i < shown; i++)
        {
            DisplayZone zone = displayZones[i];
            float alpha = Mathf.Lerp(0.22f, 0.95f, zone.Weight);
            float thickness = Mathf.Lerp(lineThickness, lineThickness * 1.9f, zone.Weight);
            Color color = zone.IsFocusZone
                ? new Color(focusZoneColor.r, focusZoneColor.g, focusZoneColor.b, Mathf.Clamp01(alpha + 0.1f))
                : new Color(lineColor.r, lineColor.g, lineColor.b, alpha);
            DrawBracketBox(zone.Rect, color, thickness);
        }
    }

    private void DrawBracketBox(Rect rect, Color color, float thickness)
    {
        float clampedCorner = Mathf.Min(cornerLength, rect.width * 0.5f, rect.height * 0.5f);
        Color previousColor = GUI.color;
        GUI.color = color;

        DrawLine(rect.xMin, rect.yMin, clampedCorner, thickness);
        DrawLine(rect.xMin, rect.yMin, thickness, clampedCorner);

        DrawLine(rect.xMax - clampedCorner, rect.yMin, clampedCorner, thickness);
        DrawLine(rect.xMax - thickness, rect.yMin, thickness, clampedCorner);

        DrawLine(rect.xMin, rect.yMax - thickness, clampedCorner, thickness);
        DrawLine(rect.xMin, rect.yMax - clampedCorner, thickness, clampedCorner);

        DrawLine(rect.xMax - clampedCorner, rect.yMax - thickness, clampedCorner, thickness);
        DrawLine(rect.xMax - thickness, rect.yMax - clampedCorner, thickness, clampedCorner);

        GUI.color = previousColor;
    }

    private void DrawFrame(Rect rect, Color color, float thickness)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        DrawLine(rect.xMin, rect.yMin, rect.width, thickness);
        DrawLine(rect.xMin, rect.yMax - thickness, rect.width, thickness);
        DrawLine(rect.xMin, rect.yMin, thickness, rect.height);
        DrawLine(rect.xMax - thickness, rect.yMin, thickness, rect.height);
        GUI.color = previousColor;
    }

    private void DrawMeteringCircle(Vector2 center, float radius, float thickness, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        DrawLine(center.x - radius, center.y - radius, radius * 2f, thickness);
        DrawLine(center.x - radius, center.y + radius - thickness, radius * 2f, thickness);
        DrawLine(center.x - radius, center.y - radius, thickness, radius * 2f);
        DrawLine(center.x + radius - thickness, center.y - radius, thickness, radius * 2f);
        GUI.color = previousColor;
    }

    private static void SortDisplayZones(DisplayZone[] zones, int count)
    {
        for (int i = 0; i < count - 1; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                if (zones[j].Weight > zones[i].Weight || (zones[j].IsFocusZone && !zones[i].IsFocusZone))
                {
                    DisplayZone temp = zones[i];
                    zones[i] = zones[j];
                    zones[j] = temp;
                }
            }
        }
    }

    private void DrawLine(float x, float y, float width, float height)
    {
        GUI.DrawTexture(new Rect(x, y, width, height), lineTexture);
    }

    private void EnsureLineTexture()
    {
        if (lineTexture)
        {
            return;
        }

        lineTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        lineTexture.SetPixel(0, 0, Color.white);
        lineTexture.Apply(false, true);
    }

    private void OnDestroy()
    {
        if (lineTexture)
        {
            Destroy(lineTexture);
        }
    }
}
