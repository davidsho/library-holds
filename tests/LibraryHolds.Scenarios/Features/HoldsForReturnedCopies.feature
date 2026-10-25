@HoldsForReturnedCopies
Feature: Filling holds when copies are returned

    A member places a hold on a title, in a format, through the holds API. When a copy is
    returned, the API decides whether it fills a waiting hold. If it does, the copy goes on the
    hold shelf, the hold is ready for collection until a collect-by time, and the notification
    provider is asked to tell the member. If it doesn't, the copy goes back on the shelf and
    nobody is told anything.

    Scenarios tagged @Randomised repeat a rule's examples with random members, titles, formats
    and times. See README.md for replaying one.

    Rule: A returned copy goes to the member who has waited longest for that title and format

        Scenario: The member who placed their hold first gets the returned copy
            Given Ana placed a hold on "The Hobbit" in hardback at 2027-03-01T09:00:00Z
            And Ben placed a hold on "The Hobbit" in hardback at 2027-03-02T09:00:00Z
            When a copy of "The Hobbit" in hardback is returned at 2027-03-10T14:00:00Z
            Then Ana's hold is ready for collection
            And the copy is on the hold shelf for Ana
            And Ana is told the copy is ready
            And Ben's hold is still waiting

        @Randomised
        Scenario Outline: Of <members> members waiting for a random title, the one who placed their hold first gets the returned copy
            Given <members> members placed holds on a random title in a random format, one after another
            When a copy of that title in that format is returned at a random time
            Then the member who placed the first hold has it ready for collection
            And the member who placed the first hold is told the copy is ready
            And every other member's hold is still waiting

            # <run> only repeats the scenario, so it runs again with a new seed; no step reads it.
            Examples:
                | members | run |
                | 2       | 1   |
                | 3       | 2   |
                | 5       | 3   |

    Rule: A copy only fills a hold for the same title in the same format

        Cancelled holds, and holds that already have a copy waiting on the hold shelf, are skipped.

        Scenario Outline: A returned copy whose <detail> is <value> doesn't fill a hold for The Hobbit in hardback
            Given Ana placed a hold on "The Hobbit" in hardback at 2027-03-01T09:00:00Z
            When a copy is returned at 2027-03-10T14:00:00Z that matches Ana's hold except for its <detail>, which is <value>
            Then Ana's hold is still waiting
            And the copy is back on the shelf
            And nobody is told anything

            Examples:
                | detail | value            |
                | title  | The Silmarillion |
                | format | paperback        |
                | format | large print      |
                | format | audiobook        |

        Scenario: A cancelled hold is skipped
            Given Ana placed a hold on "The Hobbit" in hardback at 2027-03-01T09:00:00Z
            And Ben placed a hold on "The Hobbit" in hardback at 2027-03-02T09:00:00Z
            And Ana's hold is cancelled
            When a copy of "The Hobbit" in hardback is returned at 2027-03-10T14:00:00Z
            Then Ben's hold is ready for collection
            And Ben is told the copy is ready
            And Ana's hold is still cancelled

        Scenario: A hold that already has a copy waiting on the hold shelf is skipped
            Given Ana placed a hold on "The Hobbit" in hardback at 2027-03-01T09:00:00Z
            And Ben placed a hold on "The Hobbit" in hardback at 2027-03-02T09:00:00Z
            And a copy of "The Hobbit" in hardback is returned at 2027-03-05T10:00:00Z
            And Ana's hold is ready for collection
            When a copy of "The Hobbit" in hardback is returned at 2027-03-10T14:00:00Z
            Then Ben's hold is ready for collection
            And Ben is told the copy is ready
            And Ana's hold is still ready for collection with the copy returned at 2027-03-05T10:00:00Z

    Rule: The member can collect until closing time, 20:00 UK time, on the 7th day after the copy reaches the hold shelf

        Scenario Outline: A copy that reaches the hold shelf <when> can be collected until <collect by>
            Given Ana placed a hold on "The Hobbit" in hardback at 2027-01-04T09:00:00Z
            When a copy of "The Hobbit" in hardback is returned at <returned at>
            Then Ana's hold is ready for collection until <collect by>
            And Ana is told the copy is ready

            # <when> only names each example; no step reads it. <collect by> is in UK time, with the
            # offset the API sends, because the offset is part of the contract.
            Examples:
                | when                                  | returned at          | collect by                |
                | in winter                             | 2027-01-12T14:00:00Z | 2027-01-19T20:00:00+00:00 |
                | in summer                             | 2027-06-15T14:00:00Z | 2027-06-22T20:00:00+01:00 |
                | after closing time                    | 2027-01-12T21:30:00Z | 2027-01-19T20:00:00+00:00 |
                | in the last second of a UK summer day | 2027-06-15T22:59:59Z | 2027-06-22T20:00:00+01:00 |
                | in the first second of the next day   | 2027-06-15T23:00:00Z | 2027-06-23T20:00:00+01:00 |
                | the week before the clocks go forward | 2027-03-21T14:00:00Z | 2027-03-28T20:00:00+01:00 |
                | the week before the clocks go back    | 2027-10-24T13:00:00Z | 2027-10-31T20:00:00+00:00 |

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
                | at a random time                  | 2   |
                | in the week before a clock change | 3   |
                | in the week before a clock change | 4   |
                | around midnight UK time           | 5   |
                | around midnight UK time           | 6   |

    Rule: A copy that fills no hold goes back on the shelf and nobody is told anything

        Scenario: A copy nobody is waiting for goes back on the shelf
            Given Ana placed a hold on "The Silmarillion" in hardback at 2027-03-01T09:00:00Z
            When a copy of "The Hobbit" in hardback is returned at 2027-03-10T14:00:00Z
            Then the copy is back on the shelf
            And nobody is told anything
            And Ana's hold is still waiting
