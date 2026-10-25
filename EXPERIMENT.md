# Experiment: scenarios from the code alone

This branch has its own history, separate from `main` and `planted-bug`. Its first commit is the code
from `planted-bug`, where the query for waiting holds ignores format, with these removed:

- the feature file and the step definitions;
- the README's list of rules, and its mention of the essay;
- a comment on the matcher's unit test that pointed at the query.

That first commit was the whole repository, so there was no history to read. A fresh Claude agent, with
no knowledge of the agreed rules or the planted bug, had the Reqnroll skill the team uses at Insignis and was
given the request a developer would type:

> /reqnroll-scenarios Add Reqnroll scenarios covering how returned copies fill holds in this API. The
> test infrastructure in tests/LibraryHolds.Scenarios already exists (the app under test, drivers,
> contexts, the notification provider stand-in, randomised support); there's no feature file or step
> definitions yet. Add a feature file under tests/LibraryHolds.Scenarios/Features and step definitions
> under tests/LibraryHolds.Scenarios/Steps. Make sure they pass.

It was told nobody could answer questions, and to write down any it would have asked and where. The
second commit is its feature file, steps and one new driver method, exactly as it left them.

## What it did

- **It caught the bug.** It read the comments and a unit test name saying holds are matched on title
  and format, saw that the query ignores format, and wrote both format scenarios for the correct
  behaviour, tagged `@ignore` with a comment naming the cause. It didn't change production code. The run
  is 16 passed and 2 skipped; with the one-line fix and the tags removed, all 18 pass (checked
  independently).
- **It reached both of the skill's checkpoints and carried on.** The skill stops for a person twice: to
  agree the `Rule:` lines before any Gherkin, and to review the feature file before any bindings. With
  nobody there, it wrote down what it would have asked at each point and kept going.
- **None of its rules were agreed.** It took three rules from the code's comments, with part of one
  ("nobody is told anything") from its own reading. Its account of where they came from began "Nobody
  could confirm these, so none of them is an agreed rule." The feature file doesn't say so. It's green,
  and its `Rule:` lines read like decisions.
- **It proposed edge cases without writing them,** as the skill asks: two holds placed at the same
  instant, case-sensitive titles, a copy returned again while it's on the hold shelf, a known barcode
  returned with a different title or format, simultaneous returns, and a provider outage.

## Caveat

The comments it relied on were written by the Claude that built the app, from rules agreed with a person
beforehand. In a codebase where the comments are missing, or describe the bug, there'd be nothing to tell
it the code was wrong.
