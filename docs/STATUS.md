# Development status — 16 November 2026

> Historical v0.1 status below. For the current release see [V0.2.md](V0.2.md) and the updated root README. v0.2 adds TR/EN UI, native EXE.xml startup, online SimBrief import, dark dropdowns, mock numbers and local native-client discovery. Live simulator acceptance remains pending.

## Verified here

- Six application/library projects and two executable test projects.
- Release build with .NET 8.0.301: zero warnings and errors.
- 17 core scenarios: altitude/idle/hysteresis, periodic/full/adaptive, countdown, skip/snooze, wake, capture gating, stale/disconnected/paused data, IFR/VFR, TOD alert deduplication/route ambiguity, touchdown/go-around, exact report black duration and 48 hours of simulated scheduling.
- 15 integration scenarios: settings persistence/recovery, capture gating, startup command quoting, named mutex, XML import/DTD rejection, missing TOD, report persistence, SimConnect missing-DLL fallback/binary telemetry parsing, three PNG sizes, WPF and pure-black/hidden overlays.
- Actual WPF application startup, dispatcher and dashboard rendering through smoke-test entry point.
- Dashboard and square report images visually reviewed; contrast and clipping issues corrected.

## Implemented; real-environment acceptance pending

- SimConnect connect/reconnect, subscriptions and telemetry. No official SDK DLL or running MSFS available for live validation.
- Keyboard/mouse/WinMM joystick activity, global emergency shortcut, monitor positioning and display/power recovery.
- Tray and opt-in current-user Run-key watcher. No startup registry write performed; sign-in/MSFS launch not exercised.
- Capture-exclusion request and fail-open policy. No OBS/capture-card compatibility confirmed.
- SAPI speech. No listening test or installed Turkish voice verification performed.

## Scope boundaries

Phase 1 code is present and mock-tested; **Phase 1 is not a hardware-validated release**. Phase 2 and part of Phase 3 are implemented: moving telemetry, planned-route TOD, 50 NM notification, IFR/VFR/custom settings, adaptive protection, touchdown report/history, three basic achievements and report cards.

Remaining: .NET 10 SDK migration, online SimBrief API, aircraft FMS TOD, automatic flight-plan/direct-to updates, multiple named profiles, advanced history charts/achievement animation, full Turkish/English UI localization, installer/updater/signing, live stability/capture tests and X-Plane 12 provider. No placeholder X-Plane provider or invented TOD is presented as supported.

The UI is English with optional Turkish speech. Reports use independent JSON files rather than SQLite. X-Plane can implement `IFlightSimulatorProvider` without changing the scheduler, TOD service or recorder.

## Implementation references

- [Microsoft SimConnect API](https://docs.flightsimulator.com/html/Programming_Tools/SimConnect/SimConnect_API_Reference.htm)
- [SIMCONNECT_RECV_SIMOBJECT_DATA layout](https://docs.flightsimulator.com/html/Programming_Tools/SimConnect/API_Reference/Structures_And_Enumerations/SIMCONNECT_RECV_SIMOBJECT_DATA.htm)
- [Microsoft SetWindowDisplayAffinity](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity)

The SDK DLL is not redistributed. No GitHub publication, paid service, startup registration or external app reconfiguration was performed.
