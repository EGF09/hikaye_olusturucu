# Hikaye Oluşturucu AI (WinForms .NET 8) - Ücretsiz Sürüm

Bu proje, kullanıcının girdiği kısa bir metinden (prompt) yola çıkarak AI destekli uçtan uca bir hikaye, görsel, ses ve altyazılı video oluşturan bir otomasyon sistemidir. Katmanlı mimari ve SOLID prensipleri gözetilerek geliştirilmiştir.

## Özellikler
- **LLM Entegrasyonu:** Kullanıcı prompt'una özel hikaye üretimi. (Pollinations AI - Ücretsiz / API Key Gerektirmez)
- **AI Görsel Üretimi:** Hikaye bağlamına uygun minimum 3 benzersiz sahne görseli. (Pollinations AI)
- **Text-to-Speech:** Hikayenin gerçekçi bir insan sesiyle seslendirilmesi. (Windows System.Speech.Synthesis - Çevrimdışı, Tamamen Ücretsiz)
- **Video Oluşturma (FFmpeg):** Görselleri fade geçiş efektleriyle (xfade) birleştirir, arka plana sesi koyar ve hikaye metnini altyazı (.srt) olarak videoya gömer.
- **Veritabanı Kaydı (SQLite):** Oluşturulan içeriklerin meta verileri veritabanına otomatik kaydedilir.
- **Modern UI:** Kullanıcı dostu, log takibi yapılabilen WinForms arayüzü.

## Kullanılan Teknolojiler
- C# .NET 8 (Windows Forms)
- Dependency Injection (`Microsoft.Extensions.DependencyInjection`)
- SQLite (`System.Data.SQLite`)
- Windows Native Ses Sentezi (`System.Speech`)
- FFmpeg (Komut Satırı / Filter Complex API)

## Kurulum ve Gereksinimler
1. Projenin çalışabilmesi için sisteminizde **FFmpeg** yüklü olmalıdır.
2. FFmpeg indirmek için [ffmpeg.org](https://ffmpeg.org/download.html) adresini ziyaret edin.
3. Uygulamanın Türkçe hikayeleri doğru okuyabilmesi için Windows Ayarlarından **"Türkçe Ses Paketi"**nin yüklü olduğundan emin olun.

## Konfigürasyon
Proje klasöründeki `appsettings.json` dosyasını açıp FFmpeg yolunu sisteminizdeki yola göre güncelleyin. Başka hiçbir API anahtarına gerek yoktur:
```json
{
  "Database": {
    "ConnectionString": "Data Source=stories.db;Version=3;"
  },
  "FFmpeg": {
    "ExecutablePath": "C:\\ffmpeg\\bin\\ffmpeg.exe"
  }
}
```

## Proje Mimarisi Açıklaması
- **Core/Models:** Veritabanı ve taşıma işlemleri için varlık modelleri.
- **Core/Interfaces:** Bağımlılıkları soyutlamak için kullanılan arayüzler.
- **Services:** İş mantığı, Pollinations AI HTTP bağlantıları, System.Speech motoru ve FFmpeg logic'leri.
- **DataAccess:** SQLite veritabanı CRUD işlemleri.
- **UI:** Form1 üzerinden asenkron event tabanlı ekran akışı yönetimi.