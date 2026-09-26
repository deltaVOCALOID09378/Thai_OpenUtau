// Version: 0.1
// Made and Checked By DELTA SYNTH And Gemini Claude and ChatGPT
// Original By OpenUtau Contributors

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using Avalonia.Data.Converters;
using Avalonia.Threading;
using DynamicData.Binding;
using OpenUtau.App.Views;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace OpenUtau.App.ViewModels
{
    public class LogEventConverter : IValueConverter
    {
        private ITextFormatter formater;

        public LogEventConverter()
        {
            const string template = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
            formater = new MessageTemplateTextFormatter(template);
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is LogEvent logEvent)
            {
                // [DELTA SYNTH] การแก้ไขปัญหา Thread-Safety: สร้าง StringWriter ภายใน Scope ไม่แชร์กับ Thread อื่น เพื่อป้องกันข้อมูลขยะทับกันเวลา Log ทำงานหนัก
                using (var stringWriter = new StringWriter())
                {
                    formater.Format(logEvent, stringWriter);
                    return stringWriter.ToString();
                }
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return new Avalonia.Data.BindingNotification(new NotImplementedException(), Avalonia.Data.BindingErrorType.Error);
        }
    }

    public class DebugViewModel : ViewModelBase
    {
        private DebugWindow? window;

        public void SetWindow(DebugWindow w)
        {
            window = w;
        }

        public DebugViewModel()
        {
            ReverseLogOrderCommand = ReactiveCommand.Create(() => { Sink.Inst.ReverseOrder(); });
            CopyLogCommand = ReactiveCommand.Create(() =>
            {
                window?.CopyLogText();
            });
        }

        public class Sink : ILogEventSink
        {
            static Sink sink = new Sink();
            public static Sink Inst => sink;

            public LoggingLevelSwitch LevelSwitch = new LoggingLevelSwitch(LogEventLevel.Error);
            public ObservableCollectionExtended<LogEvent> LogEvents = new ObservableCollectionExtended<LogEvent>();

            private bool reverseLogOrder = Preferences.Default.ReverseLogOrder;

            public void ReverseOrder()
            {
                reverseLogOrder = !reverseLogOrder;
                reverseOrder();
            }

            private void reverseOrder()
            {
                // [DELTA SYNTH] Performance Optimization: ใช้ AddRange เพื่ออัปเดต UI ครั้งเดียวแทนการวนลูปทีละตัว ลดอาการหน่วง และทำผ่าน UIThread เสมอ
                Dispatcher.UIThread.Post(() =>
                {
                    var x = LogEvents.AsEnumerable().Reverse().ToArray();
                    LogEvents.Clear();
                    LogEvents.AddRange(x);
                });
            }

            public void ReverseOrder(bool reversed)
            {
                if (reversed != reverseLogOrder)
                {
                    ReverseOrder();
                }
            }

            public void Emit(LogEvent logEvent)
            {
                // [DELTA SYNTH] ป้องกัน Cross-Thread Exception: LogEvents เป็นข้อมูลเชื่อมกับ UI จึงต้องโยนให้ UI Thread เข้าถึงเท่านั้น
                Dispatcher.UIThread.Post(() =>
                {
                    if (reverseLogOrder)
                    {
                        LogEvents.Insert(0, logEvent);
                    }
                    else
                    {
                        LogEvents.Add(logEvent);
                    }
                });
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                foreach (var l in LogEvents)
                {
                    sb.AppendLine($"{l.Timestamp} : {l.Level} : {l.MessageTemplate.Text}");
                }
                return sb.ToString();
            }
        }

        [Reactive] public LogEventLevel LogEventLevel { get; set; }
        public ObservableCollection<LogEvent> LogEvents => Sink.Inst.LogEvents;
        public ReactiveCommand<Unit, Unit> ReverseLogOrderCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> CopyLogCommand { get; private set; }

        public void Clear()
        {
            Dispatcher.UIThread.Post(() => Sink.Inst.LogEvents.Clear());
        }

        public void Attach()
        {
            Core.Util.ProcessRunner.DebugSwitch = true;
            Sink.Inst.LevelSwitch.MinimumLevel = LogEventLevel.Verbose;
        }

        public void Detach()
        {
            Core.Util.ProcessRunner.DebugSwitch = false;
            Sink.Inst.LevelSwitch.MinimumLevel = LogEventLevel.Error;
        }
    }
}