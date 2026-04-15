using UnityEngine;

[DisallowMultipleComponent]
public class CAMMetering : CAMMeteringBase
{
    protected override float ComputeMeteredLuminance(Color32[] pixels, int width, int height)
    {
        return ComputeLogAverageLuminance(pixels);
    }
}
