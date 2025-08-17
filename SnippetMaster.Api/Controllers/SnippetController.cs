using Microsoft.AspNetCore.Mvc;
using SnippetMaster.Api.Services;

namespace SnippetMaster.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SnippetController(ITextFormatterService textFormatterService) : ControllerBase
{
    [HttpPost("process")]
    public async Task<IActionResult> ProcessSnippet([FromBody] ProcessSnippetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest("Snippet text is required");
        }

        var processedText = textFormatterService.FormatTextSnippet(request.Text).Result;
        if (String.IsNullOrEmpty(processedText))
        {
            return BadRequest("Failed to process snippet");
        }
        
        return Ok(new ProcessSnippetResponse
        {
            Success = true,
            Message = "Snippet processed successfully",
            ProcessedText = processedText
        });
    }
}

public class ProcessSnippetRequest
{
    public string Text { get; set; } = string.Empty;
}

public class ProcessSnippetResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ProcessedText { get; set; } = string.Empty;
}