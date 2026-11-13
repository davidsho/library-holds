---
name: reqnroll-scenarios
description: Set up, write, extend, run or replay Reqnroll (BDD/Gherkin) end-to-end scenarios in a .NET service. Use when someone wants to add Reqnroll to a repo, add a .feature file, add scenarios or Examples rows, add a @Randomised twin, run Reqnroll tests, or replay a failing randomised run with SCENARIO_SEED. Also use when asked to "test the journey end to end" through the real app, message queues, webhooks, HTTP APIs and WireMock rather than with mocked unit tests.
---

# Reqnroll end-to-end scenarios

Reqnroll scenarios drive the **real app** (`WebApplicationFactory`, as `Program.cs` builds it) against **real infrastructure in containers** (Testcontainers: Postgres, SQL Server, Kafka, …). **WireMock** stands in only for external systems (payment providers, notification services, other teams' APIs, …). They test a **journey across systems**; unit logic stays in xUnit/NUnit with a mocking library.

Reqnroll is the maintained, open-source fork of SpecFlow, which reached end of life on 31 December 2024. It generates ordinary test-framework tests, so Test Explorer, `dotnet test` filters and CI work as usual.

The worked example is **library-holds** ([davidsho/library-holds](https://github.com/davidsho/library-holds), `tests/LibraryHolds.Scenarios/`). If it is checked out locally, read its `README.md` and the relevant drivers and steps before writing anything. Otherwise use [references/worked-example.md](references/worked-example.md).

Detailed guidance:
- [references/layers.md](references/layers.md): the folder layout and the rules for each layer (app, hooks, settle check, drivers, steps, contexts, model, randomness).
- [references/gherkin-style.md](references/gherkin-style.md): how to write the feature file.
- [references/setup.md](references/setup.md): adding Reqnroll to a repo that does not have it yet.
- [references/step-drift.md](references/step-drift.md): keeping each line of Gherkin and the code behind it meaning the same thing.
- [assets/](assets/): drop-in support code (settle check, hooks, context guard, seeded randomness, unused-step check) and a base app template.

## The approach in one screen

- **Organised by domain, not by feature.** `Scenarios/{Features,Steps,Drivers,Contexts,Model,Support}`. Steps are bound globally, so a new feature mostly reuses existing steps and drivers.
- **One shared base app.** Every feature gets the same database, topics and stand-ins. A feature's app only lists the **consumers it switches on**, and `Support/Hooks.cs` picks it by the feature's tag. A service with no consumers needs only the one app.
- **No sleeps and no polling for outcomes.** Every precondition check and assertion first waits for the app to **settle**: every consumer has committed past the end of its topic and the outbox has nothing left to publish, in two identical readings in a row. "X did not happen" then needs no guessed delay.
- **Steps make one or two driver calls.** Drivers own every detail of driving their part of the system. Actions only act. Preconditions use `Require*` ("Precondition failed: …") and outcomes use `Assert*`.
- **Contexts per domain concept**, with guards that name a missing earlier step instead of throwing a `NullReferenceException`.
- **The feature file is the spec.** Gherkin uses each system's real name, and keeps similarly named systems apart.
- **`@Randomised` twins** come with a logged, replayable seed. **`@ignore`** marks known defects, with the ticket number.
- **Generated code-behind is not committed** (`ReqnrollUseIntermediateOutputPathForCodeBehind`).

## First, work out where you are and what the user wants

1. **Survey the repo** before proposing anything:
   - Is Reqnroll already in? Look for `*.feature`, `[Binding]`, `Reqnroll.*` in `Directory.Packages.props` or the test `.csproj`, and a `Scenarios/README.md`. If it exists, **that README and the existing code override this skill** where they differ.
   - What integration-test infrastructure exists already (`WebApplicationFactory` subclasses, Testcontainers builders, WireMock fixtures, DB helpers)? Reuse it rather than building a second set.
   - Which test framework does the repo use (xUnit, NUnit, MSTest)? Use the matching `Reqnroll.<Framework>` package.
   - Does it have `Support/UnusedSteps.cs`? If not, offer to add it ([setup.md](references/setup.md) §1–2), so a binding no feature uses fails the run.
   - How does the service take input (message consumers, webhooks, HTTP API, scheduled jobs) and what does it produce (events, DB rows, outbound HTTP)? Does it use a transactional outbox?
2. Then decide:
   - **Run or replay existing scenarios** → [Running](#running).
   - **Add cases to an existing feature** → [Workflow](#workflow) steps 1, 3, 5–8.
   - **New feature in a repo that already has scenarios** → full [Workflow](#workflow). It will usually need only a tag, a thin app, a hook line and a few new steps.
   - **First feature in a repo** → [setup.md](references/setup.md), then the full [Workflow](#workflow).

## The non-negotiable rule: the user defines the rules

The feature file is the spec. It must say what the team decided, not what a model guessed.

- **Do not invent business rules or scenarios.** Before writing Gherkin, get the `Rule:` lines in plain English from the user, or from a ticket, PR or design doc they point you to. These state what must happen and what must *not* happen.
- If the rules are unclear, ask. Read the production code to *understand* the behaviour, but do not turn what the code currently does into a rule without the user confirming it is intended, because the code may be the bug.
- **Once the rules are agreed**, propose edge cases: exact boundaries (the last second before and the moment of a clock change), negative cases, bystanders that must be left alone, case sensitivity, duplicates, redelivery, ordering. Present them as a list for the user to accept or reject one by one, and write only the accepted ones.
- Known defects the user wants documented go in as `@ignore` scenarios with a comment naming the ticket. Never make a test pass by asserting buggy behaviour.

Do the heavy lifting on the infrastructure: the app, stand-ins, drivers, step bindings, hooks, message, webhook and WireMock plumbing, and the settle check.

## Workflow

1. **Agree the rules** (above). Write them back as the `Rule:` lines you intend to use and get a yes.
2. **Map the journey** from the production code:
   - the entry points the scenario will push into (topics consumed, webhook endpoints and their auth, API routes);
   - the observable outcomes (events published, DB rows, calls to external systems);
   - **which consumers the journey needs**, plus any topics, tables, stand-in stubs, feature flags (a flag service may run offline in tests) or hosted-service removals the shared app does not yet have.
   Tell the user what you found, and which existing steps and drivers you expect to reuse, before writing code.
3. **Write the feature file** in `Scenarios/Features/`, following [gherkin-style.md](references/gherkin-style.md). Show it to the user for review before writing bindings.
4. **Wire the feature in**: a thin `Support/App/<Feature>App.cs` listing its consumers, and a `[BeforeFeature("<Tag>")]` line in `Support/Hooks.cs`. Add anything new the journey needs (topic, stand-in stub, table, query) to the **shared** base app, its database class or its stand-ins. Never add it to the feature's app.
5. **Steps, drivers, contexts, model**, following [layers.md](references/layers.md). For each step line, search `Steps/` first and reuse the existing step if there is one. Put new steps in the domain step class they belong to, and new actions or assertions on the driver for that part of the system. Check every new or changed step against [step-drift.md](references/step-drift.md); when you change a shared step or driver method, re-check every feature line that uses it (D6).
6. **Randomised twins**, where inputs have a meaningful range: a `@Randomised` `Scenario Outline` with a `| run |` table next to the hardcoded scenario it mirrors, drawing from `ScenarioRandom` via a `Support/Randomised/` generator.
7. **Build and run** the feature (below). Every scenario must pass. Then prove a key `Then` can fail: temporarily break an example value so it should flip the outcome, confirm the failure message reads in business terms, then revert.
8. **Update `Scenarios/README.md`** if the layout, running instructions or waiting model changed. Build with the repo's analysers and fix warnings.

## Running

Docker must be running for Testcontainers. The first run pulls images and is slow.

Each feature tag becomes a `Category` trait, and each scenario title becomes the `Description` trait (xUnit). With NUnit or MSTest, use `TestCategory`/`Category` and `Name~`.

```bash
dotnet test <path/to/Tests.Integration> --filter "Category=<FeatureTag>"
```

```bash
dotnet test <path/to/Tests.Integration> --filter "Category=<FeatureA>|Category=<FeatureB>"
```

```bash
dotnet test <path/to/Tests.Integration> --filter "Description~<part of scenario title>"
```

Add `--logger "console;verbosity=detailed"` to see step-by-step output and the logged seed.

### Replaying a randomised failure

Every `@Randomised` scenario logs `Random seed: <n> (set SCENARIO_SEED=<n> to replay)`. Run the same scenario with that seed:

```bash
SCENARIO_SEED=<n> dotnet test <path/to/Tests.Integration> --filter "Description~<failing scenario title>"
```

A `SCENARIO_SEED` that is not a whole number fails the scenario rather than running with a random seed. The `run` column in Examples only repeats a row so that it runs again with a new seed; no step reads it. When a replay exposes a real bug, add a **hardcoded** scenario with those values.

### When it fails

- **"Feature '…' has no app to run against":** the feature's tag has no `[BeforeFeature]` in `Hooks.cs`.
- **"… had not settled after 45s: X has handled n of m messages; …":** a consumer is failing or stuck. Check the test output logs for its exception, and check that the consumer, topic and group id are configured.
- **"The outbox gave up publishing …":** an event was dead-lettered. Read the error recorded against it in the outbox table.
- **"Precondition failed: …":** the set-up did not take effect, so the rule under test was never reached. Usually a consumer is not switched on for this feature.
- **"This step needs …, but no earlier step in the scenario did that":** a step is missing from the scenario.
- **Container start-up errors:** Docker is not running, or the image pull is blocked (check network/VPN). On Apple Silicon, use images with arm64 manifests (e.g. `apache/kafka-native`).
- **"These step definitions match no line in any feature":** `UnusedSteps.cs` found a binding no feature uses. Delete it, or fix the wording that drifted away from it (step-drift.md D7, D9).
- **"No matching step definition" or "ambiguous step definitions":** the step text does not match, or matches two bindings. Reqnroll prints a suggested binding.

## Done means

- Rules agreed with the user; no invented scenarios.
- The feature reads as a spec: stripped to its `Rule:` lines, a product owner can check it.
- New code went into the shared domain classes. The feature itself added only its `.feature`, its thin app and one hook line, plus genuinely new steps and driver methods.
- No sleeps or outcome polling. Preconditions use `Require*` and outcomes use `Assert*`, both after the app settles.
- Negative cases assert the absence of the outcome *and* that the input was processed.
- Every new or changed step passes [step-drift.md](references/step-drift.md), including the other features that use a shared step or driver you changed.
- All scenarios in the feature pass locally, and a deliberate break was shown to fail readably.
