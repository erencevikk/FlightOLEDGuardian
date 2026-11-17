# Real-environment acceptance

Pending checks, not completed results:

1. Start MSFS 2020 and 2024 separately with official x64 SimConnect. Compare title, MSL/AGL, AP, speed and pause against the simulator.
2. Fly above 7,000 FT for 3 seconds. Idle 60 seconds. Confirm 530 normal seconds, 10 countdown seconds, 60 black seconds. Repeat; descend below hysteresis release altitude and confirm wake.
3. Test keyboard, mouse, every controller and both shortcut alternatives. Small joystick noise must not reset idle. Hotkey failure must leave protection suspended.
4. During black display pause/quit MSFS, disconnect SimConnect, reload a flight and change aircraft. Confirm overlay closes and no fabricated landing report appears.
5. Change resolution, DPI, primary monitor, unplug a monitor and suspend/resume Windows. Confirm safe recovery and selected-monitor coverage.
6. Compare planned TOD with route geometry and FMS. Test deviation, intersection and direct-to; reload changed plans. Confirm one amber/voice warning per plan.
7. Observe takeoff, landing, bounce, touch-and-go and go-around. Check statistics and no report merely from connecting in cruise.
8. Record OBS Game, Window and Display Capture separately. Inspect recordings during blackout; API success is not acceptance.
9. Test independent HDMI capture versus cloned OLED, including Mac OBS. Keep card protection disabled unless independent output is confirmed.
10. Opt into startup, sign out/in, launch selected simulator and verify one tray instance. Disable startup and verify Run entry removal. These are explicit user actions, not build steps.
11. Run an overnight flight and inspect memory/CPU/settings/logs. Accelerated scheduler testing does not substitute for real stability testing.
