# First-run decision sheet design QA

## Target

- Selected direction: Product Design option 2, a centered decision sheet with two explanatory paths and a quiet Help/Skip footer.
- Reference: `C:\Users\soere\.codex\generated_images\019fb46b-5383-7770-8f13-bf56e3ea9da2\exec-7e5cbe1e-69fa-4d0b-a1f6-eccb3c268f83.png`
- Comparison sheet: `C:\Users\soere\.codex\visualizations\2026\07\30\019fb46b-5383-7770-8f13-bf56e3ea9da2\first-run-option-2-comparison.png`
- Captured viewport: 1600 x 1000 CSS pixels.

## QA passes

### Pass 1

- P1: An obsolete `#first-run-sample-button` flex rule overrode the new grid layout, centering the sample title and separating it from the primary choice alignment.
- P2: Dark-mode capture occurred during the 140 ms theme transition, making both decision rows look washed out.

Fixes applied:

- Removed the obsolete ID-specific flex layout so both choices use the same decision-row grid.
- Added theme-settled checks before dark-mode visual capture and locked the semantic teal/gold surfaces with dark-theme-qualified selectors.

### Pass 2

- Modal is centered and blocks the underlying workspace.
- Heading, choice rows, reassurance, Help note, and Skip action follow the selected visual hierarchy.
- Both decision rows align consistently and retain clear keyboard focus.
- Light mode matches the selected off-white, teal, and warm-gold direction.
- Dark mode uses layered graphite surfaces with teal and gold semantic accents and readable text.
- No clipping, overflow, broken spacing, or mismatched radii is visible at the captured viewport.
- Focus, busy state, Skip behavior, and sample-data workflow remain covered by the focused UI test.

## Final result

Passed. No open P0, P1, or P2 visual issues.
