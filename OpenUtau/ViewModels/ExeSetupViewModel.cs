// Version: 0.1
// Made and Checked By DELTA SYNTH And Gemini Claude and ChatGPT
// Original By OpenUtau Contributors

using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels
{
    public class ExeSetupViewModel : ViewModelBase
    {
        public string filePath;
        [Reactive] public string message { get; set; }
        
        public ExeSetupViewModel(string filePath)
        {
            this.filePath = filePath;
            
            // [DELTA SYNTH] Guard Null: ป้องกัน ArgumentNullException หากข้อมูลจาก ThemeManager ไม่ถูกโหลดขึ้นมา
            string formatBase = ThemeManager.GetString("exesetup.installing") ?? "Installing \"{0}\"...";
            message = string.Format(formatBase, filePath);
            
            if (OS.IsMacOS())
            {
                message += "\n\n" + (ThemeManager.GetString("exesetup.mac") ?? "Follow setup steps for Mac.");
            }
            else if (OS.IsLinux())
            {
                string formatLinux = ThemeManager.GetString("exesetup.linux") ?? "Use Wine: {0}";
                message += "\n\n" + string.Format(formatLinux, "https://www.winehq.org/");
            }
        }
    }
}