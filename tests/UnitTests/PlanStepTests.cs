using Moq;
using Shouldly;
using Tarik.Application.Common;
using TiktokenSharp;
using Xunit;

namespace Tarik.UnitTests;

public class PlanStepTests
{
    [Fact]
    public async Task EditFilePlanStep_GetCreateFileStepPrompt_Output()
    {
        // Arrange
        TikToken tikToken = TikToken.EncodingForModel("gpt-4");
        var fileServiceMock = new Mock<IFileService>();
        fileServiceMock.Setup(x => x.DumpFiles(It.IsAny<List<PathTo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("relevant files");
        var plan = new Plan(ApprovedPlans.CalculatorPlan, "/path/to/local/directory");

        // Act
        var editFileStepPrompt = await plan.EditFileSteps[0].GetEditFileStepPrompt(plan, fileServiceMock.Object, "", CancellationToken.None);

        // Assert
        editFileStepPrompt.ShouldNotBeEmpty();
        var encoded = tikToken.Encode(editFileStepPrompt);
        encoded.Count.ShouldBeLessThan(2048); // No more than 25% of the tokens should be used for the prompt

        // Save the prompt to a file for manual inspection in OpenAI playground
        var path = Path.Combine(Directory.GetCurrentDirectory(), "editFileStepPrompt.txt");
        await File.WriteAllTextAsync(path, editFileStepPrompt);
    }
}