# ============================================================================
# Date         : 03/11/2026 (B.E. 2569 / A.D. 2026)
# File Name    : apply_all_user_requirements.py
# Version      : 1.0
# Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
# File Location: Tools/apply_all_user_requirements.py
# Description  : Applies all user requirements: User Data Path selection,
#                clean screen capture/recording without toolbar in output,
#                elapsed duration timer in PianoRoll, LoadingWindow with 'X'
#                and "กรุณา รอสักครู่..........", and rich transparent notifications.
# ============================================================================

import os
import sys

BASE_DIR = r"d:\Program Developing\Thai_OpenUtau"

def update_file(rel_path, search_str, replace_str, description):
    full_path = os.path.join(BASE_DIR, rel_path)
    if not os.path.exists(full_path):
        print(f"[ERROR] File not found: {full_path}")
        return False
    with open(full_path, "r", encoding="utf-8") as f:
        content = f.read()
    if search_str not in content:
        print(f"[SKIP/WARN] Target not found in {rel_path} for: {description}")
        return False
    new_content = content.replace(search_str, replace_str, 1)
    with open(full_path, "w", encoding="utf-8") as f:
        f.write(new_content)
    print(f"[SUCCESS] Updated {rel_path} ({description})")
    return True

print("=== Starting DELTA SYNTH User Requirements Updates ===")

# 1. Update PreferencesViewModel.cs
vm_path = r"OpenUtau\ViewModels\PreferencesViewModel.cs"
with open(os.path.join(BASE_DIR, vm_path), "r", encoding="utf-8") as f:
    vm_code = f.read()

# Add UserDataPath properties
if "public string UserDataPath" not in vm_code:
    target = "        public string SingerPath => PathManager.Inst.SingersPath;"
    replacement = """        // DELTA SYNTH: User data path selection & relocation
        public string UserDataPath => PathManager.Inst.DataPath;
        public bool IsCustomUserDataPath => PathManager.Inst.IsCustomDataPath;
        public string SingerPath => PathManager.Inst.SingersPath;"""
    if target in vm_code:
        vm_code = vm_code.replace(target, replacement, 1)
        print("[SUCCESS] Added UserDataPath properties to PreferencesViewModel.cs")

# Add SetUserDataPath and ResetUserDataPath methods
if "public void SetUserDataPath" not in vm_code:
    target = "        public void SetAddlSingersPath(string path) {"
    replacement = """        public void SetUserDataPath(string path) {
            try {
                PathManager.SetCustomDataPath(path);
                this.RaisePropertyChanged(nameof(UserDataPath));
                this.RaisePropertyChanged(nameof(IsCustomUserDataPath));
                this.RaisePropertyChanged(nameof(SingerPath));
                DocManager.Inst.ExecuteCmd(new UserMessageNotification($"📁 กำหนดที่อยู่ฐานข้อมูลผู้ใช้ใหม่เรียบร้อยแล้ว: {path} (โปรดรีสตาร์ทโปรแกรมเพื่อให้มีผลสมบูรณ์)"));
            } catch (Exception ex) {
                Serilog.Log.Error(ex, "Failed to set custom user data path");
                DocManager.Inst.ExecuteCmd(new UserMessageNotification("เกิดข้อผิดพลาดในการตั้งค่าที่อยู่ฐานข้อมูล"));
            }
        }

        public void ResetUserDataPath() {
            try {
                PathManager.SetCustomDataPath(null);
                this.RaisePropertyChanged(nameof(UserDataPath));
                this.RaisePropertyChanged(nameof(IsCustomUserDataPath));
                this.RaisePropertyChanged(nameof(SingerPath));
                DocManager.Inst.ExecuteCmd(new UserMessageNotification("📁 รีเซ็ตที่อยู่ฐานข้อมูลกลับสู่ค่าเริ่มต้นเรียบร้อยแล้ว (โปรดรีสตาร์ทโปรแกรมเพื่อให้มีผลสมบูรณ์)"));
            } catch (Exception ex) {
                Serilog.Log.Error(ex, "Failed to reset user data path");
            }
        }

        public void SetAddlSingersPath(string path) {"""
    if target in vm_code:
        vm_code = vm_code.replace(target, replacement, 1)
        print("[SUCCESS] Added SetUserDataPath/ResetUserDataPath methods to PreferencesViewModel.cs")

