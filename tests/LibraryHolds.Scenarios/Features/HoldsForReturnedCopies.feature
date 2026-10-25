@HoldsForReturnedCopies
Feature: Holds for returned copies

    A member places a hold on a title in a format through the holds API. When a copy is returned
    through the returns API, the library decides there and then whether it fills a waiting hold. A
    copy that fills a hold goes on the hold shelf for that member, and the notification provider is
    asked to tell them it is ready and when they must collect it by. A copy that fills no hold goes
    back on the shelf, and nobody is told anything.

    Scenarios tagged @Randomised repeat a rule's examples with random members, titles, formats and
    times. See tests/LibraryHolds.Scenarios/README.md for replaying one.

    Rule: A returned copy fills the hold of the member who has waited longest for that title in that format

        Scenario: The member who placed their hold first gets the returned copy
            Given Ana placed a hold on the hardback of "The Hobbit" at 2027-01-04T09:00:00Z
            And Ben placed a hold on the hardback of "The Hobbit" at 2027-01-05T09:00:00Z
            When a hardback copy of "The Hobbit" is returned at 2027-03-01T09:00:00Z
            Then the copy is on the hold shelf for Ana
            And Ana's hold is ready for collection with the copy until 2027-03-08T20:00:00+00:00
            And Ana is told the copy is ready
            And Ben's hold is still waiting
            And Ben is not told anything

        Scenario: The next copy goes to the member who is next in line
            Given Ana placed a hold on the hardback of "The Hobbit" at 2027-01-04T09:00:00Z
            And Ben placed a hold on the hardback of "The Hobbit" at 2027-01-05T09:00:00Z
            And a hardback copy of "The Hobbit" was returned at 2027-03-01T09:00:00Z and filled Ana's hold
            When a hardback copy of "The Hobbit" is returned at 2027-03-02T09:00:00Z
            Then the copy is on the hold shelf for Ben
            And Ben's hold is ready for collection with the copy until 2027-03-09T20:00:00+00:00
            And Ben is told the copy is ready
            And Ana's hold is still ready for collection with the copy returned at 2027-03-01T09:00:00Z until 2027-03-08T20:00:00+00:00

        # Known defect, ignored until it is fixed (no ticket yet: raise one and name it here).
        # HoldRepository.FindWaitingHoldsAsync filters waiting holds on title only, not format, so the
        # paperback goes to Ana, who has waited longer but for the hardback.
        @ignore
        Scenario: A copy in one format passes over a longer wait for another format
            Given Ana placed a hold on the hardback of "The Hobbit" at 2027-01-04T09:00:00Z
            And Ben placed a hold on the paperback of "The Hobbit" at 2027-01-05T09:00:00Z
            When a paperback copy of "The Hobbit" is returned at 2027-03-01T09:00:00Z
            Then the copy is on the hold shelf for Ben
            And Ben's hold is ready for collection with the copy until 2027-03-08T20:00:00+00:00
            And Ben is told the copy is ready
            And Ana's hold is still waiting
            And Ana is not told anything

        @Randomised
        Scenario Outline: A copy returned when <members> members are waiting goes to the one who placed the first hold
            Given <members> random members placed holds on the same random title in the same random format, one after another
            When a copy of that title in that format is returned at a random time
            Then the copy is on the hold shelf for the member who placed the first hold
            And that member's hold is ready for collection with the copy until 20:00 UK time on the seventh day after it was returned
            And that member is told the copy is ready
            And every other member's hold is still waiting
            And no other member is told anything

            # <run> only repeats the scenario, so it runs again with a new seed; no step reads it.
            Examples:
                | members | run |
                | 2       | 1   |
                | 3       | 1   |
                | 5       | 1   |
                | 5       | 2   |

    Rule: A returned copy that no waiting hold is for goes back on the shelf, and nobody is told anything

        Scenario: A copy of a title nobody is waiting for goes back on the shelf
            Given Ana placed a hold on the hardback of "The Hobbit" at 2027-01-04T09:00:00Z
            When a hardback copy of "Dune" is returned at 2027-03-01T09:00:00Z
            Then the copy is back on the shelf
            And nobody is told anything
            And Ana's hold is still waiting

        Scenario: A copy whose only hold was cancelled goes back on the shelf
            Given Ana placed a hold on the hardback of "The Hobbit" at 2027-01-04T09:00:00Z
            And Ana's hold was cancelled
            When a hardback copy of "The Hobbit" is returned at 2027-03-01T09:00:00Z
            Then the copy is back on the shelf
            And nobody is told anything
            And Ana's hold is still cancelled

        # Known defect, ignored until it is fixed (no ticket yet: raise one and name it here).
        # HoldRepository.FindWaitingHoldsAsync filters waiting holds on title only, not format, so the
        # paperback fills Ana's hold for the hardback.
        @ignore
        Scenario: A copy in a format nobody is waiting for goes back on the shelf
            Given Ana placed a hold on the hardback of "The Hobbit" at 2027-01-04T09:00:00Z
            When a paperback copy of "The Hobbit" is returned at 2027-03-01T09:00:00Z
            Then the copy is back on the shelf
            And nobody is told anything
            And Ana's hold is still waiting

    Rule: The member whose hold is filled is told it is ready, and can collect it until 20:00 UK time on the seventh day after the copy reaches the hold shelf

        Scenario Outline: A copy returned <why> can be collected until 20:00 UK time a week later
            Given Ana placed a hold on the hardback of "The Hobbit" at 2027-01-04T09:00:00Z
            When a hardback copy of "The Hobbit" is returned at <returned>
            Then Ana's hold is ready for collection with the copy until <collect by>
            And Ana is told the copy is ready

            # <why> only names each example; no step reads it.
            # <collect by> is in UK time with its offset, because that is how the holds API shows it.
            Examples:
                | why                                   | returned             | collect by                |
                | in winter                             | 2027-01-12T14:00:00Z | 2027-01-19T20:00:00+00:00 |
                | in summer                             | 2027-06-15T14:00:00Z | 2027-06-22T20:00:00+01:00 |
                | just after midnight UK time in summer | 2027-06-15T23:30:00Z | 2027-06-23T20:00:00+01:00 |

        @Randomised
        Scenario Outline: A copy returned <when> can be collected until 20:00 UK time on the seventh day after
            Given a random member placed a hold on a random title in a random format
            When a copy of that title in that format is returned <when>
            Then that member's hold is ready for collection with the copy until 20:00 UK time on the seventh day after it was returned
            And that member is told the copy is ready

            # <run> only repeats the scenario, so it runs again with a new seed; no step reads it.
            Examples:
                | when                              | run |
                | at a random time                  | 1   |
                | in the week before a clock change | 1   |
                | in the week before a clock change | 2   |
                | around midnight UK time           | 1   |
                | around midnight UK time           | 2   |
