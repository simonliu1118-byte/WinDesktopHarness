# Windows Desktop Harness — Final Acceptance Checklist

This checklist is only for validating the generic Windows UI harness. It contains no private implementation information.

## Normal mode

Double-click `WinDesktopHarness.exe`.

Check:

- three top-level tabs can be switched repeatedly without freezing
- resize the window between the minimum size and a larger desktop size
- controls do not overlap or clip
- keyboard Tab order remains reasonable
- Settings: Enter saves and Esc cancels
- output modes can be switched among source-adjacent, fixed folder, and ask once per batch

## Acceptance-data mode

Double-click `Start-Acceptance-Mode.cmd`.

This starts the same executable with synthetic record data loaded only for UI testing.

Check both record tabs:

- scrolling remains responsive with 500 synthetic rows per record type
- zebra striping remains aligned
- long Traditional Chinese file names are ellipsized rather than overlapping columns
- keyword, date, and status/result filters can be combined
- clearing filters restores the full list
- double-clicking a real data row opens details
- blank zebra rows cannot behave like real records
- repeated tab switching remains responsive

## Sample files

The `sample-files` folder contains empty placeholder files for the synthetic workflow. File contents are intentionally irrelevant to this harness.

Suggested checks:

1. Select several normal files and confirm progress advances one item at a time.
2. Include a file with `fail` in its name and confirm a failure row is shown.
3. Cancel a multi-file job part way through and confirm completed rows remain.
4. Use a normal file for inspection and confirm the green success state.
5. Use the `fail` inspection sample and confirm the red failure state.
6. Confirm the PDF button is disabled before an inspection result and enabled after a result.
7. Confirm the long Chinese file name remains readable or safely truncated.

## DPI / display scaling

If practical, repeat a visual pass at Windows display scaling 100%, 125%, and 150%.

The harness is accepted when no important control is clipped, tabs remain usable, record lists remain readable, and the two main workflows remain visually clear.
