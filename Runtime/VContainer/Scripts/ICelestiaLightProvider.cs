using UnityEngine;

namespace Celestia.VContainer
{
    public interface ICelestiaLightProvider
    {
        Light SunLight { get; }
        Light MoonLight { get; }
        bool OwnsLights { get; }
    }
}
