param(
    [string]$OutDir = (Join-Path $PSScriptRoot "..\src\WinServerOPT")
)

Add-Type -AssemblyName System.Drawing

function New-RoundedPath {
    param([System.Drawing.Rectangle]$Rect, [int]$Radius)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $Radius * 2
    $path.AddArc($Rect.X, $Rect.Y, $d, $d, 180, 90)
    $path.AddArc($Rect.Right - $d, $Rect.Y, $d, $d, 270, 90)
    $path.AddArc($Rect.Right - $d, $Rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($Rect.X, $Rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    return $path
}

function Draw-BrandIcon {
    param([int]$Size)

    $bmp = New-Object System.Drawing.Bitmap $Size, $Size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $margin = [int]($Size * 0.08)
    $rect = New-Object System.Drawing.Rectangle $margin, $margin, ($Size - 2 * $margin), ($Size - 2 * $margin)
    $radius = [int]($Size * 0.22)

    $c1 = [System.Drawing.Color]::FromArgb(255, 42, 140, 240)
    $c2 = [System.Drawing.Color]::FromArgb(255, 13, 91, 184)
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, $c1, $c2, 135.0
    $path = New-RoundedPath $rect $radius
    $g.FillPath($brush, $path)
    $brush.Dispose()
    $path.Dispose()

    # Windows 四格窗 + 优化横条（白）
    $cx = [int]($Size / 2)
    $cy = [int]($Size * 0.42)
    $pane = [int]($Size * 0.11)
    $gap = [int]($Size * 0.025)
    $white = [System.Drawing.Brushes]::White
    $g.FillRectangle($white, ($cx - $pane - $gap / 2), ($cy - $pane - $gap / 2), $pane, $pane)
    $g.FillRectangle($white, ($cx + $gap / 2), ($cy - $pane - $gap / 2), $pane, $pane)
    $g.FillRectangle($white, ($cx - $pane - $gap / 2), ($cy + $gap / 2), $pane, $pane)
    $g.FillRectangle($white, ($cx + $gap / 2), ($cy + $gap / 2), $pane, $pane)

    $barY = [int]($Size * 0.68)
    $barW = [int]($Size * 0.34)
    $barH = [Math]::Max(2, [int]($Size * 0.045))
    $barGap = [int]($Size * 0.06)
    $barX = $cx - [int]($barW / 2)
    $alpha = [System.Drawing.Color]::FromArgb(230, 255, 255, 255)
    $barBrush = New-Object System.Drawing.SolidBrush $alpha
    for ($i = 0; $i -lt 3; $i++) {
        $w = $barW - $i * [int]($Size * 0.05)
        $x = $barX + [int](($barW - $w) / 2)
        $y = $barY + $i * $barGap
        $g.FillRectangle($barBrush, $x, $y, $w, $barH)
    }
    $barBrush.Dispose()

    $g.Dispose()
    return $bmp
}

function Save-Ico {
    param([string]$Path, [System.Drawing.Bitmap[]]$Images)

    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter $ms
    $bw.Write([uint16]0)
    $bw.Write([uint16]1)
    $bw.Write([uint16]$Images.Length)

    $offset = 6 + (16 * $Images.Length)
    $pngData = New-Object System.Collections.Generic.List[byte[]]

    foreach ($img in $Images) {
        $s = New-Object System.IO.MemoryStream
        $img.Save($s, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngData.Add($s.ToArray()) | Out-Null
        $s.Dispose()
    }

    for ($i = 0; $i -lt $Images.Length; $i++) {
        $img = $Images[$i]
        $w = if ($img.Width -ge 256) { [byte]0 } else { [byte]$img.Width }
        $h = if ($img.Height -ge 256) { [byte]0 } else { [byte]$img.Height }
        $bw.Write([byte]$w)
        $bw.Write([byte]$h)
        $bw.Write([byte]0)
        $bw.Write([byte]0)
        $bw.Write([uint16]1)
        $bw.Write([uint16]32)
        $bw.Write([uint32]$pngData[$i].Length)
        $bw.Write([uint32]$offset)
        $offset += $pngData[$i].Length
    }

    foreach ($data in $pngData) { $bw.Write($data) }
    $bw.Flush()
    [System.IO.File]::WriteAllBytes($Path, $ms.ToArray())
    $bw.Dispose()
    $ms.Dispose()
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$pngPath = Join-Path $OutDir "app.png"
$icoPath = Join-Path $OutDir "app.ico"

$bmp256 = Draw-BrandIcon 256
$bmp256.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

$sizes = @(16, 32, 48, 256)
$icons = foreach ($s in $sizes) { Draw-BrandIcon $s }
Save-Ico $icoPath $icons

foreach ($b in $icons) { $b.Dispose() }
$bmp256.Dispose()

Write-Host "Generated: $pngPath"
Write-Host "Generated: $icoPath"
