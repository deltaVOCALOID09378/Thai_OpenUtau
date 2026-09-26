# Version 0.2
import argparse
import sys
from datetime import datetime, timezone
from pathlib import Path

def generate_appcast(version: str, os_name: str, rid: str, file_name: str, tag: str = None) -> None:
    """
    Generates the Appcast XML file for OpenUtau updates.
    """
    # Use provided tag or fallback to the standard OpenUtau build tag format
    release_tag = tag if tag else f"build%2F{version}"
    
    # Generate timezone-aware datetime for standard XML pubDate formatting
    pub_date = datetime.now(timezone.utc).astimezone().strftime("%a, %d %b %Y %H:%M:%S %z")
    
    # Utilizing f-strings for cleaner and more readable XML construction
    xml_content = f"""<?xml version="1.0" encoding="utf-8"?>
<rss version="2.0" xmlns:sparkle="http://www.andymatuschak.org/xml-namespaces/sparkle">
<channel>
    <title>OpenUtau</title>
    <language>en</language>
    <item>
        <title>OpenUtau {version}</title>
        <pubDate>{pub_date}</pubDate>
        <enclosure url="https://github.com/stakira/OpenUtau/releases/download/{release_tag}/{file_name}"
                   sparkle:version="{version}"
                   sparkle:shortVersionString="{version}"
                   sparkle:os="{os_name}"
                   type="application/octet-stream"
                   sparkle:signature="" />
    </item>
</channel>
</rss>"""

    output_filename = f"appcast.{rid}.xml"
    output_path = Path(output_filename)
    
    try:
        output_path.write_text(xml_content, encoding='utf-8')
        print(f"[Success] Generated {output_filename} successfully.")
    except Exception as e:
        print(f"[Error] Failed to write {output_filename}: {e}")
        sys.exit(1) # Signal failure to CI/CD pipelines

def main() -> None:
    parser = argparse.ArgumentParser(description='Writes Appcast XML file for OpenUtau.')
    parser.add_argument('-v', '--version', help='Version number (e.g., 0.1.0)', required=True)
    parser.add_argument('-o', '--os', help='OS name (e.g., windows, macos, linux)', required=True)
    parser.add_argument('-r', '--rid', help='Runtime Identifier (e.g., win-x64)', required=True)
    parser.add_argument('-f', '--file', help='Release file name (e.g., OpenUtau-win-x64.zip)', required=True)
    parser.add_argument('-t', '--tag', help='GitHub Release Tag (defaults to build/%%2F[version])', default=None)
    
    args = parser.parse_args()
    
    generate_appcast(
        version=args.version,
        os_name=args.os,
        rid=args.rid,
        file_name=args.file,
        tag=args.tag
    )

if __name__ == '__main__':
    main()
