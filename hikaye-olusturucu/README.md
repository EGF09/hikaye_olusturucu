# 🎬 AI Hikaye ve Video Oluşturucu (AI Story & Video Creator)

Bu uygulama, kullanıcı tarafından verilen herhangi bir konuda yapay zeka yardımıyla **3 bölümlük (Giriş, Gelişme, Sonuç) Türkçe hikaye yazan**, hikayeye uygun **3 adet yapay zeka görseli üreten**, hikayeyi **seslendiren** ve ses ile alt yazısı tamamen senkronize, sinematik pürüzsüz hareket efektlerine (zoompan) sahip **1024x1024 boyutlarında MP4 videolar** üreten modern bir masaüstü uygulamasıdır (.NET 8 & Windows Forms).

---

## 🚀 Özellikler

### 📝 1. Gelişmiş Hikaye ve Başlık Üretimi (LLM)
* **Google Gemini API Entegrasyonu:** `gemini-flash-latest` model havuzunu kullanarak kararlı ve hızlı metin üretimi sağlar. Kota aşımı durumlarında (`429` / `RESOURCE_EXHAUSTED`) otomatik olarak yedek modellere geçiş yapar.
* **Gelişmiş Yapay Zeka Şelalesi:** Gemini servisleri tamamen kapalıysa, sırasıyla **Hugging Face (Qwen)** ve **Pollinations AI** metin modellerini kullanarak hikayeyi üretmeye devam eder.
* **Yapısal Düzen:** Hikayeler tam olarak **3 paragraf (Giriş - Gelişme - Sonuç)** halinde ve aralarında boş satırlar olacak şekilde Türkçe olarak yazılır.

### 🖼️ 2. Kesintisiz Görsel Üretimi (Cascade Image Fallback)
API anahtarlarınız olmasa veya limitleriniz dolsa dahi uygulamanın hiçbir zaman görsel yerine "Hata/Boş Kutu" göstermemesi için **6 kademeli şelale sistemi** kurulmuştur:
1. **Pollinations AI (Anahtarlı):** Hızlı ve yüksek çözünürlüklü öncelikli görsel üretimi.
2. **Pollinations AI (Anahtarsız):** Kota aşımında kullanıcının yerel IP'si üzerinden keyless deneme.
3. **Hugging Face API (Flux Schnell):** Yüksek kaliteli sinematik görseller.
4. **LoremFlickr (Etiketli Stock):** AI servisleri çökerse, hikaye promptundan çıkarılan İngilizce kelimelerle konuya uygun gerçek görseller çeker.
5. **Picsum Photos (Rastgele Doğa/Sanat):** Konuya uygun görsel bulunamazsa rastgele yüksek çözünürlüklü görsel indirir.
6. **LoremFlickr (Rastgele):** Son güvence olarak tamamen rastgele bir görsel çeker.

### 🔊 3. Seslendirme ve Altyazı
* Google TTS ve sistem konuşma motorları kullanılarak hikaye otomatik olarak Türkçe seslendirilir.
* Ses dalga dosyaları (`.wav`) üretilirken kelime ve cümle bazlı zamanlama verileri (`.vtt` altyazı) eş zamanlı olarak hazırlanır.

### 📹 4. FFmpeg ile 8K Stabilize Video Üretimi
Uygulama, görselleri birleştirip ses ve altyazı ekleyerek video oluşturur:
* **Titreme ve Sallantı Giderici (Jitter/Flickering Fix):** Standart FFmpeg zoompan efektindeki kayma ve titreme sorunlarını önlemek için görseller işlem öncesinde **8192x8192 (8K)** çözünürlüğe yükseltilir. Doğrusal piksel formülü ile hareket ettirilip çıkışta **1024x1024** çözünürlüğüne düşürülür. Bu sayede hareketler tamamen pürüzsüz ve akıcıdır.
* **WebView2 Video Player:** Oluşturulan videolar uygulama içindeki yerleşik, karanlık temalı HTML5 video oynatıcıda altyazılarıyla birlikte izlenebilir.

### 💾 5. Veritabanı ve Arayüz
* **SQLite Altyapısı:** Üretilen tüm hikayeler, görsel yolları, ses ve video dosyaları `stories.db` veritabanında saklanır.
* **Premium Karanlık Tema:** Arayüzdeki beyaz çizgiler özel `BorderlessTabControl` ile giderilmiş ve tamamen göz yormayan modern karanlık tema uygulanmıştır.
* **Performans İyileştirmeleri:** Görsel yüklemelerinde dosya kilitleme ve bellek sızıntıları (memory leak) tamamen önlenmiştir.

---

## 🛠️ Kurulum ve Yapılandırma

### Gereksinimler
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [FFmpeg](https://ffmpeg.org/download.html) (Sisteminizde kurulu ve `C:\ffmpeg\bin\ffmpeg.exe` yolunda olmalıdır. Farklı bir yoldaysa `appsettings.json` dosyasından düzenleyebilirsiniz.)

### API Anahtarları Ayarı
Uygulamanın ana dizininde bulunan [appsettings.json](file:///c:/hikaye_olusturucu/hikaye-olusturucu/appsettings.json) dosyasını açıp API anahtarlarınızı girin:

```json
{
  "Database": {
    "ConnectionString": "Data Source=stories.db;Version=3;"
  },
  "FFmpeg": {
    "ExecutablePath": "C:\\ffmpeg\\bin\\ffmpeg.exe"
  },
  "ApiKeys": {
    "Gemini": "KENDİ_GEMINI_API_ANAHTARINIZ",
    "HuggingFace": "KENDİ_HF_API_ANAHTARINIZ",
    "Pollinations": "KENDİ_POLLINATIONS_API_ANAHTARINIZ"
  }
}
```

---

## 🛡️ Windows Güvenlik / Uygulama Denetimi Engeli Aşaması

Uygulamayı derleyip ilk kez çalıştırdığınızda veya bir arkadaşınıza gönderdiğinizde, Windows Defender veya **Akıllı Uygulama Denetimi (Smart App Control / AppLocker)** dosyayı imzasız olduğu için engelleyebilir ve başlatılmasına izin vermeyebilir.

Bu engeli aşmak için proje ana dizininde tek tıkla çalışan bir güvenlik izni mekanizması eklenmiştir:

1. Proje klasöründeki [Uygulama_Gecis_Izni.bat](file:///c:/hikaye_olusturucu/hikaye-olusturucu/Uygulama_Gecis_Izni.bat) dosyasına sağ tıklayıp **Yönetici Olarak Çalıştır** seçeneğini seçin (veya çift tıklayın).
2. Açılan terminal ekranı bilgisayarınızda yerel bir geliştirici sertifikası oluşturacak, sisteme güvenli olarak tanıtacak ve derlenmiş `.exe` ile `.dll` dosyalarını imzalayacaktır.
3. Betik kapandıktan sonra uygulamayı çift tıklayarak veya `dotnet` komutuyla doğrudan sorunsuzca başlatabilirsiniz.

---

## 💻 Uygulamayı Çalıştırma

Projeyi derlemek ve çalıştırmak için terminalde şu komutları kullanabilirsiniz:

```bash
# Projeyi Temizle ve Derle
dotnet clean
dotnet build

# Uygulamayı Çalıştır
dotnet run
```

Eğer Uygulama Denetimi koruması altındaysanız, derlemeden sonra `Uygulama_Gecis_Izni.bat` dosyasını çalıştırıp ardından uygulamanızı açabilirsiniz.
