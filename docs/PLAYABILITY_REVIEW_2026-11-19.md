# Prism Stack release gate — 2026-11-19

## Evidence and identity
- Bundle com.play.blockpuzzle, existing App Store Connect app 6812284455.
- iPhone portrait; simulator checks only, no physical device available.
- Original review: blank unlabeled menu; required legal placement, simultaneous row/column clearing, no-fit loss, retry, saved high score.

## Repairs pending final native acceptance
- Readable play/help/high-score menu and safe-area layout.
- Native evidence 44511447146 identified missing TMP_Settings, missing font faces and PrismPresentation.Repair exceptions. Bundled licensed DejaVu Sans plus editor-generated serialized TMP assets/settings; scene text assigned before load. New export 44514135183 is the first build containing this repair.
- No-fit detection waits for deferred line destruction/new piece initialization, checks fresh triples, uses actual shape cells rather than a fictitious origin cell.
- Simulator-only read-only JSON recorder. Host Maestro CLI delivers real taps and drags; it never writes or replaces game state.

## Acceptance still required
1. Compile latest source, clean readable native menu, help, board and controls; no runtime exceptions.
2. Real legal and rejected touch placements, exact occupancy and score after single and simultaneous row/column clears.
3. No-fit loss, visible responsive retry/new game and preserved high score after cold relaunch.
4. Inspect native captures/video for safe areas, reachable pieces, clipping and readable labels.
5. Only then merge/push main, export device target, sign/upload an incremented build and verify Apple VALID separately from actual intended group/build membership and eligible beta state.

Not released. Initial export and native build success alone are not gameplay acceptance. Old export 44504084442 must not be used for final signoff.
