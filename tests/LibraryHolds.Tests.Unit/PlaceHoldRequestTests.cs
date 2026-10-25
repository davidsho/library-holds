using FluentAssertions;
using LibraryHolds.Api.Holds;

namespace LibraryHolds.Tests.Unit;

/// <summary>Input validation is local logic with no journey behind it, so it doesn't need the real app.</summary>
public sealed class PlaceHoldRequestTests
{
    [Fact]
    public void A_complete_request_is_valid() =>
        new PlaceHoldRequest("Ana", "The Hobbit", Format.Hardback).Validate().Should().BeEmpty();

    [Theory]
    [InlineData(null, "The Hobbit", Format.Hardback, nameof(PlaceHoldRequest.MemberId))]
    [InlineData(" ", "The Hobbit", Format.Hardback, nameof(PlaceHoldRequest.MemberId))]
    [InlineData("Ana", null, Format.Hardback, nameof(PlaceHoldRequest.Title))]
    [InlineData("Ana", "", Format.Hardback, nameof(PlaceHoldRequest.Title))]
    [InlineData("Ana", "The Hobbit", null, nameof(PlaceHoldRequest.Format))]
    public void A_request_missing_a_detail_says_which(string? memberId, string? title, Format? format, string missing) =>
        new PlaceHoldRequest(memberId, title, format).Validate().Should().ContainSingle().Which.Key.Should().Be(missing);
}
