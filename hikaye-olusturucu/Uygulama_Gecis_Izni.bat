@echo off
chcp 65001 > nul
title Hikaye ve Video Oluşturucu - Güvenlik İzni
echo ======================================================================
echo          HİKAYE VE VİDEO OLUŞTURUCU - UYGULAMA GEÇİŞ İZNİ
echo ======================================================================
echo.
echo Windows Smart App Control (Akıllı Uygulama Denetimi) engellerini aşmak
echo amacıyla yerel geliştirici sertifikası oluşturuluyor ve yükleniyor...
echo.
powershell -ExecutionPolicy Bypass -File "%~dp0sign_app.ps1"
echo.
echo ======================================================================
echo İşlem tamamlandı. Artık uygulamayı sorunsuzca başlatabilirsiniz!
echo ======================================================================
pause
