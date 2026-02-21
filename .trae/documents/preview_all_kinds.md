# Plan: Implement Simultaneous Preview for All Kinds

## Goal
Implement a feature to preview all `PawnKindDef`s of a selected Faction simultaneously in a grid view, improving the UI clarity and usability.

## Proposed Changes

### 1. Refactor Utility Method
**File:** `FactionGearModification/FactionGearModification/UI/FactionGearEditor.cs`
- Add a new static method `GetFactionKinds(FactionDef factionDef)` to retrieve all unique `PawnKindDef`s associated with a faction.
- This centralizes logic currently duplicated in `KindListPanel` and `ApplyToAllKindsInFaction`.

### 2. Enhance Preview Window
**File:** `FactionGearModification/FactionGearModification/UI/FactionGearPreviewWindow.cs`
- **State Management**:
    - Add `List<PawnKindDef> allKinds` to store the list of kinds for multi-preview.
    - Add `bool showAll` flag to toggle between single and multi-preview modes.
    - Add `Dictionary<PawnKindDef, Pawn> previewPawns` to store generated pawns for each kind.
- **Constructors**:
    - Add a new constructor or modify existing one to accept `List<PawnKindDef>`.
- **Logic**:
    - Update `GeneratePreviewPawn` (rename to `GeneratePreviewPawns`?) to handle batch generation.
    - Implement error handling for batch generation (don't fail all if one fails).
- **UI (`DoWindowContents`)**:
    - Implement a Grid View for "All Kinds" mode:
        - Use a `ScrollView`.
        - Calculate columns (e.g., 4 columns) based on window width.
        - Draw a card for each kind:
            - Portrait (using `PortraitsCache`).
            - Label (Kind Name).
            - Optional: "Inspect" button to switch to single view.
    - Adjust window size:
        - Single View: Keep `450x650`.
        - Multi View: Increase to e.g., `1000x700`.

### 3. Add Entry Point
**File:** `FactionGearModification/FactionGearModification/UI/Panels/GearEditPanel.cs`
- Add a "Preview All" button next to the existing "Preview" button in the header.
- On click:
    - Retrieve current `FactionDef`.
    - Call `FactionGearEditor.GetFactionKinds` to get the list.
    - Open `FactionGearPreviewWindow` in "All Kinds" mode.

### 4. Cleanup (Optional)
**File:** `FactionGearModification/FactionGearModification/UI/Panels/KindListPanel.cs`
- Refactor `GetKindsToDraw` to use the new `FactionGearEditor.GetFactionKinds` helper.

## Verification
- Open the editor in-game.
- Select a faction.
- Click "Preview All".
- Verify that a larger window opens showing portraits for all unit types (e.g., Archer, Slasher, Boss).
- Verify "Preview" (Single) still works as before.
