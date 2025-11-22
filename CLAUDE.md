# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Advanced Grid Tool for Cities: Skylines II** - A comprehensive grid-based road network generation tool that extends the game's built-in grid functionality with advanced procedural generation algorithms. Successfully compiled and following Advanced Line Tool patterns exactly.

## ✅ Build Status: FULLY FUNCTIONAL - UI AND TOOL WORKING! (0 Errors)

### Current Status
- ✅ **Mod loads in game** - Settings appear, keybindings work
- ✅ **UI module registers** - Console shows "Advanced Grid Tool UI module registrations completed"
- ✅ **Architecture complete** - Following Line Tool patterns exactly
- ✅ **Tool activation** - Ctrl+G toggles tool on/off successfully
- ✅ **UI Panel Working** - Shows grid options when tool is active (added section to children)
- ✅ **Grid generation** - Generating 25 points with 40 connections (preview working)
- ✅ **Grid application** - User can click to apply grid
- ⚠️ **Missing icons** - Mode icons return 404 (cosmetic issue only)
- ❌ **Road creation** - Not implemented (only overlay preview)

## Architecture - Following Advanced Line Tool Patterns

### Critical Implementation Patterns
- **Base Class**: Extends `ObjectToolBaseSystem` (NOT ToolBaseSystem)
- **System Registration**: Uses `updateSystem.UpdateAt<>()` directly (NOT World.DefaultGameObjectInjectionWorld)
- **UI Bindings**: Uses `GetterValueBinding` (NOT ValueBinding) for C# side
- **React UI**: HOC pattern extending `MouseToolOptions` component
- **Overlay Rendering**: Uses `OverlayRenderSystem.Buffer` instance methods with `Line3.Segment`
- **No Harmony**: Native tool implementation without any patching

### Build Commands
```bash
# Full Build (C# and UI)
dotnet build

# UI Development
cd UI && npm install     # First time only
cd UI && npm run build   # Build UI module
cd UI && npm run dev     # Development watch mode
```

## Current Implementation

### Core Systems

#### 1. AdvancedGridToolSystem.cs
- **Extends**: `ObjectToolBaseSystem`
- **Key Features**:
  - Tool activation via hotkey (G key default)
  - Grid preview generation with 6 modes
  - Overlay rendering using buffer instance methods
  - Input handling with ControlPoint (not RaycastHit)
  - State machine (Idle → SelectingStart → SelectingEnd → PreviewingGrid → Applying)
- **System References**: Uses inherited `m_ToolSystem`, `m_PrefabSystem`

#### 2. AdvancedGridToolUISystem.cs
- **Extends**: `UISystemBase`
- **Bindings**:
  ```csharp
  AddUpdateBinding(new GetterValueBinding<bool>(
      "AdvancedGridTool", "IsActive",
      () => _toolSystem.activeTool == _gridToolSystem
  ));
  ```
- **Triggers**: Mode selection, spacing adjustment, dimension controls

#### 3. AdvancedGridToolTooltipSystem.cs
- **Extends**: `TooltipSystemBase`
- **Features**: Dynamic tooltips, mode info, measurements, grid dimensions

#### 4. Mod.cs
- **Entry Point**: Implements `IMod`
- **Critical Pattern**:
  ```csharp
  public void OnLoad(UpdateSystem updateSystem)
  {
      // Direct updateSystem usage - NOT World.DefaultGameObjectInjectionWorld
      updateSystem.UpdateAt<AdvancedGridToolSystem>(SystemUpdatePhase.ToolUpdate);
      updateSystem.UpdateAt<AdvancedGridToolUISystem>(SystemUpdatePhase.UIUpdate);
      updateSystem.UpdateAt<AdvancedGridToolTooltipSystem>(SystemUpdatePhase.UITooltip);
  }
  ```

### UI Module (React/TypeScript) - ✅ IMPLEMENTED

#### UI Architecture Pattern (Following Line Tool)
- **Module Registration** (`UI/src/index.tsx`):
  ```typescript
  moduleRegistry.extend(
      "game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx",
      'MouseToolOptions',
      AdvancedGridToolOptionsComponent(moduleRegistry)
  );
  ```

#### UI Component Structure (`UI/src/mods/AdvancedGridToolOptions.tsx`)
- **Value Bindings**: Two-way sync with C# backend
  ```typescript
  export const spacing$ = bindValue<number>('AdvancedGridTool', 'Spacing');
  export const currentMode$ = bindValue<number>('AdvancedGridTool', 'CurrentMode');
  ```
- **Trigger Bindings**: Commands to C#
  ```typescript
  export function setModeStandard() { trigger("AdvancedGridTool", "SetModeStandard"); }
  export function increaseSpacing() { trigger("AdvancedGridTool", "IncreaseSpacing"); }
  ```
- **HOC Pattern**: Wraps existing MouseToolOptions
- **Dynamic Component Resolution**: Uses moduleRegistry to access vanilla components

### Grid Generation Modes

| Mode | Status | Implementation Notes |
|------|--------|---------------------|
| Standard Grid | ✅ Framework | Basic rectangular grid generation ready |
| Organic Grid | 🚧 Placeholder | Needs Voronoi generator + Perlin noise |
| Suburban Pattern | 🚧 Placeholder | Needs curvilinear streets + cul-de-sac algorithm |
| Hexagonal Grid | 🚧 Placeholder | Needs axial/cubic coordinate system |
| Curved Grid | 🚧 Placeholder | Needs Bezier-based parallel curve generation |
| Terrain Adaptive | 🚧 Placeholder | Needs height sampling + slope-based warping |

