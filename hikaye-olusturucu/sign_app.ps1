# 1. Kod imzalama sertifikası oluştur (Eğer daha önce oluşturulmadıysa)
$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -like "*CN=HikayeOlusturucuDev*" } | Select-Object -First 1
if (-not $cert) {
    Write-Host "Yeni kod imzalama sertifikası oluşturuluyor..." -ForegroundColor Cyan
    $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=HikayeOlusturucuDev" -KeyUsage DigitalSignature -FriendlyName "Hikaye Olusturucu Geliştirici Sertifikası" -CertStoreLocation "Cert:\CurrentUser\My"
}

# 2. Sertifikayı geçici olarak dışa aktar
$certPath = Join-Path $env:TEMP "HikayeOlusturucuDev.cer"
Export-Certificate -Cert $cert -FilePath $certPath | Out-Null

# 3. Sertifikayı Güvenilen Kök Yetkilileri ve Güvenilen Yayımcılar deposuna ekle (Kullanıcı Düzeyinde)
Write-Host "Sertifika yerel olarak güvenilenlere ekleniyor..." -ForegroundColor Cyan
Import-Certificate -FilePath $certPath -CertStoreLocation "Cert:\CurrentUser\Root" | Out-Null
Import-Certificate -FilePath $certPath -CertStoreLocation "Cert:\CurrentUser\TrustedPublisher" | Out-Null

# 4. Derlenen EXE ve DLL dosyalarını imzala
$exePath = "bin\Debug\net8.0-windows10.0.19041.0\hikaye-olusturucu.exe"
$dllPath = "bin\Debug\net8.0-windows10.0.19041.0\hikaye-olusturucu.dll"

$signedAny = $false

if (Test-Path $exePath) {
    Write-Host "EXE imzalanıyor..." -ForegroundColor Green
    Set-AuthenticodeSignature -FilePath $exePath -Certificate $cert | Out-Null
    $signedAny = $true
}
if (Test-Path $dllPath) {
    Write-Host "DLL imzalanıyor..." -ForegroundColor Green
    Set-AuthenticodeSignature -FilePath $dllPath -Certificate $cert | Out-Null
    $signedAny = $true
}

if ($signedAny) {
    Write-Host "İmzalama tamamlandı! Lütfen uygulamayı veya dotnet komutunu tekrar çalıştırmayı deneyin." -ForegroundColor Green
} else {
    Write-Warning "İmzalanacak derlenmiş dosya bulunamadı. Lütfen önce projeyi derleyin."
}