with open(os.path.join(BASE_DIR, vm_path), "w", encoding="utf-8") as f:
    f.write(vm_code)

# 2. Update PreferencesDialog.axaml
pref_xaml = r"OpenUtau\Views\PreferencesDialog.axaml"
with open(os.path.join(BASE_DIR, pref_xaml), "r", encoding="utf-8") as f:
    pref_xaml_code = f.read()

if "prefs.paths.data" not in pref_xaml_code:
    target = """        <!-- Paths -->
        <StackPanel>
          <TextBlock Text="{DynamicResource prefs.paths.singer}" FontWeight="Bold"/>"""
    replacement = """        <!-- Paths -->
        <StackPanel>
          <TextBlock Text="{DynamicResource prefs.paths.data}" FontWeight="Bold"/>
          <Grid ColumnDefinitions="*,10,Auto" Margin="4,0,0,0" HorizontalAlignment="Stretch">
            <TextBlock TextWrapping="Wrap" FontSize="11" Text="{Binding UserDataPath}"/>
            <Button Content="{DynamicResource button.open}" Grid.Column="2" Click="OpenUserDataFolder"/>
          </Grid>
          <Grid HorizontalAlignment="Stretch" ColumnDefinitions="*,10,*" Margin="0,5,0,10">
            <Button Grid.Column="0" Content="{DynamicResource button.reset}"
                    HorizontalAlignment="Stretch" Click="ResetUserDataPath"/>
            <Button Grid.Column="2" Content="{DynamicResource button.select}"
                    HorizontalAlignment="Stretch" Click="SelectUserDataPath"/>
          </Grid>
          <TextBlock Text="{DynamicResource prefs.paths.singer}" FontWeight="Bold"/>"""
    if target in pref_xaml_code:
        pref_xaml_code = pref_xaml_code.replace(target, replacement, 1)
        with open(os.path.join(BASE_DIR, pref_xaml), "w", encoding="utf-8") as f:
            f.write(pref_xaml_code)
        print("[SUCCESS] Added User Data Path section to PreferencesDialog.axaml")

# 3. Update PreferencesDialog.axaml.cs
pref_cs = r"OpenUtau\Views\PreferencesDialog.axaml.cs"
with open(os.path.join(BASE_DIR, pref_cs), "r", encoding="utf-8") as f:
    pref_cs_code = f.read()

if "OpenUserDataFolder" not in pref_cs_code:
    target = "        void OpenSingersFolder(object sender, RoutedEventArgs e) {"
    replacement = """        void OpenUserDataFolder(object sender, RoutedEventArgs e) {
            try {
                Directory.CreateDirectory(PathManager.Inst.DataPath);
                OS.OpenFolder(PathManager.Inst.DataPath);
            } catch (Exception ex) {
                DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(ex));
            }
        }

        void ResetUserDataPath(object sender, RoutedEventArgs e) {
            viewModel?.ResetUserDataPath();
        }

        async void SelectUserDataPath(object sender, RoutedEventArgs e) {
            var path = await FilePicker.OpenFolder(this, "prefs.paths.data", viewModel?.UserDataPath);
            if (string.IsNullOrEmpty(path)) {
                return;
            }
            if (Directory.Exists(path)) {
                viewModel?.SetUserDataPath(path);
            }
        }

        void OpenSingersFolder(object sender, RoutedEventArgs e) {"""
    if target in pref_cs_code:
        pref_cs_code = pref_cs_code.replace(target, replacement, 1)
        with open(os.path.join(BASE_DIR, pref_cs), "w", encoding="utf-8") as f:
            f.write(pref_cs_code)
        print("[SUCCESS] Added Open/Reset/Select UserDataPath to PreferencesDialog.axaml.cs")

