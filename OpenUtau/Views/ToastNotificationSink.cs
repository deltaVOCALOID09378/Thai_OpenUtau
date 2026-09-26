using OpenUtau.App.ViewModels;
using Serilog.Core;
using Serilog.Events;

namespace OpenUtau.App.Views
{
    public class ToastNotificationSink : ILogEventSink
    {
        public static readonly ToastNotificationSink Inst = new ToastNotificationSink();

        private ToastNotificationSink() { }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent.Level == LogEventLevel.Error || logEvent.Level == LogEventLevel.Warning || logEvent.Level == LogEventLevel.Fatal)
            {
                var message = logEvent.RenderMessage();
                if (logEvent.Exception != null)
                {
                    message += $"\n{logEvent.Exception.GetType().Name}: {logEvent.Exception.Message}";
                }
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    ToastViewModel.Inst.ShowMessage(message, logEvent.Level.ToString());
                });
            }
        }
    }
}
