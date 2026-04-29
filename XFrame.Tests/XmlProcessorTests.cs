using FluentAssertions;
using XFrame.Core.Services;

public class XmlProcessorTests
{
    private readonly XmlProcessorService _service = new();

    [Theory]
    [InlineData("150.50", 150.5)]    // Standard Dot
    [InlineData("150,50", 150.5)]    // European Comma
    [InlineData(" -10.25 ", -10.25)] // Negative & Whitespace
    [InlineData("invalid", 0)]       // Garbage input
    [InlineData("", 0)]              // Empty string
    [InlineData(null, 0)]            // Null safety
    [InlineData("123_987", 123987.0)]  // Modern underscore digit separator
    [InlineData("123_456_987", 123456987.0)]  // Modern underscore digit separators
    [InlineData("0.0000034500", 0.00000345)]  // High precision with redundant trailing zeros
    [InlineData("00876.00990", 876.0099)]    // Leading padding zeros and trailing precision zeros
    [InlineData("123.987.00030", 0)]        // Multiple dots
    // The US/UK Standard: The comma (,) is used as a Thousands Separator (grouping).
    // The dot (.) is used as the Decimal Separator.
    [InlineData("123,456.80", 123456.8)]
    [InlineData("123,456,789.0099", 123456789.0099)]
    public void ParseAmount_ShouldHandleVariousFormats(string? input, double expected)
    {
        // Act
        var result = _service.ParseAmount(input);

        // Assert
        result.Should().Be(expected);
    }
}