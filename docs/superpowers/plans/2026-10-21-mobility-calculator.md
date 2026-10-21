# Mobility Calculator Implementation Plan

**Goal:** Implement the approved mobility calculator in the existing Lab2 Unity project.
**Architecture:** Pure C# catalog, signed model and RK4 solver; runtime UI Toolkit screens and custom Unity charts; editor commands for scene setup, verification and builds.
**Tech Stack:** Unity 6000.2.14f1, C#, UI Toolkit, no third-party packages.
**Spec:** ../../MobilityModel.md

## Global constraints
- Use the 16 approved variables, 16x16 A and 16x6 B matrices.
- Editable polynomial functions per nonzero edge, six editable disturbances, initial values and positive normalization scales.
- No hidden clamping. Validate finite values and computational limits.
- All screens and charts run inside Unity; WebGL is the online build target.

## Review focus
- Invalid numbers and non-positive step/scale must produce a visible error.
- Matrix orientation is column-to-row, not the transpose.
- Final time and quarter-time samples remain correct when step does not divide the interval.
- Changed parameters must invalidate previous results rather than show stale charts.
- Negative/constant values and empty selections must render without invalid plot coordinates.

## Tasks
- [x] 1. Add failing standalone C# tests for analytic ODE solutions, step convergence, matrix orientation, disconnected factors, validation and interpolation. Implement catalog, model, polynomials, RK4 and results; run tests.
- [x] 2. Implement six UI Toolkit screens and line/radar charts; persist settings across tab changes, validate all entries, clear stale results and provide readable legends.
- [x] 3. Add dedicated scene, editor setup/build/verification commands and runtime smoke capture. Compile with installed Unity and inspect actual rendered screens.
- [x] 4. Document use and WebGL requirements, review implementation and run final tests/build.

## Execution notes
- User explicitly approved the model/screens and instructed implementation; proceed inline without another approval round.
- Lab2 is not a Git checkout. Work directly in the requested project; preserve SampleScene.
- WebGL module was initially absent; user installed it and requested a browser build.

## Verification record
- Core: 31 passing checks; red phase confirmed missing Mobility implementation before adding code.
- Unity: real editor compile and six-screen construction pass; invalid disabled-link regression passes.
- Windows: successful builds, zero build errors/warnings. GPU smoke renders all six pages; screenshots in docs/screenshots.
- Interaction: calculate button, end-time radar selection and disturbance scrolling verified through native app UI.
- Independent review: two P2 findings fixed (hover interpolation and stale invalid edge draft); follow-up found no additional P1/P2.
- Visual QA: fixed matrix header/cell alignment and radar height; moved hover text away from axis title.
- WebGL: build succeeded, 0 errors/warnings. Chrome loaded all six screens; calculation u6(T)=0.508274, dynamics, disturbances and end-time radar verified. Local HTTP server at 127.0.0.1:8765; no public deployment. Delivered page CSS fills the viewport.
- Existing editor remained open; compilation/build verification used the sibling UnityValidation copy.

