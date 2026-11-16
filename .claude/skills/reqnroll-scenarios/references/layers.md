# The layers, and how to write each one

The code is organised by **what it is about** (holds, returns, notifications…), **not by which feature uses it**. Reqnroll binds steps globally, so any feature can use any step, and every driver works against every feature's app. A new feature should mostly reuse what already exists.

```
Scenarios/
  README.md                how the scenarios work, for the next engineer (keep it current)
  Features/                the .feature files, and nothing else
  Steps/                   step definitions, one class per domain concept
  Drivers/                 one driver per part of the system: its actions, Require* and Assert* methods
  Contexts/                the state one step leaves for the next, one class per domain concept
  Model/                   the records steps, drivers and contexts pass between them
  Support/App/             the app under test, its database, its stand-ins and the settle check
  Support/Randomised/      ScenarioRandom and random-but-valid details for @Randomised scenarios
  Support/                 Hooks.cs, and small extension helpers (e.g. ToPence)
```

```
Feature  →  Steps (1–2 driver calls)  →  Drivers (Kafka, webhooks, SQL, Shouldly)  →  <Service>App (one shared base)
               ↕ Contexts (per domain concept)                                          ↑ chosen per feature tag in Hooks.cs
```

Match the target repo's namespaces, analyser rules and comment density. Examples come from library-holds where it has the pattern, and are illustrative where it doesn't (it has no message consumers, so one app serves every feature).

## 1. The app: `Support/App/<Service>App.cs` and a thin app per feature

**One base app for every feature.** It owns the containers, the database, every topic, every stand-in, webhook posting and the settle check. A feature's app only says **which consumers run**, because the consumers are what make one journey differ from another:

```csharp
public sealed class OrderFulfilmentApp : ShopApp
{
    protected override IReadOnlyDictionary<string, string> ConsumersSwitchedOn { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["OrderPlaced"] = OrderPlacedTopic,
            ["PaymentCaptured"] = PaymentCapturedTopic
        };

    // The feature-flag service runs offline in tests, which would leave fulfilment switched off.
    protected override void ReplaceServices(IServiceCollection services) =>
        services.AddSingleton<IFeatureFlags, EnabledFeatureFlags>();
}
```

Rules:
- The base keeps an **`EveryConsumer` list**. Any consumer a feature does not switch on is explicitly switched off.
- **Each external system gets its own stand-in class**, named after the system (`NotificationProviderStandIn` in library-holds). Each wraps a `WireMockServer`, sets up its stubs (`AcceptHoldReadyMessages()`), exposes what it received (`HoldReadyMessages()`) and disposes itself. When two systems have similar names (two products from the same vendor, say), keep them separate in the code and in the Gherkin.
- **Fake only what is outside the service.** Controllers, consumers, publishers, the outbox, repositories and HTTP clients stay as production code.
- **The database gets its own class** (`<Service>Database`) that creates the schema (migrations plus any `Sql/*.sql` scripts) and holds the queries scenarios need. Drivers call `app.Database.*` and contain no SQL of their own.
- Remove hosted services that no scenario uses and that would only add noise (e.g. a periodic index refresh) in the base's `CreateHost`.
- Shorten production polling intervals (the outbox `PollInterval`) with a comment giving the production value.
- Create every topic up front, with **one partition** (the settle check relies on this).
- Start-up is slow, so start one app per feature, never per scenario.

## 2. Hooks: `Support/Hooks.cs`

All feature lifecycle hooks live in one class:

```csharp
[BeforeFeature("OrderFulfilment")]
public static Task StartTheOrderFulfilmentAppAsync(FeatureContext feature) =>
    feature.StartAppAsync<OrderFulfilmentApp>();

[BeforeFeature(Order = 20_000)]           // guard: fails by name if no hook above started an app
public static void RequireAnAppForTheFeature(FeatureContext feature) { ... }

[AfterFeature]
public static Task StopTheAppAsync(FeatureContext feature) => feature.StopAppAsync();

[BeforeScenario("OrderFulfilment")]
public Task ForgetEarlierScenariosOrdersAsync() => orders.ForgetEarlierScenariosOrdersAsync();
```

