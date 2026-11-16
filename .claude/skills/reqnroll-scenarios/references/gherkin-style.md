# Gherkin style

## Structure

```gherkin
@FeatureTag
Feature: <Journey name in plain English>

    <Prose: what triggers the journey, which systems it crosses (events, webhooks, external
    systems), and what the outcome must be. Reqnroll ignores this; the next reader does not.>

    Scenarios tagged @Randomised repeat a rule's examples with random <inputs>. See
    Scenarios/README.md for replaying one.

    Rule: <One business rule, as a sentence a product owner would agree with>

        <Optional prose: why the rule matters.>

        Scenario: <A concrete example of the rule>
            Given ...
            When ...
            Then ...
```

- **Tag the feature** (e.g. `@HoldsForReturnedCopies`). The tag picks the app in `Support/Hooks.cs` and becomes the `Category` trait for `dotnet test --filter`. A feature without a hook for its tag fails at start-up, by name.
- **Put how-to detail in the README, not the feature.** The feature points to `Scenarios/README.md` for replaying seeds and similar mechanics; its prose is about the business journey.
- **One `Rule:` per business rule**, with scenarios as examples of it.
- **No `Background:`** while there are only a few scenarios: repeat the `Given`s so each scenario reads top to bottom. Consider `Background` only when dozens of scenarios share identical set-up.
- Stripped to its `Rule:` lines, the file should read as the spec for the journey.

## Steps

- **Use domain language and each system's real name.** Name the actual events and webhooks, and **say which system** sends or receives them: `the payment provider posts a payment.captured webhook ...`, `a shipment is booked with the courier`. Do not use one system's name for another just because the names are similar. Avoid implementation words such as "insert row", "mock" or "call handler".
- **Reuse step text across features.** Steps are bound globally and only one definition may match a line, so write the same phrase for the same thing everywhere. Check `Steps/` before inventing new wording.
- **Use concrete values in hardcoded scenarios** (`£101.00`, `"The Hobbit"`, `2027-03-01T09:00:00Z`) so they can be followed line by line. Use obviously fictitious data, never real customer data.
- **`Given`** sets up the world, **`When`** is the single trigger under test, and **`Then`** asserts outcomes. Add more `When`s only when the trigger is genuinely a sequence ("And the payment provider redelivers that webhook").
- **A `Given` that checks a precondition** is fine when the rule only means something if it holds ("And Ana's hold is ready for collection"). Its binding uses a `Require*`, so a failure says "Precondition failed".
- **Negative cases matter as much as the happy path.** Include bystanders that must be left alone ("Ben placed a hold on ..." / "Ben's hold is still waiting").
- **A "nothing happens" scenario must also prove the input was processed**: `Then the copy is back on the shelf` / `And nobody is told anything` / `And Ana's hold is still waiting`.

## Scenario Outlines

Use an outline when one rule has many cases that differ only in data.

- The first column says **why** the row exists (`marker`, `why`, `period`). Put a comment above `Examples:` saying it is only a label:
  ```gherkin
            # <marker> only names each example; no step reads it.
            Examples:
  ```
  Also use a comment to explain any column whose exact format is part of a contract ("<collect by> is in UK time, with the offset the API sends, because the offset is part of the contract").
- Put the varying column in the scenario title (`An order whose <detail> is <value> isn't dispatched ...`, `A copy that reaches the hold shelf <when> can be collected ...`) so each generated test has a distinct name.
- **Cover exact boundaries**, not just "near" them: the last second before a change, the moment of the change, and just after it.
- Prefer one `When` step with a regex that isolates `<detail>` and `<value>`, backed by a single `switch`. Adding a case is then one row plus one switch arm.

## Randomised twins

Pair a hardcoded scenario with a `@Randomised` twin when its inputs have a meaningful range (amounts, accounts, instants).

```gherkin
        @Randomised
        Scenario Outline: A copy that reaches the hold shelf <when> can be collected until closing time on the 7th day
            Given Ana placed a hold on a random title in a random format
            When a copy matching Ana's hold is returned <when>
            Then Ana's hold is ready for collection
            And Ana is told the copy is ready

            # <run> only repeats the scenario, so it runs again with a new seed; no step reads it.
            Examples:
                | when                              | run |
                | at a random time                  | 1   |
                | in the week before a clock change | 2   |
                | around midnight UK time           | 3   |
```

- Use the same steps as the hardcoded twin, with "random", "that title" or "matching Ana's hold" in place of literals.
- Each `| run |` row is one more run with a new seed (Reqnroll and xUnit have no built-in repeat). Rows can combine a mode with a run number (`| when | run |`, e.g. `around a clock change`) to aim randomness at risky ranges.
- A plain `Scenario` can be `@Randomised` too, when one run is enough.

## Known defects

```gherkin
        # Known defect, ignored pending a product decision (TICKET-NNNN). <What goes wrong and why.>
        @ignore
        Scenario: <What *should* happen>
```

Write the scenario as the correct behaviour and tag it `@ignore`; it shows as skipped in every run. Make the random generators skip the defective range. When the ticket is fixed, remove `@ignore` and the skip, and the scenario then has to pass.
