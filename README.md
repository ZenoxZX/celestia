# Celestia

Time of day simulation for Unity. A single clock drives physically derived sun
and moon positions, which in turn drive directional lights, shadow handover and
post processing.

![A full day cycle running in the editor](.github/celestia-preview.gif)

## Installation (Package Manager — Git URL)

1. Open **Window → Package Manager**.
2. Click **+** → **Add package from git URL...** and add Celestia:
   ```
   https://github.com/ZenoxZX/celestia.git
   ```

The repository root is the package root, so no `?path=` suffix is needed.

Or declare it directly in `Packages/manifest.json`:

```json
"com.zenoxzx.celestia": "https://github.com/ZenoxZX/celestia.git"
```

The package has no dependencies. Post processing support compiles itself in
when the Universal Render Pipeline is present and stays out of the build when
it is not, so nothing has to be installed first.

## Getting started

1. `GameObject > Celestia > Sky Rig` builds the hierarchy and wires every
   reference:

   ```
   Celestia          WorldClock, CelestialHandler, CelestialLightBinder
     Sun Light       Directional light
     Moon Light      Directional light
     Sky Volume      Volume, CelestialPostProcessBinder
   ```

2. `Assets > Create > Celestia > Celestial Preset` creates a preset. Assign it
   to the `CelestialHandler`.

3. Set the latitude, season and moon phase on the preset. Press play.

## How it works

`WorldClock` keeps the day as a `0..1` progress value and raises events as
seconds, minutes, hours and days roll over. It ticks itself or accepts an
external tick, and the speed is set by real seconds per day multiplied by a
time scale.

`CelestialHandler` listens to the clock, samples `CelestialSolver` with the
preset, and publishes a `CelestialState` containing both body directions,
altitudes, azimuths, moon illumination and sky phase.

Binders consume that state. They never compute anything themselves, so you can
add your own listener for UI, audio or gameplay without touching the core.

## Coordinates

`CelestialSolver` returns altitude above the horizon and azimuth measured
clockwise from north. `CelestialState.SunDirection` points at the body;
`SunLightForward` points the way the light travels, which is what a
`Light` transform needs.

Hours are local solar time, so noon is when the sun peaks. Longitude is stored
on the preset for reference but is not yet part of the calculation — that needs
a time zone and the equation of time.

## Seasons

Year progress is continuous so a full year can be simulated:

| Year progress | Season           | Sun declination |
| ------------- | ---------------- | --------------- |
| 0.00          | Spring equinox   | 0°              |
| 0.25          | Summer solstice  | +23.44°         |
| 0.50          | Autumn equinox   | 0°              |
| 0.75          | Winter solstice  | −23.44°         |

Both equinoxes share a declination of zero, so the sun follows an identical path
on each. The moon does not: its position depends on phase, so spring and autumn
diverge once the phase is anything other than full.

## Moon phase

Phase runs `0..1`: new moon at 0, first quarter at 0.25, full at 0.5, last
quarter at 0.75. Phase sets how far the moon sits from the sun, which is why a
full moon rises at sunset and peaks at midnight.

A useful consequence: a winter full moon climbs as high at midnight as a summer
sun does at noon, while a summer full moon stays low. Season choice changes
night shadows dramatically.

## Post processing

`CelestialPostProcessBinder` drives White Balance and Color Adjustments from
sky phase, where 0 is astronomical night, 0.167 is the horizon and 1 is the sun
at zenith. Curves and gradients live on the preset.

The binder copies the volume profile at runtime and restores the original when
disabled or destroyed, so the source asset is never written to and produces no
diff. Missing overrides are added to that copy automatically; turn off
`Add Missing Overrides` to drive only what the profile already declares.

This component lives in its own assembly and compiles only when URP is
installed. Without URP the rest of the package still works.

## Lights

`CelestialLightBinder` expects two directional lights. Only one casts shadows at
a time and the handover uses a hysteresis band so it cannot flicker at the
horizon. Intensity fades as a body approaches the horizon, and a light at zero
intensity is disabled outright so it costs no shadow work.

Moon intensity is scaled by illumination, so a new moon goes dark on its own.

## Scheduling

`CelestialScheduler` runs actions at a time of day, at a sky event, across a
range, or on an interval. In the inspector each entry pairs a trigger with a
`UnityEvent`, so anything reachable from there works — `SetActive`, `enabled`
flags, your own methods.

The same schedules can be built from code:

```csharp
var scheduler = GetComponent<CelestialScheduler>();

scheduler.At(19, 42, () => lamps.SetActive(true));
scheduler.On(SkyEvent.Sunrise, () => lamps.SetActive(false));
scheduler.Every(ScheduleInterval.EveryHour, ChimeBell);

scheduler.Between(
    new TimeOfDay(19, 0), new TimeOfDay(6, 0),
    entered: () => streetLights.SetActive(true),
    exited:  () => streetLights.SetActive(false));
```

Each call returns the schedule, so it can be adjusted or removed later:

```csharp
var curfew = scheduler.At(23, 0, LockGates);
curfew.Once = true;

scheduler.Find("19:42").Enabled = false;
scheduler.Remove(curfew);
```

Adding or removing schedules from inside a callback is safe.

Triggers are matched against the span of time covered since the previous frame,
not against the current instant, so nothing is missed when the clock runs fast.

`WorldClock` exposes a static `Active` property holding the first clock that
enabled itself. Leave a scheduler's clock field empty to use it. The events
themselves stay per-instance, so several clocks can coexist.

## Requirements

- Unity 6000.3 or newer
- Universal Render Pipeline — optional, only needed for post processing

## License

MIT — see [LICENSE.md](LICENSE.md).