# 4. Update Localization Strings
strings_en = r"OpenUtau\Strings\Strings.axaml"
with open(os.path.join(BASE_DIR, strings_en), "r", encoding="utf-8") as f:
    sen = f.read()
if "prefs.paths.data" not in sen:
    target = '<system:String x:Key="prefs.paths.singer">Singers Location</system:String>'
    replacement = '<system:String x:Key="prefs.paths.data">User Data Location (Singers, Dictionaries, Plugins, etc.)</system:String>\n  ' + target
    if target in sen:
        sen = sen.replace(target, replacement, 1)
        with open(os.path.join(BASE_DIR, strings_en), "w", encoding="utf-8") as f:
            f.write(sen)
        print("[SUCCESS] Added prefs.paths.data to Strings.axaml")

strings_th = r"OpenUtau\Strings\Strings.th-TH.axaml"
with open(os.path.join(BASE_DIR, strings_th), "r", encoding="utf-8") as f:
    sth = f.read()
if "prefs.paths.data" not in sth:
    target = '<system:String x:Key="prefs.paths.singer">ตำแหน่งที่ตั้งของ Singers</system:String>'
    replacement = '<system:String x:Key="prefs.paths.data">ตำแหน่งฐานข้อมูลผู้ใช้ (Singers, Dictionaries, Plugins ฯลฯ)</system:String>\n  ' + target
    if target in sth:
        sth = sth.replace(target, replacement, 1)
        with open(os.path.join(BASE_DIR, strings_th), "w", encoding="utf-8") as f:
            f.write(sth)
        print("[SUCCESS] Added prefs.paths.data to Strings.th-TH.axaml")

# 5. Update LoadingWindow.axaml
lw_xaml = r"OpenUtau\Views\LoadingWindow.axaml"
with open(os.path.join(BASE_DIR, lw_xaml), "r", encoding="utf-8") as f:
    lw_code = f.read()

# Add X button
if "OnCloseButtonClick" not in lw_code:
    target = """    <Grid RowDefinitions="Auto, Auto, Auto, Auto">
        
        <!-- โลโก้แบรนด์ (ปรับให้เรนเดอร์ภาพคมชัดสูง) -->"""
    replacement = """    <Grid RowDefinitions="Auto, Auto, Auto, Auto">
        <!-- ปุ่มกากบาท (X) ปิดหน้าต่างป๊อปอัพที่มุมขวาบน -->
        <Button Grid.Row="0" HorizontalAlignment="Right" VerticalAlignment="Top"
                Width="28" Height="28" Background="Transparent" BorderThickness="0"
                Margin="0,-15,-25,0" Cursor="Hand" Click="OnCloseButtonClick"
                ToolTip.Tip="ปิดหน้าต่างโหลด / Dismiss Loading">
            <Button.Styles>
                <Style Selector="Button:pointerover">
                    <Setter Property="Background" Value="#33FF4422"/>
                </Style>
            </Button.Styles>
            <Path Data="M 6.4 5 L 5 6.4 L 10.6 12 L 5 17.6 L 6.4 19 L 12 13.4 L 17.6 19 L 19 17.6 L 13.4 12 L 19 6.4 L 17.6 5 L 12 10.6 Z"
                  Fill="#F0F0F0" Stretch="Uniform" Width="11" Height="11"/>
        </Button>
        
        <!-- โลโก้แบรนด์ (ปรับให้เรนเดอร์ภาพคมชัดสูง) -->"""
    if target in lw_code:
        lw_code = lw_code.replace(target, replacement, 1)
        with open(os.path.join(BASE_DIR, lw_xaml), "w", encoding="utf-8") as f:
            f.write(lw_code)
        print("[SUCCESS] Added 'X' button to LoadingWindow.axaml")

