using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Overdraw : VolumeComponent, IPostProcessComponent
{
    public BoolParameter overdrawEnable = new BoolParameter(false);
    public bool IsActive()
    {
        return overdrawEnable.value;
    }

    public bool IsTileCompatible()
    {
        return false;
    }
}