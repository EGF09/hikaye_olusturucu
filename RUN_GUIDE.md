# Adım Adım Çalıştırma Rehberi (RUN_GUIDE) - Ücretsiz Sürüm

Uygulamanın eksiksiz derlenmesi ve hatasız çalıştırılması için hiçbir API anahtarına ihtiyacınız yoktur. Tüm servisler ücretsiz altyapılar kullanacak şekilde güncellenmiştir.

## 1. Projeyi Hazırlama
Sistemde **.NET 8 SDK** kurulu olduğundan emin olun. Terminal veya Komut Satırından proje dizinine gidin:
```bash
cd C:\Users\LaxeL\OneDrive\Belgeler\GitHub\hikaye_olusturucu\hikaye-olusturucu
```

## 2. FFmpeg Kurulumu
- FFmpeg sürümünü indirin (Essentials veya Full build).
- Zip dosyasını örneğin `C:\ffmpeg` konumuna çıkartın.
- `ffmpeg.exe` dosyasının `C:\ffmpeg\bin\ffmpeg.exe` yolunda olduğundan emin olun.

## 3. Ses Sentezi İçin Türkçe Dil Paketinin (Önerilen) Kurulumu
Bu uygulama metin okuma için Windows'un dahili `System.Speech` özelliğini kullanmaktadır. Hikayelerin Türkçe ve düzgün okunabilmesi için bilgisayarınızda Türkçe ses paketinin yüklü olması gerekir:
- Windows'ta `Ayarlar > Zaman ve Dil > Dil ve Bölge` kısmına gidin.
- Türkçe için "Konuşma" veya "Ses" (Speech) paketinin indirildiğine emin olun. (Tolga vs. gibi Türkçe seslendirmenleri indirir).

## 4. appsettings.json Yapılandırması
`hikaye-olusturucu` klasörü içerisindeki `appsettings.json` dosyasını bir metin editörüyle açın:
- `FFmpeg:ExecutablePath` alanının bilgisayarınızdaki tam `ffmpeg.exe` yolunu gösterdiğinden emin olun. (Örn: `C:\\ffmpeg\\bin\\ffmpeg.exe`)

## 5. Projeyi Build Etme
Terminalden (veya Visual Studio 2022 üzerinden) paketleri yükleyip projeyi derleyin:
```bash
dotnet restore
dotnet build
```

## 6. Uygulamayı Çalıştırma
```bash
dotnet run
```
Açılan Windows Form arayüzünde;
1. "Prompt" kutusuna hikaye fikrinizi yazın.
2. **"Hikaye Oluştur"** butonuna basın. (Pollinations.ai aracılığıyla API keysiz bir şekilde hikaye ve 3 görsel üretilecek, ardından ses dosyası Windows üzerinden oluşturulacaktır.)
3. İşlem tamamlandığında **"Video Oluştur"** butonuna tıklayarak FFmpeg üzerinden geçişli ve altyazılı mp4 videonuzu oluşturun.

## Olası Hatalar ve Çözümleri
- **Hata:** `FFmpeg hatası: The system cannot find the file specified`
  **Çözüm:** `appsettings.json` içerisindeki `ExecutablePath` değeri yanlıştır. ffmpeg.exe'nin konumunu doğrulayın.
- **Hata:** Seslendirme kalitesi çok kötü / İngilizce aksanıyla Türkçe okuyor.
  **Çözüm:** Windows ayarlarında Türkçe konuşma (Speech) paketi yüklü değildir. Ayarlardan kurup uygulamayı yeniden başlatın.
- **Hata:** API İstek Hatası (Timeout)
  **Çözüm:** Pollinations.ai ücretsiz olduğu için yoğun saatlerde yanıt vermesi zaman alabilir. Lütfen bir süre sonra tekrar deneyin veya internet bağlantınızı kontrol edin.