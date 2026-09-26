// Made And Checked By DELTA SYNTH & Gemini AI
// Original by Patiphat Wongyai (Delta)
// File: ToastViewModel.cs — v1.2 (15/07/2026)
// การแก้ไข: แก้ CS0104 ambiguous Color error — ใช้ Avalonia.Media.Color แทน Color สั้นๆ
using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using ReactiveUI;
// ไม่ import Avalonia.Media ทั้งหมด เพราะจะซ้อนทับกับ System.Drawing.Color
// ให้ใช้ Fully Qualified Name แทน

namespace OpenUtau.App.ViewModels
{
    // คลาสนี้เก็บข้อมูลของ Toast message แต่ละชิ้น (message, level, สี)
    public class ToastMessage : ReactiveObject
    {
        // Message — ข้อความที่จะแสดงใน Toast popup
        public string Message { get; }
        // Level — ระดับความรุนแรง: "Error", "Fatal", "Warning", "Information"
        public string Level { get; }
        // IsError — คืนค่า true เมื่อ Level คือ Error หรือ Fatal
        public bool IsError => Level == "Error" || Level == "Fatal";

        // Background — สีพื้นหลัง Toast ตาม DELTA SYNTH palette (แดง/ดำ โปร่งใส)
        public Avalonia.Media.IBrush Background { get; }
        // Foreground — สีข้อความ (ขาวเสมอ เพื่อความชัดเจนบน Dark Theme)
        public Avalonia.Media.IBrush Foreground { get; }

        // Constructor — สร้าง Toast พร้อมสีตาม DELTA SYNTH Theme
        public ToastMessage(string message, string level)
        {
            Message = message;
            Level = level;

            if (IsError)
            {
                // Error/Fatal: พื้นหลังแดงเข้ม DELTA SYNTH (#CC1A1A) opacity 88%
                // Alpha=224 (0xE0) = 88% opacity — เห็นชัดแต่ไม่บดบังพื้นที่ทำงาน
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(224, 0xCC, 0x1A, 0x1A));
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White);
            }
            else
            {
                // Warning/Info: พื้นหลังดำโปร่งใส (#1A1A1A) opacity 87%
                // Alpha=222 (0xDE) = 87% opacity — กลมกลืนกับ Dark Theme
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(222, 0x1A, 0x1A, 0x1A));
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White);
            }
        }
    }

    // คลาสหลักจัดการ Toast Notifications ทั้งหมด (Singleton)
    public class ToastViewModel : ViewModelBase
    {
        // Inst — global instance เดียว ใช้ร่วมกันทั้งโปรแกรม
        public static ToastViewModel Inst { get; } = new ToastViewModel();

        // Messages — collection ของ Toast ที่กำลังแสดง (Observable → UI อัปเดตอัตโนมัติ)
        public ObservableCollection<ToastMessage> Messages { get; } = new ObservableCollection<ToastMessage>();

        private ToastViewModel() { }

        // ShowMessage — แสดง Toast แล้วลบออกอัตโนมัติหลัง 1 วินาที
        public void ShowMessage(string message, string level)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var toast = new ToastMessage(message, level);
                Messages.Add(toast);

                // ตั้ง timer 1 วินาที (ลดจาก 2s เดิม ตาม DELTA SYNTH UX Standard)
                // Toast หายเร็ว ไม่รบกวนการทำงาน
                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                timer.Tick += (sender, e) =>
                {
                    timer.Stop();
                    if (Messages.Contains(toast))
                    {
                        Messages.Remove(toast);
                    }
                };
                timer.Start();
            });
        }

        // CloseMessage — ลบ Toast ทันทีเมื่อผู้ใช้กดปุ่ม X
        public void CloseMessage(ToastMessage message)
        {
            if (Messages.Contains(message))
            {
                Messages.Remove(message);
            }
        }

        // ClearAll — ลบ Toast ทั้งหมดพร้อมกัน
        public void ClearAll()
        {
            Messages.Clear();
        }
    }
}
