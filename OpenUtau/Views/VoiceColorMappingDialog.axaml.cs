/*
File: VoiceColorMappingDialog.xaml.cs
Version: 1.0
Made and Checked By DELTA SYNTH And Gemini Claude and ChatGPT
Original By OpenUtau Contributors
*/
using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace OpenUtau.App.Views
{
    public partial class VoiceColorMappingDialog : Window
    {
        // Event Delegate สำหรับดักจับการดำเนินการเมื่อจบหน้าต่าง
        public Action? onFinish;
        
        public bool Apply { get; private set; } = false;

        public VoiceColorMappingDialog()
        {
            InitializeComponent();
        }

        private void Ok_OnClick(object sender, RoutedEventArgs e) => Finish();

        private void Cancel_OnClick(object sender, RoutedEventArgs e) => Close();

        // เมื่อทำงานเสร็จสมบูรณ์ระบบจะเรียก Action คืนกลับไป (Delegate Pattern)
        private void Finish()
        {
            Apply = true;
            onFinish?.Invoke();
            Close();
        }

        // ดักจับการกดยกเลิกและตกลงผ่านคีย์บอร์ดโดยตรง
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close();
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                Finish();
            }
            else
            {
                base.OnKeyDown(e);
            }
        }
    }
}