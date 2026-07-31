# Dark theme design QA

## Comparison target

- Source visual truth: `C:\Users\soere\.codex\generated_images\019fb46b-5383-7770-8f13-bf56e3ea9da2\exec-d1ec9cbc-eac8-4210-baac-3216100a70b6.png`
- Implementation screenshot: `C:\git\Okf-Todo\implementation-dark-theme.png`
- Open-dropdown screenshot: `C:\git\Okf-Todo\implementation-dark-theme-dropdown.png`
- Side-by-side evidence: `C:\git\Okf-Todo\design-qa-comparison.png`
- Source pixels: 1586 x 1024
- Implementation pixels: 1942 x 1150
- Implementation viewport: maximized Photino WebView on a 1920-class Windows desktop at 150% display scaling
- Density normalization: the side-by-side image scales the source to 1239 x 775 and the implementation to 1309 x 775, preserving each aspect ratio
- State: dark scheme, Active view, sample task 21 selected; the focused-region capture opens the native Task type dropdown

## Full-view comparison

The implementation carries the selected direction's layered graphite hierarchy across the top bar, navigation rail, task queue, editor, controls, and scroll surfaces. Teal is consistently reserved for primary interaction and selection. Amber remains waiting/warning, red remains overdue/destructive, and green remains completion. The production layout retains the existing list switcher, export/select controls, deadline, and tags because these are functional product surfaces that the visual concept simplified.

## Focused-region comparison

The task editor and Task type dropdown were checked separately because they were the weakest surfaces in the previous theme. The final controls use graphite fills, teal focus treatment, off-white text, and a dark native option list. The Windows-native selected option uses the operating system's neutral selection row while the list is open; this is legible and limited to the transient native popup.

## Required fidelity surfaces

- Fonts and typography: Segoe UI and the user's persisted Small size remain authoritative. Weight and hierarchy match the design direction without changing stored content formatting.
- Spacing and layout rhythm: existing production proportions and responsive tracks are preserved. Panel separation now comes from tonal depth and subdued borders instead of stark black/white contrast.
- Colors and visual tokens: graphite, teal, semantic amber/red/green, muted blue-grey borders, and off-white text map to the selected concept.
- Image quality and asset fidelity: the target contains no product imagery. Existing Fluent icon-font assets are retained; no placeholder, CSS-drawn, or replacement imagery was introduced.
- Copy and content: the comparison uses the same task title and core sample content. Additional production fields remain intentionally visible.

## Comparison history

1. Initial desktop capture
   - Finding: P1 white native task controls broke the dark surface hierarchy; the selected task also had an overly bright focus outline.
   - Fix: added explicit WebView-native control paint, dark form surfaces, Fluent select chevrons, and a teal accessibility focus treatment.
   - Post-fix evidence: `implementation-dark-theme.png` shows dark task controls and a teal selected-task focus state.
2. Open-dropdown capture
   - Finding: P2 the closed select and option list needed to remain dark in the real Windows WebView, not only in stylesheet inspection.
   - Fix: removed native light select-face painting, added explicit dark option colors, and preserved a visible Fluent chevron.
   - Post-fix evidence: `implementation-dark-theme-dropdown.png` shows a dark select face and dark option list. The native selected row remains neutral grey and is accepted as platform behavior.

## Findings

No actionable P0, P1, or P2 findings remain.

### Follow-up polish

- P3: the Windows-native open dropdown uses a neutral grey selected row rather than the concept's teal row. Replacing that transient OS surface would require a custom select component and is not warranted for this visual-system change.
- P3: the implementation is slightly denser than the concept when the persisted interface size is Small. This correctly respects the existing user preference.

## Primary interactions checked

- Desktop app starts and loads the dark preference.
- Existing active task and task queue render without layout breakage.
- Task type native select opens and remains readable in dark mode.
- TinyMCE toolbar and editor body use dark surfaces.
- Top-bar primary, secondary, disabled, and destructive action treatments remain distinguishable.

final result: passed
