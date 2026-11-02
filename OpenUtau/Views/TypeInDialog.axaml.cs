using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OpenUtau.App.Views {
    public partial class TypeInDialog : Window {
        public Action<string>? onFinish;
        bool finished;   // กันการกด OK หรือ Enter ซ้ำ

        public TypeInDialog() {
            InitializeComponent();
            OkButton.Click += OnOkClick;
            CancelButton.Click += (_, _) => Close();
        }

        public void SetText(string text) => TextBox.Text = text;

        public void SetPrompt(string prompt) {
            Prompt.Text = prompt;
            Prompt.IsVisible = !string.IsNullOrWhiteSpace(prompt);
        }

        protected override void OnOpened(EventArgs e) {
            base.OnOpened(e);
            TextBox.Focus();
            TextBox.SelectAll();   // เปิดมาพิมพ์ทับได้ทันที
        }

        void OnOkClick(object? sender, RoutedEventArgs e) {
            if (finished) return;
            finished = true;
            try {
                onFinish?.Invoke(TextBox.Text ?? string.Empty);
            } finally {
                Close();
            }
        }
    }
}