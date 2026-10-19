# Check which files exist in upstream, then download missing ones with curl.exe
$apiUrl = 'https://api.github.com/repos/stakira/OpenUtau/contents/OpenUtau.Core/G2p/Data'
$headers = @{ 'User-Agent' = 'Thai-OpenUtau-Builder'; 'Accept' = 'application/vnd.github.v3+json' }
$items = Invoke-RestMethod -Uri $apiUrl -Headers $headers
$existingNames = $items | ForEach-Object { $_.name }
Write-Host "Files in upstream G2p/Data:"
$existingNames | ForEach-Object { Write-Host "  $_" }

$dest = 'A:\Thai_OpenUtau\OpenUtau.Core\G2p\Data'
$needed = @('g2p-brapa.zip', 'g2p-th.zip', 'g2p-uk.zip')  # g2p-fil.zip already downloaded

foreach ($f in $needed) {
    if ($existingNames -contains $f) {
        $item = $items | Where-Object { $_.name -eq $f }
        $dlUrl = $item.download_url
        Write-Host "Downloading $f from $dlUrl ..."
        $out = "$dest\$f"
        curl.exe -L -o $out $dlUrl
        if (Test-Path $out) {
            $size = (Get-Item $out).Length
            Write-Host "OK: $f ($size bytes)"
        } else {
            Write-Host "FAILED: $f - file not created"
        }
    } else {
        Write-Host "NOT IN UPSTREAM: $f - will need manual sourcing"
    }
}
Write-Host "Done."
