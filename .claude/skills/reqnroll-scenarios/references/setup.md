# Adding Reqnroll to a repo for the first time

Do this only after surveying what the repo already has (SKILL.md, "Survey the repo"). Prefer adding to the existing integration-test project over creating a new one, because it already has the project references, the test `appsettings` and the container helpers.

## 1. Packages

If the repo uses central package management (`Directory.Packages.props`), add versions there and unversioned `PackageReference`s in the test `.csproj`. Check the latest stable versions on NuGet; at the time of writing library-holds used `Reqnroll.xUnit` 3.3.4 and `Bogus` 35.6.5.

| Package | Why |
|---|---|
| `Reqnroll.xUnit` (or `.NUnit` / `.MSTest`) | Generates tests from `.feature` files; match the repo's test framework |
| `Microsoft.AspNetCore.Mvc.Testing` | `WebApplicationFactory` to host the real app |
| `Testcontainers.PostgreSql`, `Testcontainers.MsSql`, `Testcontainers.Kafka`, … | Real infrastructure; only what the journey touches |
| `Confluent.Kafka` | Admin client for topic creation and the settle check (usually already referenced through the app) |
| `WireMock.Net` | Stand-ins for external systems |
| `Shouldly` | Assertions with readable messages (or the repo's existing library) |
| `Bogus` | Seeded randomness for `@Randomised` scenarios |
| `Dapper` | Scenario queries in the database class and the outbox progress read |

In the test `.csproj`:

```xml
<PropertyGroup>
    <!-- Reqnroll regenerates each feature's code-behind on every build; writing it to obj/ keeps
         generated code out of source control and out of diffs. -->
    <ReqnrollUseIntermediateOutputPathForCodeBehind>true</ReqnrollUseIntermediateOutputPathForCodeBehind>
</PropertyGroup>
```

Copy the feature files to the output directory, so `UnusedSteps.cs` can read them:

```xml
<ItemGroup>
    <!-- Read by Scenarios/Support/UnusedSteps.cs. -->
    <None Update="Scenarios\Features\**\*.feature">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

Add `Sql\*.sql` with `CopyToOutputDirectory` if the database class loads scripts. No `reqnroll.json` is needed.

## 2. Folders and support code

Create the layout in [layers.md](layers.md) under `tests/<Service>.Tests.Integration/Scenarios/` and copy from this skill's `assets/` folder, renaming the `YourService` namespace and `AppUnderTest`:

| Asset | Becomes | Adapt |
|---|---|---|
| `Support/App/AppUnderTest.cs` | `Support/App/<Service>App.cs` | Marker type, containers, topics, `EveryConsumer`, setting keys, stand-ins, database class |
| `Support/App/ProcessingProgress*.cs`, `ConsumerProgress.cs`, `OutboxProgress.cs`, `KafkaConsumer.cs` | same | The outbox query assumes a SQL Server table `dbo.OutboxMessage` with `RowId`, `ProcessedOnUtc` and `DeadLetteredOnUtc` columns. Change it to match the service's outbox. If the service has no outbox, drop that half; if it has other async work, add it |
| `Support/App/FeatureContextExtensions.cs` | same | Base app type name |
| `Support/Hooks.cs` | same | One `[BeforeFeature]` per feature tag |
| `Contexts/ScenarioStateExtensions.cs` | same | — |
| `Support/UnusedSteps.cs` | same | — (fails scenarios, not the plain xUnit tests, when a binding matches no feature line) |
| `Support/Randomised/ScenarioRandom.cs` | same | Keep `Amount()` only if amounts are in play |
| — | `Scenarios/README.md` | Write for the service: its systems, layout, running, waiting, randomised and ignored scenarios. library-holds' [scenarios README](https://github.com/davidsho/library-holds/blob/main/tests/LibraryHolds.Scenarios/README.md) is a model |

If the repo already has a `WebApplicationFactory` that starts containers and configures the app, extend it so that it gains `ConsumersSwitchedOn`, `EveryConsumer`, the stand-ins, the database class and `WaitUntilSettledAsync`. Do not keep two parallel factories.

The settle check needs each switched-on consumer's **group id** and **topic**, with every topic created with **one partition**. Make sure the app's consumer configuration lets the test set both.

## 3. Analysers

Build with the repo's analysers on. Make sure an unused-parameter rule (IDE0060, or ReSharper's `UnusedParameter.Local`) reports at warning or above on the test project, so a step that ignores a value it captures is flagged (step-drift.md D3). If SonarCloud or StyleCop documentation rules (e.g. SA1600, SA1611, SA1615, SA1618, SA1623, AV2305) flood the test project, switch them off for tests in `.editorconfig` with a comment explaining why.

## 4. CI

Reqnroll tests are ordinary tests, so the existing `dotnet test` step runs them if Docker is available on the agent. Check the pipeline (`.azuredevops/`, `.github/`), and confirm with the user whether integration tests run in CI, before assuming so.

## 5. First feature

Follow the full Workflow in SKILL.md. Keep the first feature small (one or two rules) and get the whole chain green, including a deliberate failing run, before adding more.
