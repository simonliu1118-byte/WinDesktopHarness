# Public Boundary

This repository is a generic Windows desktop UI/UX test harness.

## Purpose

It exists only to test visible desktop behavior such as window layout, file selection, drag and drop, progress, cancellation, settings, record-style views, synthetic result presentation, and document export.

## Hard boundary

Everything in this repository must remain safe to publish permanently.

Never copy, mirror, summarize, infer, or reproduce any material from a private project, including but not limited to:

- private source code or implementation details
- private project names or identifiers
- private documentation or comments
- algorithms, architectures, processing methods, data formats, protocols, or internal terminology
- production constants, fingerprints, hashes, keys, identifiers, model names, dependency choices, or configuration values
- private API names, class names, function names, field names, result schemas, or error text
- private test data, fixtures, measurements, benchmarks, logs, screenshots, or reports
- any detail that could reveal what a private product does internally or how it works

Do not use deletion of this repository as a security control. Assume every committed byte may remain public forever.

## Allowed content

Only domain-neutral Windows UI/UX test code and synthetic data are allowed. Backend behavior must be fake and generic. Use neutral names such as `ProcessFile`, `InspectFile`, `TaskResult`, or `MockBackend` when an interface is needed.

Synthetic results must not imitate a private implementation's real internal structure. They may represent only generic states such as:

- ready
- processing
- completed
- cancelled
- warning
- failed

Document-export tests may use fabricated names, timestamps, file names, statuses, and explanatory text. They must not reproduce private report fields or private evidentiary/technical wording.

## Working rule for future sessions

Before adding or changing any file, ask:

> Would this file still be harmless if it were indexed, cloned, forked, quoted, and kept online forever?

If the answer is not an unambiguous yes, do not put it in this repository. Keep that work in the relevant private repository instead.

When context is missing, do not guess private behavior. Continue with synthetic placeholders only.
