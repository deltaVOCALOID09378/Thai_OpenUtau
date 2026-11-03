import os

BASE_DIR = r"d:\Program Developing\Thai_OpenUtau"

def patch(file_rel, old, new):
    p = os.path.join(BASE_DIR, file_rel)
    with open(p, "r", encoding="utf-8") as f:
        c = f.read()
    if old in c:
        c = c.replace(old, new, 1)
        with open(p, "w", encoding="utf-8") as f:
            f.write(c)
        print(f"Patched {file_rel}")
    else:
        print(f"Old text not found in {file_rel}")

# 1. SingersDialog.axaml.cs
patch(r"OpenUtau\Views\SingersDialog.axaml.cs",
      """            if (cmd is LoadingNotification loadingNotif && loadingNotif.window == typeof(SingersDialog)) {
                if (loadingNotif.startLoading) {
                    LoadingWindow.BeginLoading(this);""",
      """            if (cmd is LoadingNotification loadingNotif && loadingNotif.window == typeof(SingersDialog)) {
                if (loadingNotif.startLoading) {
                    LoadingWindow.SetLoadingStatus(LoadingWindow.FormatReason(loadingNotif.loadObject));
                    LoadingWindow.BeginLoading(this);""")

# 2. PianoRoll.axaml.cs
patch(r"OpenUtau\Controls\PianoRoll.axaml.cs",
      """        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is LoadingNotification loadingNotif && loadingNotif.window == typeof(PianoRoll)) {
                if (loadingNotif.startLoading) {
                    LoadingWindow.BeginLoadingImmediate(RootWindow);""",
      """        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is LoadingNotification loadingNotif && loadingNotif.window == typeof(PianoRoll)) {
                if (loadingNotif.startLoading) {
                    LoadingWindow.SetLoadingStatus(LoadingWindow.FormatReason(loadingNotif.loadObject));
                    LoadingWindow.BeginLoadingImmediate(RootWindow);""")

# 3. MainWindow.axaml.cs
patch(r"OpenUtau\Views\MainWindow.axaml.cs",
      """            LoadingWindow.BeginLoadingImmediate(this);
            var dialog = await Task.Run(() => lifetime.Windows.FirstOrDefault(w => w is SingersDialog));""",
      """            LoadingWindow.SetLoadingStatus(LoadingWindow.FormatReason("singer"));
            LoadingWindow.BeginLoadingImmediate(this);
            var dialog = await Task.Run(() => lifetime.Windows.FirstOrDefault(w => w is SingersDialog));""")

patch(r"OpenUtau\Views\MainWindow.axaml.cs",
      """                if (pianoRoll == null) {
                    LoadingWindow.BeginLoading(this);""",
      """                if (pianoRoll == null) {
                    LoadingWindow.SetLoadingStatus(LoadingWindow.FormatReason("track"));
                    LoadingWindow.BeginLoading(this);""")

# 4. PreferencesDialog.axaml.cs
patch(r"OpenUtau\Views\PreferencesDialog.axaml.cs",
      """        async void ReloadSingers(object sender, RoutedEventArgs e) {
            LoadingWindow.BeginLoading(this);""",
      """        async void ReloadSingers(object sender, RoutedEventArgs e) {
            LoadingWindow.SetLoadingStatus(LoadingWindow.FormatReason("singer"));
            LoadingWindow.BeginLoading(this);""")
