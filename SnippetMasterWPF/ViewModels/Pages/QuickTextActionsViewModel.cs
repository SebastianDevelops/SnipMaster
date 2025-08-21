using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnippetMasterWPF.Services;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SnippetMasterWPF.ViewModels.Pages;

public partial class QuickTextActionsViewModel : ObservableObject
{
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private string _textContent = string.Empty;

    public QuickTextActionsViewModel(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [RelayCommand]
    private void ShowLoremIpsumDialog()
    {
        var input = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter number of words (1-1000):", 
            "Generate Lorem Ipsum", 
            "50");
        
        if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int wordCount) && wordCount > 0 && wordCount <= 1000)
        {
            TextContent = GenerateLoremIpsum(wordCount);
            _notificationService.ShowNotification("Generated", $"Lorem Ipsum with {wordCount} words");
        }
    }

    [RelayCommand]
    private void ShowPasswordDialog()
    {
        var input = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter password length (4-128):", 
            "Generate Password", 
            "12");
        
        if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int length) && length >= 4 && length <= 128)
        {
            var password = GeneratePassword(length, true, true, true);
            TextContent = password;
            _notificationService.ShowNotification("Generated", $"Password with {length} characters");
        }
    }

    [RelayCommand]
    private void GenerateUuid()
    {
        TextContent = Guid.NewGuid().ToString();
        _notificationService.ShowNotification("Generated", "Random UUID");
    }

    [RelayCommand]
    private void ToUpperCase()
    {
        if (!string.IsNullOrEmpty(TextContent))
        {
            TextContent = TextContent.ToUpper();
            _notificationService.ShowNotification("Transformed", "Text converted to UPPERCASE");
        }
    }

    [RelayCommand]
    private void ToLowerCase()
    {
        if (!string.IsNullOrEmpty(TextContent))
        {
            TextContent = TextContent.ToLower();
            _notificationService.ShowNotification("Transformed", "Text converted to lowercase");
        }
    }

    [RelayCommand]
    private void ToTitleCase()
    {
        if (!string.IsNullOrEmpty(TextContent))
        {
            TextContent = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(TextContent.ToLower());
            _notificationService.ShowNotification("Transformed", "Text converted to Title Case");
        }
    }

    [RelayCommand]
    private void ReverseText()
    {
        if (!string.IsNullOrEmpty(TextContent))
        {
            TextContent = new string(TextContent.Reverse().ToArray());
            _notificationService.ShowNotification("Transformed", "Text reversed");
        }
    }

    [RelayCommand]
    private void RemoveExtraSpaces()
    {
        if (!string.IsNullOrEmpty(TextContent))
        {
            TextContent = Regex.Replace(TextContent.Trim(), @"\s+", " ");
            _notificationService.ShowNotification("Cleaned", "Extra spaces removed");
        }
    }

    [RelayCommand]
    private void SortLines()
    {
        if (!string.IsNullOrEmpty(TextContent))
        {
            var lines = TextContent.Split('\n').Select(l => l.Trim()).OrderBy(l => l);
            TextContent = string.Join('\n', lines);
            _notificationService.ShowNotification("Sorted", "Lines sorted alphabetically");
        }
    }

    [RelayCommand]
    private void RemoveDuplicates()
    {
        if (!string.IsNullOrEmpty(TextContent))
        {
            var lines = TextContent.Split('\n').Select(l => l.Trim()).Distinct();
            TextContent = string.Join('\n', lines);
            _notificationService.ShowNotification("Cleaned", "Duplicate lines removed");
        }
    }

    [RelayCommand]
    private void CountWords()
    {
        if (!string.IsNullOrEmpty(TextContent))
        {
            var wordCount = TextContent.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var charCount = TextContent.Length;
            _notificationService.ShowNotification("Count", $"{wordCount} words, {charCount} characters");
        }
    }



    private string GenerateLoremIpsum(int wordCount)
    {
        var words = new[] { "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit", "sed", "do", "eiusmod", "tempor", "incididunt", "ut", "labore", "et", "dolore", "magna", "aliqua" };
        var random = new Random();
        var result = new StringBuilder();

        for (int i = 0; i < wordCount; i++)
        {
            if (i > 0) result.Append(" ");
            result.Append(words[random.Next(words.Length)]);
        }

        return result.ToString();
    }

    private string GeneratePassword(int length, bool includeUppercase, bool includeNumbers, bool includeSymbols)
    {
        var chars = "abcdefghijklmnopqrstuvwxyz";
        if (includeUppercase) chars += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        if (includeNumbers) chars += "0123456789";
        if (includeSymbols) chars += "!@#$%^&*()_+-=[]{}|;:,.<>?";

        var random = new Random();
        return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}