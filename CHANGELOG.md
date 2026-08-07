# Changelog

All notable changes to the Celestia Unity package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/).

## [0.1.0] — Unreleased

### Added

- `WorldClock` — day progress in the 0..1 range with self or external ticking,
  adjustable time scale, pause and resume, and per second, minute, hour and day
  events. Boundary events survive large time steps without skipping hours.
- `TimeOfDay` — serializable time struct with progress conversion and comparison.
- `CelestialSolver` — sun and moon altitude and azimuth from latitude, year
  progress and moon phase. Includes atmospheric air mass, horizon fade, sky
  phase and shadow length helpers.
- `CelestialHandler` — samples the solver from the clock and publishes
  `CelestialState`.
- `CelestialPreset` — ScriptableObject holding latitude, year progress, moon
  phase, light intensities and colour gradients, plus post processing curves.
- `CelestialLightBinder` — drives two directional lights, hands shadow casting
  between them with hysteresis, fades near the horizon and keeps
  `RenderSettings.sun` on the active body.
- `CelestialPostProcessBinder` — drives White Balance and Color Adjustments from
  sky phase. Works on a runtime copy of the volume profile so the source asset
  is never modified. Compiles only when the Universal Render Pipeline is present.
- Scene gizmos showing the sky dome, sun and moon arcs, cardinal directions and
  live altitude readouts.
- `GameObject > Celestia > Sky Rig` menu item that builds the full hierarchy.
- Edit mode tests covering the solver, clock and time struct.
