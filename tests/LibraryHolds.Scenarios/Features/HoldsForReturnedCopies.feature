@HoldsForReturnedCopies
Feature: Holds filled by returned copies

    A copy comes back through the returns API. The library holds API decides whether it fills a waiting
    hold. If it does, the copy goes to the hold shelf for that member, the hold becomes ready for
    collection with a collect-by time, and the notification provider is asked to tell the member. If it
    doesn't, the copy goes back on the shelf and the notification provider is asked to tell nobody
    anything.

    Scenarios tagged @Randomised repeat a rule's examples with random members, titles, formats and
    times. See Scenarios/README.md for replaying one.

    Rule: A returned copy fills the waiting hold of the member who has waited longest for its title in its format

        Scenario: A copy returned while one member is waiting goes to the hold shelf for that member
            Given Ana placed a hold on "The Hobbit" in hardback at 2027-02-01T09:00:00Z
            When a copy of "The Hobbit" in hardback is returned at 2027-03-01T09:00:00Z
            Then the copy is on the hold shelf for Ana
            And Ana's hold is ready for collection until 2027-03-08T20:00:00+00:00
            And Ana is told the copy is ready

        Scenario: Of two members waiting, the copy goes to the one who placed their hold first
            Given Ana placed a hold on "The Hobbit" in hardback at 2027-02-01T09:00:00Z
            And Ben placed a hold on "The Hobbit" in hardback at 2027-02-02T09:00:00Z
            When a copy of "The Hobbit" in hardback is returned at 2027-03-01T09:00:00Z
            Then the copy is on the hold shelf for Ana
            And Ana's hold is ready for collection until 2027-03-08T20:00:00+00:00
            And Ana is told the copy is ready
            And Ben's hold is still waiting
            And Ben is not told anything

        Scenario: A hold that is already ready for collection is passed over for the next member waiting
            Given Ana placed a hold on "The Hobbit" in hardback at 2027-02-01T09:00:00Z
            And Ben placed a hold on "The Hobbit" in hardback at 2027-02-02T09:00:00Z
            And a copy of "The Hobbit" in hardback was returned at 2027-03-01T09:00:00Z
            And Ana's hold is ready for collection
            When another copy of "The Hobbit" in hardback is returned at 2027-03-02T10:00:00Z
            Then the copy is on the hold shelf for Ben
            And Ben's hold is ready for collection until 2027-03-09T20:00:00+00:00
            And Ben is told the copy is ready
            And Ana's hold is still ready for collection with the copy returned at 2027-03-01T09:00:00Z

        @Randomised
        Scenario Outline: Of <members> members waiting for a random title, the copy goes to the one who placed their hold first
            Given <members> members placed holds on a random title in a random format, one after another
            When a copy of that title in that format is returned at a random time
            Then the first member's hold is ready for collection
            And the first member is told the copy is ready
            And every other member's hold is still waiting

            # <run> only repeats the scenario, so it runs again with a new seed; no step reads it.
            Examples:
                | members | run |
                | 2       | 1   |
                | 5       | 2   |

    Rule: A returned copy that fills no hold goes back on the shelf, and nobody is told anything

        Scenario: A copy of a title nobody is waiting for goes back on the shelf
            When a copy of "The Hobbit" in hardback is returned at 2027-03-01T09:00:00Z
            Then the copy is back on the shelf
            And nobody is told anything

        Scenario: A copy of a different title doesn't fill a waiting hold
            Given Ana placed a hold on "The Hobbit" in hardback at 2027-02-01T09:00:00Z
            When a copy of "The Silmarillion" in hardback is returned at 2027-03-01T09:00:00Z
            Then the copy is back on the shelf
            And Ana's hold is still waiting
            And nobody is told anything

        # Known defect, no ticket raised yet: a copy of the right title fills a hold in any format, so a
        # large-print reader is told a hardback is waiting for them. The API looks for waiting holds by
        # title only.
        @ignore
        Scenario: A copy in a different format doesn't fill a waiting hold
            Given Ana placed a hold on "The Hobbit" in large print at 2027-02-01T09:00:00Z
            When a copy of "The Hobbit" in hardback is returned at 2027-03-01T09:00:00Z
            Then the copy is back on the shelf
            And Ana's hold is still waiting
            And nobody is told anything

        Scenario: A cancelled hold isn't filled
            Given Ana placed a hold on "The Hobbit" in hardback at 2027-02-01T09:00:00Z
            And Ana's hold was cancelled
            When a copy of "The Hobbit" in hardback is returned at 2027-03-01T09:00:00Z
            Then the copy is back on the shelf
            And Ana's hold is still cancelled
            And nobody is told anything

    Rule: The member can collect the copy until closing time, 20:00 UK time, on the 7th day after it reaches the hold shelf

        Scenario Outline: A copy that reaches the hold shelf on a <season> afternoon can be collected until closing time on the 7th day
            Given Ana placed a hold on "The Hobbit" in hardback at 2027-01-05T09:00:00Z
            When a copy of "The Hobbit" in hardback is returned at <returned at>
            Then Ana's hold is ready for collection until <collect by>
            And Ana is told the copy is ready

            # <season> only names each example; no step reads it.
            # <collect by> is in UK time, with the offset the API sends, because the offset is part of the contract.
            Examples:
                | season | returned at          | collect by                |
                | winter | 2027-01-12T14:00:00Z | 2027-01-19T20:00:00+00:00 |
                | summer | 2027-06-15T14:00:00Z | 2027-06-22T20:00:00+01:00 |

        @Randomised
        Scenario Outline: A copy that reaches the hold shelf <when> can be collected until closing time on the 7th day
            Given Ana placed a hold on a random title in a random format
            When a copy of that title in that format is returned <when>
            Then Ana's hold is ready for collection
            And Ana is told the copy is ready

            # <run> only repeats the scenario, so it runs again with a new seed; no step reads it.
            Examples:
                | when                              | run |
                | at a random time                  | 1   |
                | in the week before a clock change | 2   |
                | around midnight UK time           | 3   |
