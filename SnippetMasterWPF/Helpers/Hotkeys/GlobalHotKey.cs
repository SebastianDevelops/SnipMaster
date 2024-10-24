using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SnippetMasterWPF.Helpers.Hotkeys
{
    public class GlobalHotKey
    {
        public ModifierKeys Modifier { get; set; }

        public Key Key { get; set; }

        public Action Callback { get; set; }

        public bool CanExecute { get; set; }

        public GlobalHotKey(ModifierKeys modifier, Key key, Action callback, bool canExecute = true)
        {
            Modifier = modifier;
            Key = key;
            Callback = callback;
            CanExecute = canExecute;
        }
    }
}
