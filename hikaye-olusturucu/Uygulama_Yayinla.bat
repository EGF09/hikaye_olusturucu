@echo off
chcp 65001 > nul
title Hikaye ve Video Oluşturucu - Yayınlama Sihirbazı
echo ======================================================================
echo          HİKAYE VE VİDEO OLUŞTURUCU - YAYINLAMA VE DERLEME
echo ======================================================================
echo.
echo Uygulama tek bir .EXE dosyası olarak derleniyor (Release)...
echo (Bu işlem ilk kez yapıldığında birkaç dakika sürebilir)
echo.
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true
if %errorlevel% neq 0 (
    echo.
    echo [HATA] Derleme işlemi başarısız oldu!
    pause
    exit /b
)
echo.
echo Derleme başarılı!
echo.
echo ======================================================================
echo          GÜVENLİK İMZASI EKLEME (Smart App Control Geçişi)
echo ======================================================================
echo.
echo Derlenen yayın klasöründeki dosyalar imzalanıyor...
powershell -Command "$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -like '*CN=HikayeOlusturucuDev*' } | Select-Object -First 1; if ($cert) { Set-AuthenticodeSignature -FilePath 'bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\hikaye-olusturucu.exe' -Certificate $cert | Out-Null; Write-Host 'EXE başarıyla imzalandı!' -ForegroundColor Green } else { Write-Warning 'İmzalama sertifikası bulunamadı, lütfen önce Uygulama_Gecis_Izni.bat çalıştırın!' }"
echo.
echo ======================================================================
echo          YAYIN DOSYALARI HAZIR!
echo ======================================================================
echo.
echo Çıktı klasörü açılıyor...
explorer "%~dp0bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\"
echo.
echo ÖNEMLİ NOTLAR:
echo 1. Arkadaşınızın bilgisayarında çalışması için bu klasördeki 'hikaye-olusturucu.exe'
echo    ve 'appsettings.json' dosyalarını bir arada göndermelisiniz.
echo 2. Arkadaşınızın bilgisayarında Windows Smart App Control engeli varsa, ona da
echo    'Uygulama_Gecis_Izni.bat' dosyasını bir kez yönetici olarak çalıştırtmalısınız.
echo.
pause
