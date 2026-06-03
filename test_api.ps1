Write-Host "--- Hugging Face Router Test ---"
$headers = @{
    "Authorization" = "Bearer YOUR_HF_API_KEY"
    "Content-Type"  = "application/json"
}
$body = @{
    model    = "Qwen/Qwen2.5-7B-Instruct"
    messages = @(
        @{ role = "user"; content = "merhaba" }
    )
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest -Uri "https://router.huggingface.co/v1/chat/completions" -Headers $headers -Method Post -Body $body -UseBasicParsing
    Write-Host "Status: $($response.StatusCode)"
    Write-Host "Content: $($response.Content)"
} catch {
    Write-Host "Hata: $_"
}

Write-Host "`n--- Pollinations AI Test ---"
for ($i = 0; $i -lt 2; $i++) {
    Write-Host "Attempt $i"
    try {
        $response = Invoke-WebRequest -Uri "https://text.pollinations.ai/" -Method Post -ContentType "application/json" -Body '{"messages":[{"role":"user","content":"merhaba"}],"model":"openai"}' -UseBasicParsing
        Write-Host "Status: $($response.StatusCode)"
        Write-Host "Content: $($response.Content)"
    } catch {
        Write-Host "Hata: $_"
    }
    Start-Sleep -Seconds 2
}
