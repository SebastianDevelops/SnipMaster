using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Input;
using SnippetMasterWPF.Infrastructure.Mvvm;
using SnippetMasterWPF.Services;
using RelayCommand = SnippetMasterWPF.Infrastructure.Mvvm.RelayCommand;

namespace SnippetMasterWPF.ViewModels.Pages
{
    public partial class PdfEditorAiViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient;
        private readonly INotificationService _notificationService;
        
        [ObservableProperty]
        private string _email = string.Empty;
        
        [ObservableProperty]
        private bool _isSubmitting = false;

        public PdfEditorAiViewModel(HttpClient httpClient, INotificationService notificationService)
        {
            _httpClient = httpClient;
            _notificationService = notificationService;
        }

        public ICommand NotifyMeCommand => new RelayCommand(NotifyMe, CanNotifyMe);

        private async void NotifyMe()
        {
            if (string.IsNullOrWhiteSpace(Email) || !IsValidEmail(Email))
            {
                _notificationService.ShowError("Invalid Email", "Please enter a valid email address.");
                return;
            }

            IsSubmitting = true;
            
            try
            {
                var payload = new { email = Email };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("https://www.snipmaster.fun/api/notify", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var message = result.GetProperty("message").GetString();
                    _notificationService.ShowSuccess("Success", message);
                    Email = string.Empty;
                }
                else
                {
                    _notificationService.ShowError("Error", "Failed to subscribe. Please try again.");
                }
            }
            catch
            {
                _notificationService.ShowError("Network Error", "Please check your connection and try again.");
            }
            finally
            {
                IsSubmitting = false;
            }
        }
        
        private bool CanNotifyMe()
        {
            return !IsSubmitting;
        }
        
        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}