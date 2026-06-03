try {
    $config = Get-Content -Raw -Path "appsettings.json" | ConvertFrom-Json
    $geminiKey = $config.ApiKeys.Gemini
    $pollinationsKey = $config.ApiKeys.Pollinations
    
    if (-not $geminiKey) {
        Write-Host "Gemini API Key not found."
        exit
    }
    
    $trimmedKey = $geminiKey.Trim()
    $trimmedPoll = if ($pollinationsKey) { $pollinationsKey.Trim() } else { "" }
    
    Write-Host "--- 1. Generating Story & Caching ---"
    $prompt = "Ormanda kaybolan sevimli bir yavru kedi"
    
    $systemPrompt = "Sen yaratıcı, sürükleyici ve profesyonel bir Türkçe hikaye yazarısın. " +
                  "Kullanıcının verdiği konuya uygun olarak bir hikaye, bu hikayeye uygun bir başlık ve hikayenin sahnelerini temsil eden 3 adet İngilizce görsel üretme promptu hazırla. " +
                  "Yanıtı mutlaka aşağıdaki JSON formatında döndür, JSON dışında başka hiçbir metin (açıklama, markdown kod bloğu işareti vb.) ekleme:\n" +
                  "{\n" +
                  "  \"title\": \"Maksimum 4 kelimelik başlık\",\n" +
                  "  \"story\": \"En az 4-5 uzun paragraftan oluşan hikaye metni (paragraflar arasında \\n\\n olmalı)\",\n" +
                  "  \"prompts\": [\n" +
                  "    \"descriptive image prompt 1 detailing subjects, style and lighting\",\n" +
                  "    \"descriptive image prompt 2 detailing subjects, style and lighting\",\n" +
                  "    \"descriptive image prompt 3 detailing subjects, style and lighting\"\n" +
                  "  ]\n" +
                  "}"
                  
    $url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=$([Uri]::EscapeDataString($trimmedKey))"
    $body = @{
        contents = @(
            @{
                parts = @(
                    @{ text = "$systemPrompt`n`nKonu: $prompt" }
                )
            }
        )
    } | ConvertTo-Json -Depth 5 -Compress
    
    $response = Invoke-RestMethod -Uri $url -Method Post -Headers @{"Content-Type" = "application/json"} -Body $body
    $jsonResponse = $response.candidates[0].content.parts[0].text
    
    Write-Host "Raw JSON Response:"
    Write-Host $jsonResponse
    
    $cleanJson = $jsonResponse.Trim()
    if ($cleanJson.StartsWith("```json")) { $cleanJson = $cleanJson.Substring(7) }
    if ($cleanJson.StartsWith("```")) { $cleanJson = $cleanJson.Substring(3) }
    if ($cleanJson.EndsWith("```")) { $cleanJson = $cleanJson.Substring(0, $cleanJson.Length - 3) }
    $cleanJson = $cleanJson.Trim()
    
    $parsed = $cleanJson | ConvertFrom-Json
    
    $cachedTitle = $parsed.title
    $cachedPrompts = $parsed.prompts
    $storyContent = $parsed.story
    
    Write-Host "`n--- Parsed Results ---"
    Write-Host "Title: $cachedTitle"
    Write-Host "Story Paragraphs count: $($storyContent.Split("`n").Length)"
    Write-Host "Prompts count: $($cachedPrompts.Count)"
    foreach ($p in $cachedPrompts) {
        Write-Host " - Prompt: $p"
    }
    
    Write-Host "`n--- 2. Requesting Image from Pollinations with Key ---"
    $firstPrompt = $cachedPrompts[0]
    $imgUrl = "https://gen.pollinations.ai/image/$([Uri]::EscapeDataString($firstPrompt))?width=256&height=256&nologo=true&seed=123&key=$trimmedPoll"
    
    Write-Host "Requesting from: $imgUrl"
    $imgResponse = Invoke-WebRequest -Uri $imgUrl -Method Get -TimeoutSec 15
    Write-Host "Image Success! Length: $($imgResponse.Content.Length) bytes"
    
} catch {
    Write-Host "Flow Failed!"
    Write-Host $_.Exception.Message
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        Write-Host "Response Body: $($reader.ReadToEnd())"
    }
}