# 6. Update LoadingWindow.axaml.cs
lw_cs = r"OpenUtau\Views\LoadingWindow.axaml.cs"
with open(os.path.join(BASE_DIR, lw_cs), "r", encoding="utf-8") as f:
    lw_cs_code = f.read()

if "FormatReason" not in lw_cs_code:
    target = '            loadingDialog.Text.Text = "กำลังโหลดระบบ... / Loading...";'
    replacement = '''            loadingDialog.Text.Text = "ระบบกำลังเตรียมข้อมูลสำหรับการทำงาน...\\n\\nกรุณา รอสักครู่..........";
        }

        public void OnCloseButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) {
            CloseLoadingWindow();
        }

        public static string FormatReason(string? loadObject) {
            string obj = (loadObject ?? string.Empty).ToLowerInvariant().Trim();
            if (obj.Contains("singer") || obj.Contains("นักร้อง")) {
                return "ระบบกำลังโหลดข้อมูลนักร้องและเสียงพากย์...";
            }
            if (obj.Contains("project") || obj.Contains("โปรเจกต์") || obj.Contains("track") || obj.Contains("แทร็ก")) {
                return "ระบบกำลังโหลดข้อมูลโปรเจกต์และแทร็กเสียง...";
            }
            if (obj.Contains("oto")) {
                return "ระบบกำลังโหลดข้อมูลการตั้งค่า Oto.ini...";
            }
            if (obj.Contains("plugin") || obj.Contains("ปลั๊กอิน")) {
                return "ระบบกำลังประมวลผลปลั๊กอินคำสั่ง...";
            }
            return !string.IsNullOrWhiteSpace(loadObject) ? $"ระบบกำลังโหลดข้อมูล: {loadObject}" : "ระบบกำลังเตรียมข้อมูลสำหรับการทำงาน...";
        }

        public static void SetLoadingStatus(string reason, string? detail = null) {
            string bottomLine = "กรุณา รอสักครู่..........";
            string msg = string.IsNullOrWhiteSpace(detail)
                ? $"{reason}\\n\\n{bottomLine}"
                : $"{reason}\\n{detail}\\n\\n{bottomLine}";
            if (loadingDialog != null) {
                loadingDialog.Text.Text = msg;
            }'''
    if target in lw_cs_code:
        lw_cs_code = lw_cs_code.replace(target, replacement, 1)
        with open(os.path.join(BASE_DIR, lw_cs), "w", encoding="utf-8") as f:
            f.write(lw_cs_code)
        print("[SUCCESS] Enhanced LoadingWindow.axaml.cs with FormatReason, SetLoadingStatus, and OnCloseButtonClick")

# 7. Update ScreenCaptureService.cs (non-intrusive toolbar exclusion + duration timer)
sc_cs = r"OpenUtau\Capture\ScreenCaptureService.cs"
with open(os.path.join(BASE_DIR, sc_cs), "r", encoding="utf-8") as f:
    sc_code = f.read()

if "BeforeRenderFrame" not in sc_code:
    target = "        public Action<bool>? OnRecordingStateChanged { get; set; }"
    replacement = """        public Action<bool>? OnRecordingStateChanged { get; set; }
        public Action? BeforeRenderFrame { get; set; }
        public Action? AfterRenderFrame { get; set; }
        public Action<TimeSpan>? OnRecordingDurationTick { get; set; }
        private Stopwatch? recordingStopwatch;
        public TimeSpan ElapsedTime => recordingStopwatch?.Elapsed ?? TimeSpan.Zero;"""
    if target in sc_code:
        sc_code = sc_code.replace(target, replacement, 1)

