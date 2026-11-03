with open(r'd:\Program Developing\Thai_OpenUtau\OpenUtau\Strings\Strings.axaml', 'r', encoding='utf-8') as f:
    en = f.read()
if 'prefs.paths.data' not in en:
    en = en.replace('<system:String x:Key="prefs.paths.singer">Singer Path</system:String>', '<system:String x:Key="prefs.paths.data">User Data Location (Singers, Dictionaries, Plugins, etc.)</system:String>\n  <system:String x:Key="prefs.paths.singer">Singer Path</system:String>')
    with open(r'd:\Program Developing\Thai_OpenUtau\OpenUtau\Strings\Strings.axaml', 'w', encoding='utf-8') as f:
        f.write(en)
    print('Updated Strings.axaml')

with open(r'd:\Program Developing\Thai_OpenUtau\OpenUtau\Strings\Strings.th-TH.axaml', 'r', encoding='utf-8') as f:
    th = f.read()
if 'prefs.paths.data' not in th:
    th = th.replace('<system:String x:Key="prefs.paths.singer">โฟลเดอร์เก็บคลังเสียงนักร้องหลัก (Singer Path)</system:String>', '<system:String x:Key="prefs.paths.data">ตำแหน่งฐานข้อมูลผู้ใช้หลัก (Singers, Dictionaries, Plugins ฯลฯ)</system:String>\n  <system:String x:Key="prefs.paths.singer">โฟลเดอร์เก็บคลังเสียงนักร้องหลัก (Singer Path)</system:String>')
    with open(r'd:\Program Developing\Thai_OpenUtau\OpenUtau\Strings\Strings.th-TH.axaml', 'w', encoding='utf-8') as f:
        f.write(th)
    print('Updated Strings.th-TH.axaml')
