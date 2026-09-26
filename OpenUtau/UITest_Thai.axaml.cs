using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OpenUtau {
    public partial class UITest_Thai : Window {
        public UITest_Thai() {
            InitializeComponent();
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
