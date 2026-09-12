Add-Type -AssemblyName System.Drawing

$root = 'D:\Projects\frox\FROX'
$outDir = Join-Path $root '.tmp_panda_analysis'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$fw = 64
$fh = 85
$cols = 4
$rows = 4
$scale = 2

foreach ($name in @('panda-idle-sitting', 'panda-idle-sleeping', 'panda-thinking-forchatting')) {
    $src = Join-Path $root ("assets\PandaAssets\" + $name + ".png")
    $img = [System.Drawing.Image]::FromFile($src)
    $out = New-Object System.Drawing.Bitmap(($cols * $fw * $scale), ($rows * $fh * $scale))
    $g = [System.Drawing.Graphics]::FromImage($out)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $g.Clear([System.Drawing.Color]::FromArgb(255, 40, 40, 48))
    for ($i = 0; $i -lt 16; $i++) {
        $sr = New-Object System.Drawing.Rectangle(($i * $fw), 0, $fw, $fh)
        $dx = ($i % $cols) * $fw * $scale
        $dy = [math]::Floor($i / $cols) * $fh * $scale
        $dr = New-Object System.Drawing.Rectangle($dx, $dy, ($fw * $scale), ($fh * $scale))
        $g.DrawImage($img, $dr, $sr, [System.Drawing.GraphicsUnit]::Pixel)
        $g.DrawString([string]$i, (New-Object System.Drawing.Font('Consolas', 20, [System.Drawing.FontStyle]::Bold)), [System.Drawing.Brushes]::Yellow, $dx + 4, $dy + 4)
    }
    $g.Dispose()
    $out.Save((Join-Path $outDir ($name + '.montage.png')), [System.Drawing.Imaging.ImageFormat]::Png)
    $out.Dispose()
    $img.Dispose()
    Write-Host "done $name"
}
Write-Host 'ALL DONE'