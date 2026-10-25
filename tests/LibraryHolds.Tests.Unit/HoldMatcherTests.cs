using FluentAssertions;
using LibraryHolds.Api.Holds;
using Moq;

namespace LibraryHolds.Tests.Unit;

public sealed class HoldMatcherTests
{
    private static readonly DateTimeOffset March1 = new(2027, 3, 1, 9, 0, 0, TimeSpan.Zero);

    private readonly Mock<IHoldRepository> _holds = new();

    /// <summary>
    /// The way unit tests of this kind are usually written. It's green, and it proves the matcher asks
    /// the repository the right question. It can't tell you whether the repository's query answers it.
    /// </summary>
    [Fact]
    public async Task Looks_for_waiting_holds_with_the_returned_copys_title_and_format()
    {
        _holds
            .Setup(h => h.FindWaitingHoldsAsync(It.IsAny<string>(), It.IsAny<Format>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Hold>());

        await new HoldMatcher(_holds.Object).FindHoldToFillAsync("The Hobbit", Format.Hardback, CancellationToken.None);

        _holds.Verify(h => h.FindWaitingHoldsAsync("The Hobbit", Format.Hardback, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Choosing between holds is logic in the matcher itself, so a unit test covers it fully.</summary>
    [Fact]
    public async Task Fills_the_hold_of_the_member_who_has_waited_longest()
    {
        var ana = WaitingHold("Ana", placedAt: March1);
        var ben = WaitingHold("Ben", placedAt: March1.AddDays(1));
        var cat = WaitingHold("Cat", placedAt: March1.AddDays(2));
        _holds
            .Setup(h => h.FindWaitingHoldsAsync("The Hobbit", Format.Hardback, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Hold> { cat, ana, ben });

        var hold = await new HoldMatcher(_holds.Object).FindHoldToFillAsync("The Hobbit", Format.Hardback, CancellationToken.None);

        hold.Should().BeSameAs(ana);
    }

    [Fact]
    public async Task Fills_no_hold_when_nobody_is_waiting()
    {
        _holds
            .Setup(h => h.FindWaitingHoldsAsync("The Hobbit", Format.Hardback, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Hold>());

        var hold = await new HoldMatcher(_holds.Object).FindHoldToFillAsync("The Hobbit", Format.Hardback, CancellationToken.None);

        hold.Should().BeNull();
    }

    private static Hold WaitingHold(string member, DateTimeOffset placedAt) => new()
    {
        Id = Guid.NewGuid(),
        MemberId = member,
        Title = "The Hobbit",
        Format = Format.Hardback,
        PlacedAt = placedAt
    };
}
