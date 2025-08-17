using System.Diagnostics;
using DotnetGeminiSDK.Client.Interfaces;
using DotnetGeminiSDK.Model.Response;
using SnippetMaster.Api.Constants;

namespace SnippetMaster.Api.Services;

public class TextFormatterService(IGeminiClient geminiClient) : ITextFormatterService
{
    public async Task<string> FormatTextSnippet(string text)
    {
        try
        {
            var response = await geminiClient.TextPrompt(@$"{Prompts.TypoChecker} 
                                                                    ----------
                                                                    Text to correct
                                                                     ---------
                                                                     {text}");
            return VerifyLlmTextResponse(response, text);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            return text;
        }
    }

    private string VerifyLlmTextResponse(GeminiMessageResponse? message, string text)
    {
        if (message != null && message.Candidates.Count > 0)
        {
            var content = message.Candidates.FirstOrDefault()?.Content;
            var correctedText = content?.Parts.FirstOrDefault()?.Text;

            if (string.IsNullOrEmpty(correctedText))
            {
                return text;
            }
                
            return correctedText;
        }

        return text;
    }
}