# In CapturePictureAsync:
if "BeforeRenderFrame?.Invoke();" not in sc_code:
    target = """                using var rtb = new RenderTargetBitmap(new PixelSize(width, height), dpi);
                rtb.Render(control);
                await Task.Run(() => rtb.Save(fullPath));"""
    replacement = """                using var rtb = new RenderTargetBitmap(new PixelSize(width, height), dpi);
                BeforeRenderFrame?.Invoke();
                try {
                    rtb.Render(control);
                } finally {
                    AfterRenderFrame?.Invoke();
                }
                await Task.Run(() => rtb.Save(fullPath));"""
    if target in sc_code:
        sc_code = sc_code.replace(target, replacement, 1)

# In StartRecording:
if "recordingStopwatch = Stopwatch.StartNew();" not in sc_code:
    target = "                isRecording = true;"
    replacement = """                isRecording = true;
                recordingStopwatch = Stopwatch.StartNew();"""
    if target in sc_code:
        sc_code = sc_code.replace(target, replacement, 1)

# In recordTimer.Tick:
if "AfterRenderFrame?.Invoke();" not in sc_code:
    target = """                    try {
                        using var rtb = new RenderTargetBitmap(new PixelSize(width, height), dpi);
                        rtb.Render(targetControl);
                        using var ms = new MemoryStream();
                        rtb.Save(ms);
                        frameQueue.TryAdd(ms.ToArray());
                    } catch (Exception ex) {"""
    replacement = """                    try {
                        using var rtb = new RenderTargetBitmap(new PixelSize(width, height), dpi);
                        BeforeRenderFrame?.Invoke();
                        try {
                            rtb.Render(targetControl);
                        } finally {
                            AfterRenderFrame?.Invoke();
                        }
                        using var ms = new MemoryStream();
                        rtb.Save(ms);
                        frameQueue.TryAdd(ms.ToArray());
                        if (recordingStopwatch != null) {
                            OnRecordingDurationTick?.Invoke(recordingStopwatch.Elapsed);
                        }
                    } catch (Exception ex) {"""
    if target in sc_code:
        sc_code = sc_code.replace(target, replacement, 1)

# In StopRecordingAsync:
if "recordingStopwatch?.Stop();" not in sc_code:
    target = """            recordTimer?.Stop();
            recordTimer = null;"""
    replacement = """            recordTimer?.Stop();
            recordTimer = null;
            recordingStopwatch?.Stop();
            recordingStopwatch = null;"""
    if target in sc_code:
        sc_code = sc_code.replace(target, replacement, 1)

with open(os.path.join(BASE_DIR, sc_cs), "w", encoding="utf-8") as f:
    f.write(sc_code)
print("[SUCCESS] Updated ScreenCaptureService.cs with Before/After render exclusion and duration timer")

# 8. Update PianoRoll.axaml (Toolbar naming + Elapsed Duration Display)
pr_xaml = r"OpenUtau\Controls\PianoRoll.axaml"
with open(os.path.join(BASE_DIR, pr_xaml), "r", encoding="utf-8") as f:
    pr_xaml_code = f.read()

