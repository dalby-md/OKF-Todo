# Design QA — Task export recipe refinement

## Comparison target

- Source visual truth: `docs/images/design-options/task-export-option-3-export-recipe.png`
- Implementation screenshot: `docs/images/design-options/task-export-option-3-implementation.png`
- Combined comparison: `docs/images/design-options/task-export-option-3-design-qa-comparison.png`
- State: light theme, All lists, Active results, six-field recipe, Sort by recipe selected
- CSS viewport: 1354 × 1162 at device scale factor 1
- Source pixels: 1354 × 1162
- Implementation pixels: 1354 × 1162
- Density normalization: none required

## Findings

- No actionable P0, P1, or P2 differences remain.
- The larger implementation frame and taller preview are intentional refinements requested after selecting option 3. They preserve the source hierarchy while improving data inspection.
- The export-only unsaved-change banner has been removed. Export still saves open task changes before continuing, so the safety behavior remains without consuming dialog space.
- The preview now shows multiple realistic rows, has a sticky header, and independently scrolls vertically and horizontally.

## Required fidelity surfaces

- Fonts and typography: existing application type family, weights, hierarchy, truncation, and compact UI sizing remain consistent with the source direction and surrounding product.
- Spacing and layout rhythm: the two-pane grid, numbered recipe rows, row-order control, preview, and footer remain aligned. The wider frame and reclaimed vertical space give the preview materially better proportions without clipping actions.
- Colors and visual tokens: established teal, neutral surfaces, borders, focus states, and semantic action colors are preserved in light and dark theme rules.
- Image and asset quality: this interface contains no raster imagery requiring recreation. Existing Fluent icon assets remain sharp and consistent.
- Copy and content: labels remain concise and product-specific. Removed banner copy no longer competes with the preview.

## Full-view evidence

The combined image shows the saved option and refined implementation at equal dimensions. The implementation retains the defining field-library/recipe split, prominent sort-mode switch, six ordered rows, per-field direction controls, and preview. The requested increase in modal and preview size is clearly visible, and footer actions remain persistently available.

No focused crop was needed because the equal-size full-view comparison keeps all relevant controls and table text readable at original resolution.

## Comparison history

- Earlier P2: preview was shallow and showed only one row; the unsaved-change notice consumed valuable vertical space.
- Fix: removed the notice, increased the desktop dialog from 1040px to 1220px maximum width, increased composer height, allocated a dedicated preview grid track, rendered up to 50 rows, and added independent overflow with a sticky header.
- Post-fix evidence: `task-export-option-3-implementation.png` shows four real preview rows and substantial remaining scroll space; the browser test verifies modal width, preview height, multiple rows, and `overflow-y: auto`.

## Interaction verification

- Focused browser UI test covers adding, removing, and moving fields; choosing recipe sorting; changing direction; persisted reopening; multi-row preview rendering; expanded layout; and scrollable overflow.
- JavaScript syntax check and release build pass.
- Canonical Help is copied unchanged into normal application output.

final result: passed
