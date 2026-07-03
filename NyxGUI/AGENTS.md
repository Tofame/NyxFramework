# AGENTS.md — NyxGUI

This project is a standalone, decoupled widget tree and layout system (decoupled from direct OpenGL rendering).

## Architecture & Responsibilities

- **Widget Tree**: Controls like panels, buttons, scroll areas, and mini-windows are managed via `NyxElement` trees.
- **Declarative Parser**: Can initialize widget layouts from `.nyxui` XML definition templates.
- **Layout & Anchoring**: Anchors use absolute screen coordinates, resolved in correct topological order when using sibling constraints.

## Key Conventions & Guidelines

- **No OpenGL dependency**: Widgets paint abstractly through `INyxGuiPainter`.
- **Absolute Coordinates**: Store coordinates in screen-absolute values. Coordinate offsets are computed by moving children when parent bounds shift.
- **Topological Layout Resolution**: Sibling-dependent programmatic widgets require `NyxLayoutResolver.RelayoutContainer` to guarantee correct anchor computation order.
- Refer to [.agent/skills/nyxgui_anchoring.skill](file:///d:/ReposCSharp/NyxFramework/.agent/skills/nyxgui_anchoring.skill) for detailed programmatic layout constraints and pitfalls.
