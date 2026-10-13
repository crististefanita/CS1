namespace Atf.Reporting.Extent.Tests;

public sealed class ExtentStepClassifierTests
{
    [Theory(DisplayName = "Step classifier recognizes lifecycle phases")]
    [InlineData("SETUP")]
    [InlineData("TEST BODY")]
    [InlineData("TEARDOWN")]
    public void RecognizesPhase(string title)
    {
        Assert.True(ExtentStepClassifier.IsPhase(title));
    }

    [Theory(DisplayName = "Step classifier recognizes HTTP request titles")]
    [InlineData("GET /users")]
    [InlineData("POST /users")]
    [InlineData("PATCH /users/1")]
    public void RecognizesHttpRequest(string title)
    {
        Assert.True(ExtentStepClassifier.IsHttp(title));
        Assert.False(ExtentStepClassifier.IsValidation(title));
    }

    [Theory(DisplayName = "Step classifier recognizes validation titles")]
    [InlineData("Validate: status=200")]
    [InlineData("validate: user contract")]
    public void RecognizesValidation(string title)
    {
        Assert.True(ExtentStepClassifier.IsValidation(title));
        Assert.False(ExtentStepClassifier.IsHttp(title));
    }
}
