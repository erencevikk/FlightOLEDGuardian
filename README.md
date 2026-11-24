# FlightOLED Guardian v0.5.5

![FlightOLED Guardian](src/FlightOLEDGuardian.App/Assets/Guardian.png)

[English](#english) · [Türkçe](#türkçe) · [Download / İndir](https://github.com/erencevikk/FlightOLEDGuardian/releases/tag/v0.5.5) · [Flightsim.to](https://flightsim.to/addon/119434/flightoled-guardian-oled-display-protection-simbrief-tod-preview)

## English

FlightOLED Guardian is a free, open-source Windows utility for managing how long static cockpit imagery stays on an OLED display during Microsoft Flight Simulator 2020/2024 or X-Plane 12 flights.

### Download and install v0.5.5

**What's new in v0.5.5.** A first-run setup screen (language, protected display, protection mode, MSFS start, info card), a redesigned OLED-dim black-screen status card, and fixes: v0.5.4 no longer reports its own release as an update, synthetic input (remote desktop, keep-awake tools, remappers) no longer wakes the screen, DDC/CI dimming no longer stalls the dashboard, and low fuel keeps the screen awake. Keyboard and mouse monitoring still pauses while the simulator is closed.

- **Standalone Windows x64.** Extract [FlightOLEDGuardian-v0.5.5-Standalone-win-x64.zip](https://github.com/erencevikk/FlightOLEDGuardian/releases/download/v0.5.5/FlightOLEDGuardian-v0.5.5-Standalone-win-x64.zip) and run `FlightOLEDGuardian.App.exe`. Keep the extracted files together. .NET and x64 SimConnect are bundled.
- **Windows setup.** Run [FlightOLEDGuardian-v0.5.5-Setup.exe](https://github.com/erencevikk/FlightOLEDGuardian/releases/download/v0.5.5/FlightOLEDGuardian-v0.5.5-Setup.exe) for a per-user installation. This option requires .NET 8 Desktop Runtime x64. Close an existing Guardian instance from the tray before updating.
- **Source and checksums.** The exact source for the packaged build is in [FlightOLEDGuardian-v0.5.5-Source.zip](https://github.com/erencevikk/FlightOLEDGuardian/releases/download/v0.5.5/FlightOLEDGuardian-v0.5.5-Source.zip). Verify downloads with [FlightOLEDGuardian-v0.5.5-SHA256SUMS.txt](https://github.com/erencevikk/FlightOLEDGuardian/releases/download/v0.5.5/FlightOLEDGuardian-v0.5.5-SHA256SUMS.txt). [Release assets](https://github.com/erencevikk/FlightOLEDGuardian/releases/tag/v0.5.5#assets)

### Features

- Smooth transitions, a timed dim stage, top and bottom masks, multiple protected displays, and joystick bindings for **Protect Now** and **Snooze**.
- IFR and VFR profiles, optional MSFS startup and a moving protection indicator. The default IFR profile requires confirmed autopilot engagement for automatic darkening.
- SimBrief planned Top of Descent (TOD), an optional 50 NM alert, and protection hold near planned TOD or during sustained descent. Automatic simulator pause is optional and intended for offline use. X-Plane pause requires Web API v2 or later.
- Screen wake on supported flight warnings, optional simulator volume reduction, physical monitor dimming via DDC/CI and a Windows EcoQoS hint. DDC/CI is off by default and support depends on the display and connection.
- Monthly full-black duration in flight reports, a simulator-free mock mode, and an English or Turkish interface.

### Updating and feedback

Guardian checks GitHub at startup. You can also use **Check for updates** in Settings. Download and install updates from the release page.

Automated UI and installer checks passed for v0.5.5. Live simulator and physical OBS/HDMI capture still require validation on your setup. Check your own capture arrangement before streaming. Share your Guardian and simulator versions, display setup, and reproduction steps via [GitHub Issues](https://github.com/erencevikk/FlightOLEDGuardian/issues).

Optional support is available through my [YouTube channel — Eren Çevik](https://www.youtube.com/@eren_cevik). The utility is free.

## Türkçe

FlightOLED Guardian, Microsoft Flight Simulator 2020/2024 veya X-Plane 12 uçuşlarında sabit kokpit görüntüsünün OLED ekranda kalma süresini yönetmeye yardımcı olan ücretsiz, açık kaynaklı bir Windows uygulamasıdır.

### İndirme ve kurulum v0.5.5

**v0.5.5 yenilikleri.** İlk kurulum ekranı (dil, korunacak ekran, koruma modu, MSFS ile başlatma, bilgi kartı), OLED için düşük parlaklıkta yeniden tasarlanan siyah ekran kartı ve düzeltmeler: v0.5.4'ün kendi sürümünü güncelleme olarak göstermesi giderildi; uzak masaüstü, uyanık tutma ve tuş eşleme programlarının gönderdiği sahte girdi artık ekranı uyandırmıyor; DDC/CI karartma arayüzü dondurmuyor; düşük yakıtta ekran açık kalıyor. Simülatör kapalıyken klavye ve fare izleme yine durur.

- **Bağımsız Windows x64.** [FlightOLEDGuardian-v0.5.5-Standalone-win-x64.zip](https://github.com/erencevikk/FlightOLEDGuardian/releases/download/v0.5.5/FlightOLEDGuardian-v0.5.5-Standalone-win-x64.zip) dosyasını çıkartın ve `FlightOLEDGuardian.App.exe` dosyasını çalıştırın. Çıkartılan dosyaları birlikte tutun. .NET ve x64 SimConnect pakete dahildir.
- **Windows kurulumu.** Kullanıcı hesabınıza kurmak için [FlightOLEDGuardian-v0.5.5-Setup.exe](https://github.com/erencevikk/FlightOLEDGuardian/releases/download/v0.5.5/FlightOLEDGuardian-v0.5.5-Setup.exe) dosyasını çalıştırın. Bu seçenek .NET 8 Desktop Runtime x64 gerektirir. Güncellemeden önce çalışan Guardian örneğini tepsi menüsünden kapatın.
- **Kaynak kod ve sağlama değerleri.** Yayımlanan derlemenin kaynakları [FlightOLEDGuardian-v0.5.5-Source.zip](https://github.com/erencevikk/FlightOLEDGuardian/releases/download/v0.5.5/FlightOLEDGuardian-v0.5.5-Source.zip) dosyasındadır. İndirmeleri [FlightOLEDGuardian-v0.5.5-SHA256SUMS.txt](https://github.com/erencevikk/FlightOLEDGuardian/releases/download/v0.5.5/FlightOLEDGuardian-v0.5.5-SHA256SUMS.txt) ile doğrulayabilirsiniz. [Sürüm dosyaları](https://github.com/erencevikk/FlightOLEDGuardian/releases/tag/v0.5.5#assets)

### Özellikler

- Yumuşak geçiş, süreli loş ekran aşaması, üst ve alt kenar maskeleri, çoklu ekran seçimi ve **Şimdi Koru** ile **Ertele** için joystick tuş ataması.
- IFR ve VFR profilleri, MSFS ile isteğe bağlı açılış ve hareketli koruma göstergesi. Varsayılan IFR profilinde otomatik karartma doğrulanmış otopilot durumu gerektirir.
- SimBrief planındaki alçalış başlangıcı (TOD), isteğe bağlı 50 NM uyarısı ve planlanan TOD yakınında veya sürekli alçalışta korumayı bekletme. İsteğe bağlı otomatik duraklatma çevrimdışı kullanım içindir. X-Plane duraklatması Web API v2 veya üstünü gerektirir.
- Desteklenen uçuş uyarılarında ekranı açma, isteğe bağlı simülatör sesi kısma, DDC/CI ile fiziksel parlaklık kontrolü ve Windows EcoQoS seçeneği. DDC/CI varsayılan olarak kapalıdır. Destek ekrana ve bağlantıya bağlıdır.
- Raporlarda aylık tam siyah ekran süresi, simülatörsüz test modu ve Türkçe veya İngilizce arayüz.

### Güncelleme ve geri bildirim

Guardian açılışta GitHub üzerinden güncellemeleri kontrol eder. Ayarlar bölümündeki **Güncellemeleri kontrol et** düğmesini de kullanabilirsiniz. Yeni sürümü yayın sayfasından indirip kurabilirsiniz.

v0.5.5, otomatik arayüz ve kurulum kontrollerinden geçti. Canlı simülatör ve fiziksel OBS/HDMI yakalama kendi kurulumunuzda doğrulanmalıdır. Yayında kullanmadan önce kısa bir deneme kaydı alın. Bir sorun yaşarsanız Guardian ve simülatör sürümünü, ekran düzenini ve sorunu yeniden oluşturma adımlarını [GitHub Issues](https://github.com/erencevikk/FlightOLEDGuardian/issues) üzerinden paylaşabilirsiniz.

Uygulama ücretsizdir. Dilerseniz [YouTube kanalım — Eren Çevik](https://www.youtube.com/@eren_cevik) üzerinden gönüllü destek verebilirsiniz.

[MIT License / MIT Lisansı](LICENSE)
