// Version: 0.1
// Made and Checked By DELTA SYNTH And Gemini Claude and ChatGPT
// Original By OpenUtau Contributors

using OpenUtau.Core.DiffSinger;
using OpenUtau.Core.Util;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels
{
    // [DELTA SYNTH] จัดการการตั้งค่าสำหรับการบันทึกสคริปต์ส่งออกไปยัง DiffSinger
    public class DsScriptExportViewModel : ViewModelBase
    {
        [Reactive] public bool ExportPitch { get; set; } = true;
        [Reactive] public bool ExportVariance { get; set; } = false;
        public bool TensorCacheEnabled => Preferences.Default.DiffSingerTensorCache;

        public DsScriptExportOptions BuildOptions()
        {
            return new DsScriptExportOptions
            {
                exportPitch = ExportPitch,
                exportVariance = TensorCacheEnabled && ExportVariance,
            };
        }
    }
}