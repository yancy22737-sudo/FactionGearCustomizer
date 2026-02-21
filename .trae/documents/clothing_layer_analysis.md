# Clothing Layer Functionality Analysis & Plan

## 1. Current State Analysis

### 1.1 Logic (FactionGearManager.cs)
The current categorization logic splits items into `Helmets`, `Armors`, `Apparel`, and `Others`.
- **Helmets**: `Overhead` layer.
- **Armors**: `Shell` layer OR High Armor Rating (> 0.4).
- **Apparel**: (`OnSkin` OR `Middle`) AND NOT `Overhead` AND NOT `Belt` AND Low Armor Rating (< 0.4).
- **Others**: `Belt` layer.

**Identified Issues:**
1.  **Categorization Overlap**: An item that is both `Shell` AND `OnSkin`/`Middle` (e.g., a full-body jumpsuit or specialized suit) with low armor (< 0.4) will appear in **both** `Armors` (due to `Shell` layer) and `Apparel` (due to `OnSkin`/`Middle` + low armor).
    - *Fix*: Logic should be mutually exclusive. If an item is categorized as Armor, it should be excluded from Apparel.

### 1.2 UI (GearEditPanel.cs)
The UI includes a `DrawLayerPreview` method that visualizes coverage.
- **Rows**: Head, Torso, Shoulders, Arms, Hands, Legs, Feet.
- **Columns**: Skin, Mid, Shell, Belt, Over.

**Identified Issues:**
1.  **Performance/Optimization**: `DrawLayerPreview` calls `DefDatabase<BodyPartGroupDef>.GetNamedSilentFail(...)` inside the drawing loop (every frame). This is inefficient.
    - *Fix*: Cache BodyPartGroupDefs in a static list or fields.
2.  **Inconsistency**: Uses `ApparelLayerDefOf.Belt` in Manager but `DefDatabase<ApparelLayerDef>.GetNamedSilentFail("Belt")` in UI.
    - *Fix*: Use `ApparelLayerDefOf.Belt` consistently.
3.  **Feature Gap**: The Layer Preview is only visible in "Simple Mode". "Advanced Mode" users lose this useful visualization.
    - *Proposal*: Consider exposing it in Advanced Mode or a shared "Preview" tab/window.
4.  **Hardcoded Layout**: The UI manually calculates rects and offsets.

## 2. Implementation Plan

### Step 1: Fix Categorization Logic
- Modify `FactionGearManager.EnsureCacheInitialized` to ensure `cachedAllApparel` strictly excludes items already caught by `cachedAllArmors`.
- Logic change: `Apparel` = (`OnSkin` OR `Middle`) AND NOT `Overhead` AND NOT `Belt` AND NOT `Shell` AND Low Armor Rating.
- *Wait*, if I exclude `Shell`, then `OnSkin`+`Shell` items only go to `Armors`. This seems correct as `Shell` usually implies outer/armor layer.

### Step 2: Optimize UI Performance
- In `GearEditPanel`, create static cached fields for `BodyPartGroupDef`s used in `DrawLayerPreview`.
    - `groupTorso`, `groupShoulders`, `groupArms`, `groupLegs`, `groupFeet`, etc.
    - Initialize them once (lazy load or static constructor).

### Step 3: Standardize Definitions
- Replace `GetNamedSilentFail("Belt")` with `ApparelLayerDefOf.Belt`.

### Step 4: UI Cleanup (Optional but recommended)
- The user asked "if UI is correct". The current matrix is "correct" for vanilla layers.
- "Any redundant logic and ui": The duplicated calls to `GetNamed` are redundant processing.
- The `DrawCompactLayerCell` creates `new Rect` and `new Color` repeatedly. Minor, but can be cleaned up.

## 3. Checklist
- [ ] Refine `FactionGearManager.cs` categorization logic.
- [ ] Add caching for BodyPartGroups in `GearEditPanel.cs`.
- [ ] Fix `ApparelLayerDefOf` usage in `GearEditPanel.cs`.
- [ ] Verify `DrawLayerPreview` logic matches the new categorization (visual consistency).
