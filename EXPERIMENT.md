# Experiment: scenarios from the code, with the format hints removed

This is a second run of the [first experiment](https://github.com/davidsho/library-holds/tree/experiment/scenarios-from-code),
to see whether the model needed the comments and the unit test name that told it holds match on format.

This branch has its own history, separate from `main` and `planted-bug`. Its first commit is the same code the
first experiment started from, where the query for waiting holds ignores format, with these also removed:

- the two comments in `HoldMatcher.cs` that said a hold is found for a title "in this format" and "title and format";
- the unit test name `Looks_for_waiting_holds_with_the_returned_copys_title_and_format`, now
  `Looks_for_waiting_holds_for_the_returned_copy` (the test body is unchanged);
- "in a particular format" from the comment on `Hold`;
- the comment on `ReturnedCopy` saying it carries "every detail the matching rule depends on".

The code itself is untouched. The query still ignores format, `FindWaitingHoldsAsync` still takes a `format` it never
uses, the database still has an index on title, format and status, and the README still says a member places a hold
on a title in a format.

The agent had a copy of the [skill](https://github.com/davidsho/library-holds/tree/main/.claude/skills/reqnroll-scenarios)
from `main` in `.claude/skills`, with its library-holds worked example removed, because that describes this repo's
rules and links to the first experiment. The links to this repo, and one outline title that hinted at matching, were
taken out of the rest of the skill for the same reason.

A fresh Claude agent was given the same request as the first experiment, word for word, and told nobody could answer
questions. It was told to work only in this repository. The second commit is its feature file, steps and two new
driver methods, exactly as it left them.

## What it did

- **It caught the bug again.** It named three clues: the unused `format` parameter, the index on title and format,
  and the README's description of a hold. It wrote the format scenario for the correct behaviour, tagged `@ignore`
  with a comment saying no ticket has been raised. It didn't change production code. The run is 13 passed and 1
  skipped; with the one-line fix and the tag removed, all 14 pass (checked independently).
- **None of its rules were agreed.** It wrote three `Rule:` lines close to the four agreed ones, taken from comments
  and the README. Its report said it had marked them as unconfirmed, but only the report does. The feature file
  doesn't say so, and its `Rule:` lines read like decisions.
- **It decided some behaviour itself.** It wrote scenarios saying a cancelled hold isn't filled and a hold that's
  already ready is passed over, treating both as what "waiting" means.
- **It proposed edge cases without writing them**, much as the first run did: holds placed at the same instant, title
  case and spacing, a copy scanned twice, a known barcode returned with a different title or format, and the
  notification provider failing.

## What the clues have in common

Every clue it used was written by someone who knew holds match on format: the parameter, the index and the README all
came from the agreed rules. The model still depended on rules a person had decided. It found them in quieter places
than a comment.
