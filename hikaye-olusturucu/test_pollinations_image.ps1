try {
    $prompt = "A beautiful magical forest, digital art"
    $seed = [System.Guid]::NewGuid().GetHashCode()
    $url = "https://image.pollinations.ai/prompt/$([Uri]::EscapeDataString($prompt))?width=1024&height=1024&nologo=true&seed=$seed"
    
    Write-Host "Requesting image from: $url"
    
    $startTime = Get-Date
    $response = Invoke-WebRequest -Uri $url -Method Get -TimeoutSec 30
    $endTime = Get-Date
    $duration = ($endTime - $startTime).TotalSeconds
    
    Write-Host "Status Code: $($response.StatusCode)"
    Write-Host "Content Length: $($response.Content.Length) bytes"
    Write-Host "Response Headers:"
    $response.Headers | Out-String | Write-Host
    Write-Host "Duration: $duration seconds"
    
    if ($response.Content.Length -le 1000) {
        $raw = [System.Text.Encoding]::UTF8.GetString($response.Content)
        Write-Host "Response content (short): $raw"
    }
} catch {
    Write-Host "Image request failed!"
    Write-Host $_.Exception.Message
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        Write-Host "Error Body: $($reader.ReadToEnd())"
    }
}
