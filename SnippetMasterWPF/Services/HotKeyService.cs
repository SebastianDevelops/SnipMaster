using SnippetMasterWPF.Helpers.Hotkeys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SnippetMasterWPF.Services
{
    public class HotKeyService : IHotKeyService
    {
        public void RegisterHotkeys(Action snipAction)
        {
            try
            {
                HotKeysManager.AddHotkey(ModifierKeys.Alt, Key.Q, snipAction);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to register shortcut {ex.Message}", "Error");
            }
        }
    }

    public interface IHotKeyService
    {
        void RegisterHotkeys(Action snipAction);
    }
}
