using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace SnippetMasterWPF.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "https://localhost:7001"; // Update with your API URL

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> ProcessSnippetAsync(string text)
    {
        try
        {
            var request = new { Text = text };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/snippet/process", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ProcessSnippetResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                return result?.ProcessedText ?? text;
            }
        }
        catch
        {
            // Return original text on any error
        }
        
        return text;
    }

    private class ProcessSnippetResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ProcessedText { get; set; } = string.Empty;
    }
}