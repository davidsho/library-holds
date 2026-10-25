# Library holds

A library holds API. A member places a hold on a title in a format. When a copy is returned, the API
decides whether it fills a waiting hold, and an external notification provider is asked to tell the
member.

## Running

Needs the .NET 10 SDK, and Docker for the scenarios.

```bash
dotnet test
```

See the [scenarios README](tests/LibraryHolds.Scenarios/README.md) for running one feature, replaying a
randomised failure, and the conventions the scenarios follow.

## Tests

**Unit tests** (`tests/LibraryHolds.Tests.Unit`, xUnit, Moq, FluentAssertions) cover logic that lives in
one place.

**Scenarios** (`tests/LibraryHolds.Scenarios`, Reqnroll, Testcontainers, WireMock, FluentAssertions)
cover journeys through the real API.

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
