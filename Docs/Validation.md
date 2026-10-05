# Verification

Verified in Unity 6000.2.8f1 on Windows on October 4, 2026.

- The standalone C# build produced `Assets/Plugins/Avoider/Avoider.dll` successfully using Unity's bundled compiler and .NET Standard 2.1 reference assembly.
- Unity loaded the Avoider component from the `Avoider` assembly. The Unity project does not compile the plug-in source a second time.
- Automated sampler checks passed across 12 seeds: bounds, minimum pairwise distance, useful sample count, and invalid-spacing rejection.
- Play-mode checks passed: both actors on the saved NavMesh, visible versus occluded points, a hidden destination with a complete path, and minimum path length among reachable candidates.
- Timed movement checks passed: the Avoider moves into cover, faces the player, and moves again when the player is relocated around cover.
- Edge cases passed: out of range stops navigation, no cover clears the destination, and missing-agent, missing-target, and off-mesh configurations are rejected with warnings.
- The game screenshot was visually inspected after replacing immediate-mode lines with a URP-compatible mesh overlay. The HUD, red/green samples, range circle, actors, and cover render correctly.
- The Windows standalone player build succeeded with zero build errors (`Builds/Windows/AvoiderShowcase.exe`).

Reproduce the checks with **Avoider → Run Validation**. The raw reports are written to `Artifacts/sampler-tests.txt` and `Artifacts/play-tests.txt`; local reports are excluded from source control. Intentional invalid-agent tests can also produce a Unity NavMesh placement error alongside the expected plug-in warnings.

No Visual Studio or .NET SDK was found in the standard installation locations available to this session. The included Visual Studio solution targets .NET Standard 2.1 and references the installed Unity modules; its source was compiled using the included PowerShell fallback. Open the solution in Visual Studio to demonstrate that build workflow for the assignment video.
