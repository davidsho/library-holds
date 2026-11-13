# Step drift

Step drift is when a line of Gherkin and the code behind it stop meaning the same thing. The feature still reads as the spec, but the test checks something else, so it can pass while the rule it states is broken. It creeps in when a step is first written loosely, when a shared step or driver is bent to suit a new feature, and when wording is edited in one place but not the others.

Check every rule below when writing or changing a step, binding or driver. Reviewers can cite them by ID.

## The rules

**D1. A step claims nothing its binding does not check.** Every outcome in a `Then` is asserted. "Ana's hold is still ready for collection *with the copy returned at 2027-03-01T09:00:00Z*" needs an assertion on which copy, not just on the hold's status. Either assert the claim or take it out of the wording.

**D2. A step checks no business rule it does not state.** A hidden assertion makes a failure read as the wrong rule breaking, and puts a rule in the code that the spec never mentions. Extra checks that only guard the scenario's own set-up belong in a `Require*` on a `Given`. An equivalence comparison of the whole object (layers.md §4) is fine; a targeted assertion on a field the Gherkin never mentions is not.

**D3. Every value in the step reaches the code.** Each captured parameter (`{decimal}`, `{string}`, regex group, outline `<placeholder>`) is passed on to a driver or context, never ignored or replaced by a default. Values written as words are the values used: "two days later" is `TimeSpan.FromDays(2)`, not three. A default the Gherkin deliberately leaves out (layers.md §5) is a named, documented constant, and the step wording does not suggest a value it does not use.

**D4. The wording's scope is the check's scope.** "Ana's hold" means the hold this scenario placed for Ana, from its context, not "any hold" or "the latest row". "Nobody is told anything" is checked for this scenario's input, so another scenario's data can neither pass nor fail it.

**D5. `Require*` backs `Given`s and `Assert*` backs `Then`s.** A `Given` that relies on its effect having happened pairs the action with a `Require*`. A `Then` that calls a `Require*` reports a broken rule as "Precondition failed", and a `Given` that calls an `Assert*` reports a broken set-up as a broken rule.

**D6. Changing a shared step or driver method is checked against every feature that uses it.** List every feature line that reaches the changed code, and confirm each still means what it says. If one feature needs a different meaning, add a new step or driver method with its own wording rather than bending the shared one.

**D7. One phrase means one thing, and one thing has one phrase.** Rewording a step rewords it in every feature. Two step texts bound to the same driver call with the same arguments are near-duplicates: keep one phrasing. The same phrase used for two different checks in different features is drift: rephrase one.

**D8. The binding's name and failure message follow the step text.** When the text changes, rename the method (keyword plus step text, layers.md §5) and update the `Assert*` failure message. A method name or message that no longer matches the step is the cheapest sign of drift.

**D9. No step definition goes unused.** A binding that matches no line in any feature is either dead or a step whose wording drifted. `assets/Support/UnusedSteps.cs` fails the run when one exists; without it, search `Features/` for each binding's text.

## What can be checked mechanically

- **D9**: `UnusedSteps.cs` (setup.md). It reads every feature file, so a filtered run reports the same as a full one.
- **D3, ignored parameters**: an unused-parameter analyser (IDE0060, or ReSharper's `UnusedParameter.Local`) at warning or above on the test project.
- Ambiguous and missing step definitions: Reqnroll already fails the scenario.

The rest needs a reader comparing the Gherkin with the binding and driver, line by line.
