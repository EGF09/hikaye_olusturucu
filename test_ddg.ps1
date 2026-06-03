$response = Invoke-WebRequest -Uri "https://duckduckgo.com/duckchat/v1/status" -Headers @{"x-vqd-accept"="1"} -Method Get
$vqd = $response.Headers["x-vqd-4"]
Write-Host "VQD: $vqd"
$body = '{"model":"gpt-4o-mini","messages":[{"role":"user","content":"merhaba"}]}'
$chatResponse = Invoke-WebRequest -Uri "https://duckduckgo.com/duckchat/v1/chat" -Headers @{"x-vqd-4"=$vqd; "Content-Type"="application/json"} -Method Post -Body $body
Write-Host "Chat Response: $($chatResponse.Content)"