namespace SnippetMaster.Api.Constants;

public static class Prompts
{
    public const string TypoChecker = @"Analyse the text and fix potential typos and incorrect spelling.
                                        Keep the overall test and context exactly the same, only fixing what's requested above.
                                        Return only the corrected text and nothing else.
                                        If the text is already correct, return only the text and nothing else";
}