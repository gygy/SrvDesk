Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition(
  [System.Windows.Automation.AutomationElement]::NameProperty, "Shutdown Agent")
$win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
if (-not $win) { Write-Host "NOT FOUND"; exit 1 }

$r = $win.Current.BoundingRectangle
Write-Host ("Window: {0}x{1} at {2},{3}" -f [int]$r.Width, [int]$r.Height, [int]$r.X, [int]$r.Y)

$walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
function Dump([System.Windows.Automation.AutomationElement]$el, [int]$depth) {
  if ($depth -gt 5) { return }
  $c = $el.Current
  $pad = "  " * $depth
  $line = "{0}{1} name='{2}' class='{3}' y={4} x={5} w={6} h={7}" -f `
    $pad, $c.ControlType.ProgrammaticName, $c.Name, $c.ClassName, `
    [int]$c.BoundingRectangle.Y, [int]$c.BoundingRectangle.X, `
    [int]$c.BoundingRectangle.Width, [int]$c.BoundingRectangle.Height
  Write-Host $line
  $child = $walker.GetFirstChild($el)
  while ($null -ne $child) {
    Dump $child ($depth + 1)
    $child = $walker.GetNextSibling($child)
  }
}
Dump $win 0

# screenshot
$bmp = New-Object System.Drawing.Bitmap ([int]$r.Width), ([int]$r.Height)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen([int]$r.X, [int]$r.Y, 0, 0, $bmp.Size)
$out = "G:\gitea\SrvDesk\docs\shutdown-agent-ref.png"
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Host "shot=$out"
