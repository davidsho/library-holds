# Library holds

A small library holds API, with two kinds of test: xUnit unit tests for the logic that's local to one
class, and Reqnroll scenarios that test the whole journey through the real API. It's the companion to
the essay *Rules You Can Run*.

A member places a hold on a title in a format. When a copy is returned, the API decides whether it
fills a waiting hold. If it does, the copy goes on the hold shelf, the hold is ready for collection,
and an external notification provider is asked to tell the member. The rules:

1. A returned copy goes to the member who has waited longest for that title and format.
2. A copy only fills a hold for the same title in the same format. Cancelled holds, and holds that
   already have a copy waiting on the hold shelf, are skipped.
3. The member can collect until closing time, 20:00 UK time, on the 7th day after the copy reaches
   the hold shelf.
4. A copy that fills no hold goes back on the shelf and nobody is told anything.

The spec for these is [HoldsForReturnedCopies.feature](tests/LibraryHolds.Scenarios/Features/HoldsForReturnedCopies.feature).

The app and its tests were written by Claude. The four rules were agreed with it before any Gherkin
was written.

## Running

Needs the .NET 10 SDK, and Docker for the scenarios.

```bash
dotnet test
```

See the [scenarios README](tests/LibraryHolds.Scenarios/README.md) for running one feature, replaying a
randomised failure, and the conventions the scenarios follow.

## What each kind of test is for

**Unit tests** (`tests/LibraryHolds.Tests.Unit`, xUnit, Moq, FluentAssertions) cover logic that lives in
one place: the collect-by date arithmetic across clock changes, which hold the matcher chooses, and
request validation. They run in milliseconds and pinpoint the line that's wrong.

**Scenarios** (`tests/LibraryHolds.Scenarios`, Reqnroll, Testcontainers, WireMock, FluentAssertions)
cover the journey: the real query against a real database, the outbox sending to the provider, and
what the API returns afterwards. They're slower, and they catch what a mocked repository can't.

## API

| Method | Path | Does |
|---|---|---|
| `POST` | `/holds` | Places a hold: `{ memberId, title, format }` |
| `GET` | `/holds/{id}` | A hold, with its status and collect-by time |
| `DELETE` | `/holds/{id}` | Cancels a waiting hold |
| `POST` | `/returns` | Returns a copy: `{ copyId, title, format }` |
| `GET` | `/copies/{id}` | Where a copy is: the shelf, or the hold shelf for a hold |
| `GET` | `/admin/outbox` | How many notifications are pending, sent and failed |

Formats are `Hardback`, `Paperback`, `LargePrint` and `Audiobook`.

## Known gaps

The four rules don't cover everything, and this sample deliberately leaves the gaps alone rather than
deciding them without anyone agreeing. A fresh Claude, asked to write scenarios on the
`experiment/scenarios-from-code` branch, listed these as edge cases it would have asked about:

- A copy returned while it's already on the hold shelf fills a second hold, so two holds point at the
  same copy.
- A copy returned again with a different title or format keeps the title and format it was first
  returned with.
- Titles match exactly, so "the hobbit" doesn't fill a hold on "The Hobbit".
- Two holds placed at the same instant are filled in no particular order.

## Licence

The sample uses FluentAssertions 8, which is free for non-commercial use. Using it commercially needs a
paid licence; version 7.x, the last Apache-licensed one, works the same way for everything here.
