$base = 'https://github.com/stakira/OpenUtau/raw/master/OpenUtau.Core/G2p/Data'
$dest = 'A:\Thai_OpenUtau\OpenUtau.Core\G2p\Data'
$files = @('g2p-brapa.zip', 'g2p-fil.zip', 'g2p-th.zip', 'g2p-uk.zip')

foreach ($f in $files) {
    $url = "$base/$f"
    $out = "$dest\$f"
    Write-Host "Downloading $f ..."
    try {
        Invoke-WebRequest -Uri $url -OutFile $out -UseBasicParsing
        $size = (Get-Item $out).Length
        Write-Host "OK: $f ($size bytes)"
    } catch {
        Write-Host "FAILED: $f - $($_.Exception.Message)"
    }
}

Write-Host "Done."
