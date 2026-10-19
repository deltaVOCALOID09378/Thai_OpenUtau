using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using OpenUtau.App;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.App.Helpers {
    public static class KeySignatureMenuHelper {
        static ContextMenu? openMenu;

        public static void OpenPicker(
            Control placementTarget,
            Func<IEnumerable<UNote>> noteSource,
            Action<MusicalKey> onSelected,
            MusicalKey? currentKey = null) {
            if (placementTarget == null) {
                return;
            }
            if (openMenu != null) {
                openMenu.Close();
                openMenu = null;
            }
            var menu = new ContextMenu {
                Placement = PlacementMode.BottomEdgeAlignedLeft,
            };
            menu.Closed += (_, _) => {
                if (openMenu == menu) {
                    openMenu = null;
                }
            };
            openMenu = menu;
            var analyzingItem = new MenuItem {
                Header = ThemeManager.GetString("key.picker.analyzing"),
                IsEnabled = false,
            };
            menu.Items.Add(analyzingItem);
            menu.Open(placementTarget);

            _ = Task.Run(() => {
                var notes = noteSource().ToList();
                var detected = KeySignatureHelper.DetectKeys(notes, 3);
                var current = currentKey ?? MusicalKey.FromProject(DocManager.Inst.Project);
                Dispatcher.UIThread.Post(() => PopulateMenu(menu, detected, current, onSelected));
            });
        }

        static void PopulateMenu(
            ContextMenu menu,
            IReadOnlyList<MusicalKey> detected,
            MusicalKey current,
            Action<MusicalKey> onSelected) {
            menu.Items.Clear();
            if (detected.Count > 0) {
                menu.Items.Add(CreateHeaderItem(ThemeManager.GetString("key.picker.suggested")));
                foreach (var key in detected) {
                    menu.Items.Add(CreateKeyItem(key, current, onSelected, suggested: true));
                }
                menu.Items.Add(new Separator());
            }
            menu.Items.Add(CreateHeaderItem(ThemeManager.GetString("key.picker.all")));
            foreach (var key in KeySignatureHelper.AllKeys()) {
                menu.Items.Add(CreateKeyItem(key, current, onSelected, suggested: false));
            }
        }

        static MenuItem CreateHeaderItem(string header) => new() {
            Header = header,
            IsEnabled = false,
            FontWeight = FontWeight.SemiBold,
        };

        static MenuItem CreateKeyItem(
            MusicalKey key,
            MusicalKey current,
            Action<MusicalKey> onSelected,
            bool suggested) {
            bool isCurrent = key == current;
            string label = KeySignatureHelper.FormatKey(key);
            if (suggested) {
                label = $"★ {label}";
            }
            return new MenuItem {
                Header = label,
                Icon = new CheckBox {
                    Classes = { "menu" },
                    IsChecked = isCurrent,
                },
                Command = ReactiveUI.ReactiveCommand.Create(() => onSelected(key)),
            };
        }
    }
}
