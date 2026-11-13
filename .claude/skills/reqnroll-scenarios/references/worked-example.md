# Worked example: library-holds

Repository: [davidsho/library-holds](https://github.com/davidsho/library-holds). Scenarios live in `tests/LibraryHolds.Scenarios/`, and its [README.md](https://github.com/davidsho/library-holds/blob/main/tests/LibraryHolds.Scenarios/README.md) describes the approach for the next engineer. If the repo is checked out locally, read the files directly; this page is a summary.

It is a small library holds API: members place holds on a title in a format, returned copies fill the longest-waiting matching hold, and a notification provider is asked to tell the member when they can collect it by.

## Systems named in the scenarios

- **The holds API**: the service under test, run as `Program.cs` builds it, against a real Postgres in a container.
- **The notification provider**: the only system outside the service, so the only stand-in (WireMock). The API sends to it through an outbox.

## Features

| Feature | Journey | App |
|---|---|---|
| `Features/HoldsForReturnedCopies.feature` | Hold placed through the API → copy returned through the API → copy goes on the hold shelf or back on the shelf → notification sent through the outbox to the provider | `LibraryHoldsApp` (one app; the service has no message consumers, so there is nothing to switch on per feature) |

## Files by layer

| Folder | Files |
|---|---|
| `Steps/` | `HoldSteps`, `ReturnSteps`, `NotificationSteps` |
| `Drivers/` | `HoldsDriver`, `ReturnsDriver`, `NotificationsDriver` |
| `Contexts/` | `HoldsContext`, `CopiesContext` |
| `Model/` | `PlacedHold`, `ReturnedCopy`, and `Contracts.cs` (the API's JSON, written out independently of the API's own types) |
| `Support/App/` | `LibraryHoldsApp`, `NotificationProviderStandIn`, `FeatureContextExtensions` |
| `Support/Randomised/` | `ScenarioRandom`, `RandomHoldDetails`, `RandomInstants` |
| `Support/` | `Hooks`, `StrictJson`, `ExpectedCollectBy`, `ScenarioStateExtensions` |

## Where it is simpler than this skill's templates

- **One app, not a base app plus one per feature.** The per-feature app exists to switch message consumers on and off. With no consumers, one app serves every feature.
- **The settle check only reads the outbox**, through an admin endpoint, because returns are handled inside their request and write their notification in the same transaction. A service that consumes messages also needs the consumer half of the settle check (`assets/Support/App/`).
- **Every scenario starts clean.** A `[BeforeScenario]` hook settles, then truncates the tables and clears what the stand-in received.

## Points worth copying

- **The feature file is the spec.** Stripped to its `Rule:` lines, it is the four rules agreed before any Gherkin was written.
- **`WaitUntilSettledAsync`** replaced every poll and sleep. "Nobody is told anything" is checked by settling, then reading what the stand-in received once.
- **`Require*` vs `Assert*`**: `HoldsDriver.RequireReadyForCollectionAsync` backs "Given Ana's hold is ready for collection", so a failure there reads "Precondition failed" and is never mistaken for the rule under test breaking.
- **Driver actions only act.** `ReturnsDriver.ReturnAsync` returns the copy and checks the response code. Whether it filled a hold is for the `Then` steps.
- **Whole-object comparison**: every `Assert*` builds the hold or copy it expects and compares it with what the API returned, so a field no step mentions is still checked.
- **Strict contracts**: responses and the messages the stand-in receives are read with `StrictJson`, so a field the API adds, drops or renames fails the read.
- **At-least-once**: `NotificationsDriver.AssertToldTheCopyIsReadyAsync` accepts a repeated message from the outbox and checks the first one.
- **Expected values are worked out independently**: `ExpectedCollectBy` uses NodaTime, not the API's own calculation.
- **Boundary rows**: the collect-by outline covers the weeks before the clocks change and just after midnight UK time, and its randomised twin aims at the same ranges.
- **Known defects**: the [experiment branch](https://github.com/davidsho/library-holds/tree/experiment/scenarios-from-code) shows two `@ignore` scenarios written for the correct behaviour, with a comment naming the cause.
- Generated `*.feature.cs` files are written to `obj/` and not committed.