### Settings System & Keybindings
- **Grid Configuration**: Mode, spacing, dimensions
- **Placement Options**: Terrain snap, road snap, auto-zones, water avoidance, slope limits
- **Visual Settings**: Transparency, grid numbers, measurements, animation
- **Default Keybindings** (configurable in settings):
  - Tool activation: **Ctrl+G** (was G, user changed)
  - Mode change: **Shift+G** (was Tab, user changed)
  - Decrease spacing: **[**
  - Increase spacing: **]**

### UI Features (When Tool Active)
- **Mode Selection Row**: 6 buttons for grid patterns (Standard, Organic, Suburban, Hex, Curved, Terrain)
- **Spacing Controls**: Visual display with +/- buttons (shows "XX.X m")
- **Grid Dimensions**: Width and height controls with arrow buttons
- **Dynamic Sections**: Mode-specific options appear based on selected mode
- **Tooltips**: All controls have descriptive tooltips
- **Integration**: Appears in standard tool options panel alongside vanilla tools

## Key Dependencies
```xml
<Reference Include="Game" />
<Reference Include="Colossal.Core" />
<Reference Include="Colossal.Mathematics" />
<Reference Include="Unity.Entities" />
<Reference Include="Unity.Mathematics" />
<Reference Include="Unity.Burst" />
<Reference Include="UnityEngine.AudioModule" />
```

## Overlay Rendering Pattern (Critical)
```csharp
// CORRECT - Instance method on buffer
_overlayBuffer.DrawCircle(color1, color2, 0.5f, 0, new float2(0, 1), position, radius);
_overlayBuffer.DrawLine(color, new Line3.Segment(from, to), width);

// WRONG - Static method (will not compile)
OverlayRenderSystem.DrawCircle(ref _overlayBuffer, ...);  // ❌ NO!
```

## File Structure
```
AdvancedGridTool/
├── Code/
│   ├── Systems/
│   │   ├── AdvancedGridToolSystem.cs (ObjectToolBaseSystem)
│   │   ├── AdvancedGridToolUISystem.cs (UISystemBase)
│   │   └── AdvancedGridToolTooltipSystem.cs (TooltipSystemBase)
│   └── Components/
│       └── GridToolComponents.cs (IComponentData structs)
├── ModSettings/
│   ├── ModSettings.cs (Configuration + Keybindings)
│   └── LocaleEN.cs (Localization)
├── UI/
│   ├── src/
│   │   ├── index.tsx (Module registration)
│   │   └── mods/
│   │       └── AdvancedGridToolOptions.tsx (Tool panel component)
│   ├── package.json (Dependencies)
│   ├── tsconfig.json (TypeScript config)
│   ├── webpack.config.js (Build configuration)
│   └── mod.json (UI module metadata)
├── Mod.cs (IMod entry point)
└── AdvancedGridTool.csproj (Project file)
```

## Next Steps
1. **Test Tool Activation** - Press Ctrl+G in-game to verify tool enables and UI appears
2. **Implement Grid Algorithms** - Add actual procedural generation logic for each mode:
   - Organic: Voronoi + Perlin noise
   - Suburban: Curvilinear streets + cul-de-sacs
   - Hexagonal: Axial coordinate system
   - Curved: Bezier curve generation
   - Terrain Adaptive: Height sampling
3. **Network Creation** - Convert preview to actual road placement (currently only shows overlay)
4. **Custom Icons** - Create proper icons for grid modes (currently using placeholders)
5. **Performance** - Add Burst compilation for heavy grid calculations

## Common Pitfalls to Avoid

### C# Backend
- ❌ Don't use `World.DefaultGameObjectInjectionWorld`
- ❌ Don't extend `ToolBaseSystem`
- ❌ Don't use `ValueBinding` for UI bindings
- ❌ Don't call overlay methods as static
- ❌ Don't use `RaycastHit` with GetRaycastResult
- ✅ DO use `updateSystem` parameter directly
- ✅ DO extend `ObjectToolBaseSystem`
- ✅ DO use `GetterValueBinding` for C# UI bindings
- ✅ DO call overlay methods on buffer instance
- ✅ DO use `ControlPoint` with GetRaycastResult

### React UI Module
- ❌ Don't import `useLocalization` from `cs2/modding`
- ❌ Don't use direct component imports
- ❌ Don't forget the HOC pattern
- ✅ DO import `useLocalization` from `cs2/l10n`
- ✅ DO use moduleRegistry for component resolution
- ✅ DO follow the HOC pattern: `(moduleRegistry) => (Component) => {...}`
- ✅ DO use `bindValue` for two-way bindings
- ✅ DO use `trigger` for one-way commands

## Development Notes
- Successfully refactored from ToolBaseSystem to ObjectToolBaseSystem pattern
- All compilation errors resolved (was 13 errors, now 0)
- UI module implemented following Line Tool's HOC pattern
- React components properly integrated with game's UI system
- Following Advanced Line Tool implementation patterns exactly
- No Harmony patching required - pure native implementation
- UI logs show successful registration: "Advanced Grid Tool UI module registrations completed."
- User successfully changed keybindings in-game (confirms mod is loading)
- **FULLY WORKING as of 2025-11-22**: Tool activates via Ctrl+G, UI panel displays, grid generation functional

### Critical Fixes That Made It Work
1. **Prevented auto-activation**: Added `m_ToolSystem.activeTool == this` check in TrySetPrefab
2. **Fixed React component pattern**: Changed from `Component(props)` to `Component()` (no props)
3. **Corrected tooltip theme path**: Fixed from "components/tooltips" to "common/tooltip"
4. **Removed prop cloning**: Directly mutate `result.props.children?.push()` instead of cloning
5. **Get FocusDisabled from registry**: Don't create it manually, get from registry