if 'x:Name="CaptureToolbarBorder"' not in pr_xaml_code:
    target = """    <!-- Floating Transparent Screen Capture & Video Toolbar (Bottom-Right Corner) -->
    <Border Grid.Column="0" Grid.ColumnSpan="9"
            HorizontalAlignment="Right" VerticalAlignment="Bottom"
            Margin="0,0,24,24" ZIndex="1000"
            Background="#66202025" CornerRadius="8"
            BorderBrush="#40FFFFFF" BorderThickness="1"
            BoxShadow="0 4 12 0 #40000000">
      <StackPanel Orientation="Horizontal" Spacing="4" Margin="4">"""
    replacement = """    <!-- Floating Transparent Screen Capture & Video Toolbar (Bottom-Right Corner) -->
    <Border x:Name="CaptureToolbarBorder" Grid.Column="0" Grid.ColumnSpan="9"
            HorizontalAlignment="Right" VerticalAlignment="Bottom"
            Margin="0,0,24,24" ZIndex="1000"
            Background="#66202025" CornerRadius="8"
            BorderBrush="#40FFFFFF" BorderThickness="1"
            BoxShadow="0 4 12 0 #40000000">
      <StackPanel Orientation="Horizontal" Spacing="4" Margin="6,4">
        <!-- Duration Display (Visible only when recording, excluded from recorded output) -->
        <TextBlock x:Name="CaptureDurationText"
                   VerticalAlignment="Center"
                   FontFamily="Leelawadee UI, sans-serif"
                   FontSize="12" FontWeight="SemiBold"
                   Foreground="#F0F0F0"
                   Margin="4,0,2,0"
                   IsVisible="False"
                   Text="00:00"/>"""
    if target in pr_xaml_code:
        pr_xaml_code = pr_xaml_code.replace(target, replacement, 1)
        with open(os.path.join(BASE_DIR, pr_xaml), "w", encoding="utf-8") as f:
            f.write(pr_xaml_code)
        print("[SUCCESS] Updated PianoRoll.axaml with CaptureToolbarBorder and CaptureDurationText")

# 9. Update PianoRoll.axaml.cs
pr_cs = r"OpenUtau\Controls\PianoRoll.axaml.cs"
with open(os.path.join(BASE_DIR, pr_cs), "r", encoding="utf-8") as f:
    pr_cs_code = f.read()

# Setup ScreenCaptureService callbacks
if "CaptureToolbarBorder" not in pr_cs_code:
    target = """        #region Screen Capture & Video Recording Handlers
        private async void OnCapturePhotoClick(object? sender, RoutedEventArgs e) {"""
    replacement = """        #region Screen Capture & Video Recording Handlers
        private void InitializeCaptureServiceHooks() {
            ScreenCaptureService.Inst.BeforeRenderFrame = () => {
                var toolbar = this.FindControl<Border>("CaptureToolbarBorder");
                if (toolbar != null) {
                    toolbar.IsVisible = false;
                }
            };
            ScreenCaptureService.Inst.AfterRenderFrame = () => {
                var toolbar = this.FindControl<Border>("CaptureToolbarBorder");
                if (toolbar != null) {
                    toolbar.IsVisible = true;
                }
            };
            ScreenCaptureService.Inst.OnRecordingDurationTick = elapsed => {
                Dispatcher.UIThread.Post(() => {
                    var txt = this.FindControl<TextBlock>("CaptureDurationText");
                    if (txt != null) {
                        txt.Text = string.Format("{0:00}:{1:00}", (int)elapsed.TotalMinutes, elapsed.Seconds);
                        txt.IsVisible = true;
                    }
                });
            };
        }

        private async void OnCapturePhotoClick(object? sender, RoutedEventArgs e) {
            InitializeCaptureServiceHooks();"""
    if target in pr_cs_code:
        pr_cs_code = pr_cs_code.replace(target, replacement, 1)

    target_video = """        private async void OnCaptureVideoClick(object? sender, RoutedEventArgs e) {"""
    replacement_video = """        private async void OnCaptureVideoClick(object? sender, RoutedEventArgs e) {
            InitializeCaptureServiceHooks();"""
    if target_video in pr_cs_code:
        pr_cs_code = pr_cs_code.replace(target_video, replacement_video, 1)

    target_update = """                var dot = this.FindControl<Avalonia.Controls.Shapes.Ellipse>("CaptureRecordingDot");
                if (dot != null) {
                    dot.IsVisible = isRecording;
                }"""
    replacement_update = """                var dot = this.FindControl<Avalonia.Controls.Shapes.Ellipse>("CaptureRecordingDot");
                if (dot != null) {
                    dot.IsVisible = isRecording;
                }
                var durationText = this.FindControl<TextBlock>("CaptureDurationText");
                if (durationText != null) {
                    durationText.IsVisible = isRecording;
                    if (!isRecording) {
                        durationText.Text = "00:00";
                    }
                }"""
    if target_update in pr_cs_code:
        pr_cs_code = pr_cs_code.replace(target_update, replacement_update, 1)

    with open(os.path.join(BASE_DIR, pr_cs), "w", encoding="utf-8") as f:
        f.write(pr_cs_code)
    print("[SUCCESS] Updated PianoRoll.axaml.cs with toolbar exclusion and duration updates")

