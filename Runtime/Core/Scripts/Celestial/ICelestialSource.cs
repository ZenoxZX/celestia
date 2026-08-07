using System;

namespace Celestia
{
    public interface ICelestialSource
    {
        CelestialState State { get; }

        CelestialPreset Preset { get; }

        event Action<CelestialState> StateChanged;

        CelestialState Evaluate(float dayProgress);
    }
}