- `FeatureContextExtensions.StartAppAsync<TApp>()` registers the app **as the base type**, so drivers depend on `<Service>App` and work in every feature.
- `StopAppAsync` does nothing if the app never started, so a start-up failure is not hidden behind a second error.
- **Adding a feature means adding a tag and one `[BeforeFeature]` line here.** Steps and drivers are only written for what nothing existing covers.
- A `[BeforeScenario]` reset first waits for the app to settle, so nothing an earlier scenario set off lands after the reset.

Templates: `assets/Support/Hooks.cs` and `assets/Support/App/FeatureContextExtensions.cs`.

## 3. Waiting: the settle check, not polling or sleeps

**Steps never sleep for a fixed time and never poll for a particular outcome.** Every `Require*`/`Assert*` first calls `app.WaitUntilSettledAsync()`, which waits until the service has finished with everything it has been given:

- every consumer the feature runs has **committed past the last message on its topic** (Kafka admin: end offset vs the consumer group's committed offset), and
- the **outbox has no unpublished rows**.

It passes only when **two readings in a row are idle and identical**, because one idle reading can fall between a consumer queuing an event and the outbox picking it up. A dead-lettered outbox row throws, because otherwise a "nothing was published" scenario would pass for the wrong reason. A timeout reports where each consumer and the outbox had got to.

The result: each read is the outcome, not a moment on the way to it, and "X did **not** happen" needs no guessed delay.

Templates: `assets/Support/App/ProcessingProgress*.cs`, `ConsumerProgress.cs`, `OutboxProgress.cs`, `KafkaConsumer.cs`, and `WaitUntilSettledAsync` in `AppUnderTest.cs`. If the service does more asynchronous work (another queue, a background job), add it to `ProcessingProgress` rather than adding a sleep. A service with no consumers only needs the outbox half: library-holds reads its outbox through an admin endpoint.

## 4. Drivers: `Drivers/<PartOfTheSystem>Driver.cs`

One driver per part of the system (`HoldsDriver`, `ReturnsDriver`, `NotificationsDriver` in library-holds). A driver is the **only** place that knows how its part is driven: HTTP calls, messages, webhook JSON, SQL (through the database class) and the assertion library.

```csharp
public sealed class HoldsDriver(LibraryHoldsApp app)
{
    public async Task<PlacedHold> PlaceAsync(string member, string title, string format, DateTimeOffset at) { ... }

    /// <summary>For a Given that must have taken effect before the scenario can go on.</summary>
    public async Task RequireReadyForCollectionAsync(PlacedHold hold, ReturnedCopy copy)
    {
        try
        {
            await AssertReadyForCollectionAsync(hold, copy, Wire.Instant(ExpectedCollectBy.For(copy.ReturnedAt)));
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Precondition failed: {e.Message}", e);
        }
    }

    public Task AssertWaitingAsync(PlacedHold hold) =>
        AssertIsAsync(hold, Expected(hold, "Waiting"), "{0}'s hold should still be waiting");

    private async Task AssertIsAsync(PlacedHold hold, HoldJson expected, string because, params object[] becauseArgs)
    {
        await app.WaitUntilSettledAsync();
        var actual = await app.Api.GetFromStrictJsonAsync<HoldJson>($"/holds/{hold.Id}");
        actual.Should().BeEquivalentTo(expected, o => o.ComparingRecordsByMembers(), because, [hold.Member, .. becauseArgs]);
    }
}
```

Rules:
- **Depend only on the base app**, never on a feature's app. A driver may use another driver where the domain does (e.g. `RefundsDriver(ShopApp app, PaymentsDriver payments)`).
- **Actions only act.** Whether a consumer then reacts depends on which consumers the feature runs, so an action never waits for an outcome. The **step** says what it needs, using a `Require*`.
- **Three kinds of method**:
  - actions (`PlaceAsync`, `CancelAsync`, `ReturnAsync`);
  - **`Require*`** for preconditions in `Given`s, with failure messages starting **`Precondition failed:`** so a broken set-up is never mistaken for a broken rule;
  - **`Assert*`** for `Then`s, named after the Gherkin they back, with a message describing what actually happened.
- **Build inputs as production or the real sender does**, and name the source (`The same message OrderPlacedPublisher builds`). Make deliberate differences from real life explicit (e.g. an event's `OccurredAt` set a few seconds after the thing it describes, so a consumer dating from the wrong field fails).
- **Contract checks on outbound calls read the raw JSON field by field**, against a field list spelt out in the driver rather than read from the production request model. Renaming or re-serialising a field then fails the contract instead of silently moving with it.
- **Compare whole objects with an equivalence assertion where it makes sense and is possible**, using whatever assertion library the repo has (Shouldly `ShouldBeEquivalentTo`, FluentAssertions `BeEquivalentTo`, …). Build the expected object in the driver from the scenario's own data and compare it with what the app produced (a stored row, a consumed event, a deserialised request). When someone adds a field, the comparison then fails until the expectation covers it, instead of the new field going unchecked. Use it only where:
  - the whole object is deterministic, or the non-deterministic members (generated ids, timestamps) can be excluded or set from the actual value deliberately, with a comment;
  - the library supports it cleanly for that type.
  It is **in addition to**, not instead of, the other assertions: keep the targeted `Assert*` checks that state the rule, with their business-language failure messages, and keep the raw-JSON field-list checks for wire contracts. Never use it to pass an object that was copied from the actual value.
- **Allow for at-least-once delivery.** Assert on the first matching event and say why a duplicate is acceptable.
- Response codes from webhook posts are asserted in the driver (`response.StatusCode.ShouldBe(HttpStatusCode.OK)`).
- `ForgetEarlierScenarios…Async` resets live on the driver that owns the data, and wait for the app to settle first.

## 5. Steps: `Steps/<DomainConcept>Steps.cs`

One class per **domain concept the Gherkin talks about** (`HoldSteps`, `ReturnSteps`, `NotificationSteps` in library-holds), not per feature.

```csharp
[Binding]
public sealed class HoldSteps(HoldsDriver holds, HoldsContext placed, CopiesContext returned /* , ... */)
{
    [Then("{word}'s hold is still waiting")]
    public Task ThenMembersHoldIsStillWaitingAsync(string member) => holds.AssertWaitingAsync(placed.For(member));
}
```

Rules:
- **A step makes one or two driver calls and nothing else.** A `Given` that needs its effect to have happened pairs the action with a `Require*`:
  ```csharp
  await orders.PlaceAsync(order.Order);
  await orders.RequireStatusAsync(order.Order, OrderStatus.Placed);
  ```
- **Only one step definition may match a line of Gherkin**, across the whole project. Before writing one, search `Steps/` for an existing step. If two features need different meanings for similar text, rephrase one of them.
- Method name: keyword plus step text in PascalCase, with an `Async` suffix when it returns a `Task`.
- **Cucumber expressions** (`{decimal}`, `{int}`, `{word}`, `{string}`) by default. Use a `^...$` regex only to capture free text such as `<detail>`.
- For an outline that varies one detail, a single `switch` in the step is **the one list** of what the rule depends on, ending in `_ => throw new ArgumentOutOfRangeException(...)`. A randomised variant picks a value guaranteed to differ and delegates to the same step.
- Literal formats (UTC instants, UK wall clocks, an external system's date format) are parsed or formatted with exact patterns and `CultureInfo.InvariantCulture`, in a named constant or private helper.
- Steps may hold small, documented defaults that the Gherkin deliberately leaves out (e.g. `AnyCustomer` for scenarios about *when* an order was placed, not who placed it).

## 6. Contexts: `Contexts/<DomainConcept>Context.cs`

One class per domain concept (`HoldsContext`, `CopiesContext` in library-holds). Reqnroll creates one of each per scenario and injects it into every step class that asks for it, so steps in different classes share state.

```csharp
public sealed class HoldsContext
{
    private readonly List<PlacedHold> _inOrderPlaced = [];

    public void Add(PlacedHold hold) => _inOrderPlaced.Add(hold);

    public PlacedHold For(string member) =>
        _inOrderPlaced.SingleOrDefault(h => h.Member == member).OrNotYetGiven($"{member} to have placed a hold");
}
```

- **Nullable state with an `OrNotYetGiven` guard** (`assets/Contexts/ScenarioStateExtensions.cs`). A step used without its prerequisite fails with "This step needs Ana to have placed a hold, but no earlier step in the scenario did that", not a `NullReferenceException`.
- Add `HasX` where a step's behaviour legitimately depends on whether an earlier step ran (e.g. `CustomerContext.HasAccount`: the order goes to the customer's account if one was set up, otherwise to a new one).
- No logic beyond that, and no statics.

## 7. Model: `Model/*.cs`

Small records passed between steps, drivers and contexts (`PlacedHold`, `ReturnedCopy` in library-holds), one per file, each with a doc comment saying what it represents in domain terms. A record that describes an input carries **every detail the rule matches on**, so steps vary it with `with { ... }`.

## 8. Randomness: `Support/Randomised/`

- **`ScenarioRandom`** (`assets/Support/Randomised/ScenarioRandom.cs`): one seeded `Bogus.Randomizer` per scenario. It logs `Random seed: <n> (set SCENARIO_SEED=<n> to replay)`, honours `SCENARIO_SEED`, and **fails the scenario if `SCENARIO_SEED` is set but is not a whole number**, so a replay never quietly runs with a different seed. Generators shared across domains (e.g. `Amount()`) live here.
- **`Random<Thing>Details` / `Random<Thing>s`** (`RandomHoldDetails`, `RandomInstants`) take `ScenarioRandom` and produce random but valid values:
  - every `*OtherThan` method **guarantees** a different value;
  - values stay within what the wire format accepts, with a comment saying why;
  - risky ranges are targeted deliberately (clock changes), and expected values are worked out independently of the code under test;
  - known-defect ranges are skipped, naming the ticket.
- Never use `Random`, `Guid` or `DateTime.Now` for inputs that affect the outcome. Ids such as `Guid.NewGuid()` for uniqueness are fine.

## 9. Small helpers: `Support/*Extensions.cs`

Put wire-format conversions in one place and make them strict. For example, `ToPence()` **refuses a fraction of a penny** rather than rounding it, because rounding would hide a broken example.

## 10. Project settings

- `<ReqnrollUseIntermediateOutputPathForCodeBehind>true</ReqnrollUseIntermediateOutputPathForCodeBehind>` in the test `.csproj`, with a comment. Generated `*.feature.cs` files go to `obj/` and are **not committed**.
- `Sql\*.sql` copied to output with a wildcard, not file by file.
- If SonarCloud or StyleCop flags missing XML documentation on test code, turn those rules off for tests in `.editorconfig` with a comment, rather than adding boilerplate comments. Keep the real doc comments that explain *why*.

## Checklist before handing back

- [ ] The feature's tag has a `[BeforeFeature]` in `Hooks.cs` and a thin app that only lists its consumers (plus any `ReplaceServices`).
- [ ] New steps went into the existing domain step classes, and no step text is duplicated or near-duplicated.
- [ ] Every new or changed step passes [step-drift.md](step-drift.md), and every feature using a changed shared step or driver method still means what it says.
- [ ] Every `Given` that depends on an effect uses a `Require*`, and every `Then` uses an `Assert*`, both after `WaitUntilSettledAsync`.
- [ ] No `Task.Delay`, `Thread.Sleep` or outcome polling in steps or drivers.
- [ ] Context properties are guarded with `OrNotYetGiven`.
- [ ] Where it makes sense, outcomes are also compared with an equivalence assertion, so new fields are covered automatically, alongside the targeted `Assert*` checks.
- [ ] `Scenarios/README.md` updated if the layout, running instructions or waiting model changed.
- [ ] `dotnet test ... --filter "Category=<FeatureTag>"` passes, a deliberate break failed readably and was reverted, and randomised scenarios pass over several runs.
