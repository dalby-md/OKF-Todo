# Task export redesign options

Historical design record. The concept images were removed from the repository
on 11 August 2026; the text below preserves the options and decision. The images
remain available in Git history before commit `4f94caa`. For the current product,
see the [screenshot gallery](../screenshots.md) and [user guide](../help/using-okf-todo.md).

These concepts were generated on 2026-08-03 to rethink export field selection, column order, and row sorting.

## Option 1 — Ordered canvas

A single ordered field list makes column position and optional recipe sorting visible in one compact surface.

## Option 2 — Export runway

A horizontal sequence emphasizes the exported table from left to right and adds a larger live preview.

## Option 3 — Export recipe

A two-pane composer separates the field library from the selected export recipe. The recipe is the authoritative column order. **Sort by recipe** explicitly opts into using that same sequence as a composite row sort.

## Decision

Option 3 is selected for implementation. It scales to the complete field set, keeps adding and removing fields obvious, supports drag and keyboard reordering, and explains the relationship between column order and row sorting without making that relationship implicit.
