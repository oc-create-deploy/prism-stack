# Native acceptance — 19 November 2026

Runtime source cbd5209; simulator export 44572205581.
Native QA 44583224004 succeeded with real XCUITest gestures on iPhone simulator:
26 placements, eight line clears, simultaneous multi-line clearing, no-fit loss,
visible loss actions, New Game retry with empty board, persisted high score after
process relaunch, and fresh-round score reset. No model writes used by test.
Reviewed native menu, full-board gameplay, loss and retry captures. Board edges
and score fit portrait layout. No physical device available; no physical test claimed.

Harness uses three-second native swipes on shared macOS runner. Earlier shorter
swipes intermittently missed intended drops; exact cause remains unproven.
A successful slower-input run is not evidence that every fast gesture works.

TestFlight replacement still requires a device export, signed upload, Apple VALID,
and separate intended tester/group/build and eligible beta-state verification.
