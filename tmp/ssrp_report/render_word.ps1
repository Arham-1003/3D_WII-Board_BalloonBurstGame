param([string]$DocumentPath,[string]$PdfPath)
$ErrorActionPreference = 'Stop'
$renderWord = $null
$renderDoc = $null
try {
    $renderWord = New-Object -ComObject Word.Application
    $renderWord.Visible = $false
    $renderWord.DisplayAlerts = 0
    $renderDoc = $renderWord.Documents.Open($DocumentPath, $false, $true)
    $renderDoc.Repaginate()
    $renderDoc.ExportAsFixedFormat($PdfPath, 17)
    Write-Output ('Rendered pages: ' + $renderDoc.ComputeStatistics(2))
} finally {
    if ($null -ne $renderDoc) { $renderDoc.Close(0) }
    if ($null -ne $renderWord) { $renderWord.Quit() }
}
