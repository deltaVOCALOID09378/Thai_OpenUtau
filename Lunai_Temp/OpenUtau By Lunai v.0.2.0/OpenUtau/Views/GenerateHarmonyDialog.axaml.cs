using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using OpenUtau.Core.Editing;
using OpenUtau.Core.Ustx;

namespace OpenUtau.App.Views {
    public partial class GenerateHarmonyDialog : Window {
        readonly GenerateHarmonyViewModel viewModel;
        readonly UVoicePart sourcePart;

        public GenerateHarmonyDialog(UVoicePart part) {
            sourcePart = part;
            viewModel = new GenerateHarmonyViewModel(part);
            InitializeComponent();
            DataContext = viewModel;
            KeyComboBox.ItemsSource = viewModel.KeyOptions;
            KeyComboBox.SelectedItem = viewModel.SelectedKeyOption;
        }

        void OnCancel(object? sender, RoutedEventArgs e) => Close(false);

        void OnOk(object? sender, RoutedEventArgs e) {
            var intervals = viewModel.GetSelectedIntervals();
            if (intervals.Count == 0) {
                return;
            }
            var key = viewModel.SelectedKeyOption?.Key ?? MusicalKey.FromProject(DocManager.Inst.Project);
            DocManager.Inst.StartUndoGroup("command.part.generateharmonies");
            HarmonyGenerator.CreateHarmonyTracks(DocManager.Inst.Project, sourcePart, key, intervals);
            DocManager.Inst.EndUndoGroup();
            Close(true);
        }
    }
}