# 10. Update ToastViewModel.cs (longer duration for errors, friendly clarifying messages)
toast_cs = r"OpenUtau\ViewModels\ToastViewModel.cs"
with open(os.path.join(BASE_DIR, toast_cs), "r", encoding="utf-8") as f:
    toast_code = f.read()

if "ClarifyMessage" not in toast_code:
    target = """        // ShowMessage — แสดง Toast แล้วลบออกอัตโนมัติหลัง 1 วินาที
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
                };"""
    replacement = """        // ClarifyMessage — ถอดรหัสข้อความเทคนิคให้อธิบายตรงไปตรงมา เข้าใจง่าย
        public static string ClarifyMessage(string rawMessage)
        {
            if (string.IsNullOrWhiteSpace(rawMessage)) return rawMessage;
            if (rawMessage.Contains("NoResamplerException") || rawMessage.Contains("Resampler"))
                return "⚠️ ไม่พบ Resampler ที่กำหนด: แนะนำตรวจสอบการตั้งค่า Resamplers หรือเลือกใช้ Worldline";
            if (rawMessage.Contains("NoWavtoolException") || rawMessage.Contains("Wavtool"))
                return "⚠️ ไม่พบ Wavtool: แนะนำตรวจสอบการตั้งค่า Wavtools ใน Preferences";
            if (rawMessage.Contains("UnauthorizedAccessException") || rawMessage.Contains("Access to the path is denied"))
                return "🔒 ไม่สามารถเขียนไฟล์ได้เนื่องจากสิทธิ์ของระบบ: โปรดตรวจสอบสิทธิ์โฟลเดอร์หรือพื้นที่จัดเก็บ";
            if (rawMessage.Contains("Sharing violation") || rawMessage.Contains("used by another process"))
                return "📄 ไฟล์กำลังถูกใช้งานโดยโปรแกรมอื่น: ระบบกำลังรอเพื่อลองใหม่อย่างปลอดภัย";
            if (rawMessage.Contains("FFmpeg not found"))
                return "⚠️ ไม่พบ ffmpeg.exe: โปรดวาง ffmpeg.exe ในโฟลเดอร์โปรแกรมเพื่อเปิดใช้การบันทึกวิดีโอ";
            return rawMessage;
        }

        // ShowMessage — แสดง Toast โปร่งใส แจ้งเตือนครบถ้วน พร้อมเวลาแสดงผลที่เหมาะสม
        public void ShowMessage(string message, string level)
        {
            string friendlyMessage = ClarifyMessage(message);
            Dispatcher.UIThread.Post(() =>
            {
                var toast = new ToastMessage(friendlyMessage, level);
                Messages.Add(toast);

                // ปรับระยะเวลา: 5s สำหรับ Error/Fatal เพื่อให้ผู้ใช้อ่านรายละเอียดได้ทัน, 3s สำหรับ Info/Warning
                double durationSeconds = (level == "Error" || level == "Fatal") ? 5.0 : 3.0;
                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(durationSeconds)
                };"""
    if target in toast_code:
        toast_code = toast_code.replace(target, replacement, 1)
        with open(os.path.join(BASE_DIR, toast_cs), "w", encoding="utf-8") as f:
            f.write(toast_code)
        print("[SUCCESS] Updated ToastViewModel.cs with ClarifyMessage and adaptive display duration")

