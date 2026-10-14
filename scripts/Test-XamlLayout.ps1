param([string]$AppRoot = (Join-Path $PSScriptRoot '../src/iBackup.App'))
$ErrorActionPreference = 'Stop'
$checked = 0
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $AppRoot 'Views') -Filter '*.xaml') {
    [xml]$document = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($grid in $document.SelectNodes("//*[local-name()='Grid']")) {
        foreach ($axis in @('Row', 'Column')) {
            $definitions = $grid.SelectNodes("*[local-name()='Grid.${axis}Definitions']/*")
            $count = [Math]::Max(1, $definitions.Count)
            foreach ($child in $grid.ChildNodes) {
                if ($child -isnot [System.Xml.XmlElement]) { continue }
                $indexText = $child.GetAttribute("Grid.$axis")
                if ($indexText -match '^\d+$' -and [int]$indexText -ge $count) {
                    throw "$($file.Name): Grid.$axis=$indexText exceeds $count defined tracks."
                }
            }
        }
        $checked++
    }
}
Write-Host "Validated row/column references in $checked XAML grids."
