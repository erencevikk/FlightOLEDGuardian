# FlightOLED Guardian development checkpoint

Updated: 17 November 2026.

## Current distribution update

GitHub README, release notes and repository description are English first, Turkish second. A separate self-contained Windows x64 variant now bundles .NET 8.0.34. Official NuGet runtime/host packs were downloaded and verified against NuGet catalog SHA-512 values. Standalone apphost smoke execution loaded `coreclr.dll` from its own publish directory with an unavailable global runtime root and passed all 8 UI checks. `build-standalone.ps1` and `package-standalone.ps1` reproduce this variant; original v0.3.0 assets are preserved.

Flightsim.to form: Other Applications, MSFS2024, version 0.3.0, AI assistance disclosed, comments enabled. English/Turkish description, 69 MB standalone ZIP and 3 actual labelled mock/test UI screenshots are staged. Submission is NOT published: a valid VirusTotal scan is required, and scanning/submission requires action-time acceptance of VirusTotal and Flightsim.to terms. No VirusTotal sample was submitted yet. Preserve the live form tab and finish only after the user confirms those actions.

Repository: https://github.com/erencevikk/FlightOLEDGuardian

Published build: https://github.com/erencevikk/FlightOLEDGuardian/releases/tag/v0.3.0

## Current state

- .NET 8 Windows WPF app, six projects; English/Turkish, independent first-launch language picker.
- Default 7,000 FT AGL, 60 seconds idle, 540 seconds normal (last 10 countdown), 60 seconds black. Skip, Snooze, manual wake and emergency hotkey.
- Native read-only SimConnect; mock telemetry and planned SimBrief TOD import.
- Streamer identity masking and capture compatibility gating.
- Per-user installer and portable package. Settings/reports stay outside the installation folder.
- Every-startup and manual GitHub latest stable release checks. Published latest response verified as v0.3.0, not draft/pre-release.
- Source files at the release tag matched all 51 corresponding local Git blob hashes. Published EXE/ZIP digests matched local SHA-256 checksums.

## Validation

`./build.ps1 -UiTest -Smoke -Publish`

21 core + 33 integration/WPF tests; 8 full-app UI checks; 4 installer extraction/render checks passed. `./package.ps1` rebuilds the installer and complete ZIP. Do not overwrite published release assets with different bytes; use a new version for the next build.

## Outstanding

- No valid live MSFS samples received in the last desktop check; the DLL loaded, but MSFS was not present in the visible window inventory. Actual flight cycling remains unverified.
- OBS/physical HDMI capture test remains unverified.
- Actual Windows account installation, upgrade and uninstall remain unverified; only workspace extraction/UI tests ran.
- Disable MSFS auto-launch before uninstalling; after moving a portable installation, save the enabled auto-launch setting to refresh EXE.xml.
- GitHub connector reads this repository but write calls returned 403. The initial source/release upload used the explicitly authorized browser session. Future connector write access may require adding this repository to the connection's repository selection. Do not read credentials or bypass access controls.

Detailed user review: [docs/V0.3-REVIEW.md](docs/V0.3-REVIEW.md). Its GitHub-blocked section describes the earlier, pre-publication review snapshot; this checkpoint supersedes that publishing status.

Keep user settings, SimBrief identities, flight reports, SDK binaries, build output and temporary test files out of source control. Live flight and capture tests must not be reported as passed based on mocks.
