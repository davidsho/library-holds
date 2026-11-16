# Scenarios

Reqnroll scenarios that drive the real API, as `Program.cs` builds it, against a real Postgres in a
container. The notification provider is the only stand-in (WireMock), because it's the only thing
outside the service. Every input goes in through the API, and every outcome is read back through the
API or from what the stand-in received. Unit logic stays in `LibraryHolds.Tests.Unit`.

## Running

Docker must be running. The first run pulls `postgres:17-alpine`.

```bash
dotnet test tests/LibraryHolds.Scenarios
```

Each feature's tag becomes a `Category` trait, so one feature, or only the randomised scenarios, can
be run on their own:

```bash
dotnet test tests/LibraryHolds.Scenarios --filter "Category=HoldsForReturnedCopies"
```

```bash
dotnet test tests/LibraryHolds.Scenarios --filter "Category=Randomised"
```

Add `--logger "console;verbosity=detailed"` to see each step and the random seed.

## Replaying a randomised failure

Every `@Randomised` scenario logs `Random seed: <n> (set SCENARIO_SEED=<n> to replay)`. Run it again
with that seed:

```bash
SCENARIO_SEED=<n> dotnet test tests/LibraryHolds.Scenarios --filter "Category=Randomised"
```

A `SCENARIO_SEED` that isn't a whole number fails the scenario, so a replay never quietly runs with a
different seed. When a replay shows a real bug, add its values as a hardcoded example so the case is
pinned for good.

## Layout

```
Features/              the .feature files, and nothing else
Steps/                 step definitions, one class per thing the Gherkin talks about
Drivers/               the only code that knows how to drive each part of the API
Contexts/              what one step leaves for the next, per scenario
Model/                 the records passed between them, and the API's JSON contracts
Support/App/           the app under test, the notification provider stand-in, the settle check
Support/Randomised/    the seeded randomness behind @Randomised scenarios
Support/               hooks, strict JSON, the independent collect-by calculation
```

## Conventions

- **The rules come from people.** The `Rule:` lines say what was decided. Edge cases are proposed,
  then accepted or rejected one by one. Current behaviour never becomes a rule without someone
  confirming it's intended, because the code may be the bug.
- **Steps make one or two driver calls and nothing else.** Drivers own every detail of the API.
- **Actions only act.** A `Given` that has to have taken effect uses a `Require*` method, which fails
  with "Precondition failed", so a broken set-up is never mistaken for a broken rule. A `Then` uses an
  `Assert*` method named after the sentence it backs.
- **No sleeps.** Every `Require*` and `Assert*` waits for the app to settle first (the outbox has
  nothing left to send), so "nobody is told anything" doesn't depend on a guessed delay.
- **Contracts are strict.** Responses and the messages the stand-in receives are read into the records
  in `Model/Contracts.cs`, which are written out here rather than borrowed from the API. A field the API
  adds or drops fails the read, and a value it changes fails the equivalence assertion that follows.
- **Expected values are worked out independently.** The expected collect-by time comes from NodaTime,
  not from the API's own calculation.
- **One phrase per meaning.** Steps are matched by text, so the compiler can't tell when two phrasings
  mean the same thing. Before adding a step, search `Steps/` for one that already says it. In review, a
  new step string is only accepted when its meaning differs from every existing one; otherwise the
  scenario reuses the existing wording.

## When it fails

- **"Feature '…' has no app to run against":** the feature's tag has no `[BeforeFeature]` in `Hooks.cs`.
- **"The app had not settled after 30s":** the outbox couldn't send its notifications. Check the test
  output for the provider's errors.
- **"The outbox gave up sending":** a notification failed every attempt, so a "nobody is told" scenario
  would otherwise pass for the wrong reason.
- **"The JSON no longer matches the … contract":** the API's response changed shape. Update the record in
  `Model/Contracts.cs` if the change was intended.
- **"Precondition failed: …":** a `Given` didn't take effect, so the rule under test was never reached.
- **"This step needs …, but no earlier step in the scenario did that":** a step is missing from the scenario.