# 11. Update MainWindowViewModel.cs to also forward UserMessageNotification to Toast
mvm_cs = r"OpenUtau\ViewModels\MainWindowViewModel.cs"
with open(os.path.join(BASE_DIR, mvm_cs), "r", encoding="utf-8") as f:
    mvm_code = f.read()

if "ToastViewModel.Inst.ShowMessage(userMsgNotification.Message" not in mvm_code:
    target = """            } else if (cmd is UserMessageNotification userMsgNotification) {
                Dispatcher.UIThread.InvokeAsync(() => {"""
    replacement = """            } else if (cmd is UserMessageNotification userMsgNotification) {
                ToastViewModel.Inst.ShowMessage(userMsgNotification.Message, "Information");
                Dispatcher.UIThread.InvokeAsync(() => {"""
    if target in mvm_code:
        mvm_code = mvm_code.replace(target, replacement, 1)
        with open(os.path.join(BASE_DIR, mvm_cs), "w", encoding="utf-8") as f:
            f.write(mvm_code)
        print("[SUCCESS] Connected UserMessageNotification to ToastViewModel in MainWindowViewModel.cs")

# 12. Update LoadingNotification handlers in MainWindow.axaml.cs to pass formatted reason
mw_cs = r"OpenUtau\Views\MainWindow.axaml.cs"
with open(os.path.join(BASE_DIR, mw_cs), "r", encoding="utf-8") as f:
    mw_code = f.read()

if "LoadingWindow.SetLoadingStatus" not in mw_code:
    target = """        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is SingersRefreshedNotification) {"""
    replacement = """        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is LoadingNotification loadingNotif && loadingNotif.window == typeof(MainWindow)) {
                if (loadingNotif.startLoading) {
                    LoadingWindow.SetLoadingStatus(LoadingWindow.FormatReason(loadingNotif.loadObject));
                    LoadingWindow.BeginLoading(this);
                } else {
                    LoadingWindow.EndLoading();
                }
            }
            if (cmd is SingersRefreshedNotification) {"""
    if target in mw_code:
        mw_code = mw_code.replace(target, replacement, 1)
        with open(os.path.join(BASE_DIR, mw_cs), "w", encoding="utf-8") as f:
            f.write(mw_code)
        print("[SUCCESS] Added LoadingNotification handler with FormatReason to MainWindow.axaml.cs")

# 13. Update PreferencesRecordPane.axaml to support 240p to 4K resolutions and Screenshot/Video folders
prp_xaml = r"OpenUtau\Views\PreferencesRecordPane.axaml"
with open(os.path.join(BASE_DIR, prp_xaml), "r", encoding="utf-8") as f:
    prp_code = f.read()

if "SelectedCaptureResolution" not in prp_code:
    target = """      <StackPanel Spacing="5">
        <TextBlock Text="Quality (Piano Roll only)"/>
        <ComboBox SelectedIndex="{Binding CaptureResolutionIndex}" HorizontalAlignment="Stretch">
          <ComboBoxItem Content="720p"/>
          <ComboBoxItem Content="1080p"/>
          <ComboBoxItem Content="2K"/>
          <ComboBoxItem Content="4K"/>
        </ComboBox>
      </StackPanel>"""
    replacement = """      <StackPanel Spacing="5">
        <TextBlock Text="Quality (Piano Roll: 240p - 4K)"/>
        <ComboBox ItemsSource="{Binding CaptureResolutions}" SelectedItem="{Binding SelectedCaptureResolution}" HorizontalAlignment="Stretch"/>
      </StackPanel>"""
    if target in prp_code:
        prp_code = prp_code.replace(target, replacement, 1)
        with open(os.path.join(BASE_DIR, prp_xaml), "w", encoding="utf-8") as f:
            f.write(prp_code)
        print("[SUCCESS] Updated PreferencesRecordPane.axaml with full 240p-4K resolution selection")

print("=== DELTA SYNTH User Requirements Updates Completed Successfully ===")
