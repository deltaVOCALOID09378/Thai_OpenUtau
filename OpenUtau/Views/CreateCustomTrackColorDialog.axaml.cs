using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using OpenUtau.App;
using OpenUtau.Colors;

namespace OpenUtau.App.Views;

public partial class CreateCustomTrackColorDialog : Window {
    public Func<string, Color, Color, bool>? onFinish;

    public CreateCustomTrackColorDialog() {
        InitializeComponent();
        OkButton.Click += (_, _) => Finish();
        CancelButton.Click += (_, _) => Close();
        NameBox.AttachedToVisualTree += (_, _) => {
            NameBox.SelectAll();
            NameBox.Focus();
        };
    }

    public void InitializeForCreate() {
        Title = ThemeManager.GetString("prefs.appearance.customtrackcolor.create.title");
        NameBox.Text = string.Empty;
        BaseColorPicker.Color = Color.Parse("#7266EE");
        BrightColorPicker.Color = Color.Parse("#B9B4F9");
    }

    public void InitializeForEdit(TrackColor color) {
        Title = ThemeManager.GetString("prefs.appearance.customtrackcolor.edit.title");
        var yaml = TrackColorYaml.LoadFromFile(color.StoragePath!);
        NameBox.Text = yaml.Name;
        BaseColorPicker.Color = Color.Parse(yaml.BaseColor);
        BrightColorPicker.Color = Color.Parse(yaml.BrightColor);
    }

    void Finish() {
        if (onFinish?.Invoke(NameBox.Text ?? string.Empty, BaseColorPicker.Color, BrightColorPicker.Color) != false) {
            Close();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e) {
        if (e.Key == Key.Escape) {
            e.Handled = true;
            Close();
        } else if (e.Key == Key.Enter) {
            e.Handled = true;
            Finish();
        } else {
            base.OnKeyDown(e);
        }
    }
}
