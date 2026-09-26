using Avalonia.Threading;
using OpenUtau.App.ViewModels;
using Serilog.Core;
using Serilog.Events;

namespace OpenUtau.App
{
    public class ToastLogSink : ILogEventSink
    {
        public static ToastLogSink Inst { get; } = new ToastLogSink();

        public void Emit(LogEvent logEvent)
        {
            if (logEvent.Level == LogEventLevel.Error || logEvent.Level == LogEventLevel.Fatal)
            {
                var message = logEvent.RenderMessage();
                if (logEvent.Exception != null)
                {
                    message += $"\n{logEvent.Exception.Message}";
                }

                Dispatcher.UIThread.Post(() =>
                {
                    ToastViewModel.Inst.ShowMessage(message, "Error");
                });
            }
            else if (logEvent.Level == LogEventLevel.Warning)
            {
                var message = logEvent.RenderMessage();
                Dispatcher.UIThread.Post(() =>
                {
                    ToastViewModel.Inst.ShowMessage(message, "Warning");
                });
            }
        }
    }
}