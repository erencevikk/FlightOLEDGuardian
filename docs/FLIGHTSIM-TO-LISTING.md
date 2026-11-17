# FlightOLED Guardian — Flightsim.to submission copy

Status: prepared for submission; not published. Waiting for the user's Flightsim.to sign-in. English first, Turkish second.

Suggested title: **FlightOLED Guardian — OLED Display Protection & SimBrief TOD (Public Preview)**

Version: **0.3.0**

Suggested category: standalone utilities / display or hardware utilities, depending on the categories offered by the upload form.

Package: `FlightOLEDGuardian-v0.3-FlightsimTo.zip`

## English

**Give your OLED a break during long simulator sessions.**

FlightOLED Guardian is a free, open-source Windows companion designed to schedule black-screen intervals on your selected display using flight altitude and user inactivity. It is a separate desktop application, with a tray icon and no permanent overlay during normal flight.

### What it does

- Default activation above **7,000 FT AGL**, after **60 seconds without user input**.
- Default cycle: **9 minutes normal display**, a **10-second countdown included in that period**, then **1 minute black**.
- Skip once, Snooze, manual protection and immediate wake controls.
- Keyboard, mouse and significant supported joystick input wake the screen.
- IFR, VFR and custom profiles; English and Turkish interface with a first-run language choice.
- SimBrief Alias or Pilot ID import for **planned TOD** route distance. A SimBrief password is not required. Aircraft FMS/VNAV TOD can differ.
- SimBrief identity hidden by default and always masked in streaming mode.
- Optional moving dim telemetry, a 50 NM TOD alert and flight reports.
- Startup/manual GitHub update checks and optional MSFS EXE.xml auto-launch.
- Simulator-free mock lab with editable altitude and TOD values.

### Installation

1. Extract the ZIP.
2. Close any running Guardian instance using **Exit** in its tray menu.
3. Run **FlightOLEDGuardian-v0.3-Setup.exe**, or use **Portable/FlightOLEDGuardian.App.exe** and keep its files together.
4. Select your language, protected monitor and preferred profile. Check the streaming/capture settings before enabling protection during a recording or stream.

Requirements: **Windows 10/11 x64**, **.NET 8 Desktop Runtime x64**, and an official installed **x64 native SimConnect client** for live MSFS telemetry. The Microsoft SDK DLL is not redistributed. Select the official client in Settings if automatic detection does not find it. This is a desktop utility, **not a Community-folder add-on**. It does not write aircraft controls.

Before uninstalling, disable **Start with MSFS** in Guardian. When moving from portable to Setup, save the enabled auto-launch setting to refresh the EXE.xml executable path. Settings and flight reports are preserved separately.

### Public preview — please read

54 automated tests, 8 full-app UI checks and 4 installer extraction/render checks passed. **Actual MSFS flight cycling, real-account installation/upgrade/uninstallation and OBS/physical HDMI capture still require real-world validation.** The native SimConnect DLL loaded in the last desktop check, but no live flight telemetry was received in that test window. MSFS 2020/2024 support is intended; this is not a claim that both have completed end-to-end acceptance testing.

**Capture cards and cloned HDMI outputs can include the black overlay.** A Windows capture-exclusion success does not prove physical capture compatibility. Record a test with your exact setup. This tool reduces static-image display time; it does not guarantee prevention of OLED burn-in. SimBrief identity masking is display privacy, not encryption of the local settings file.

### Feedback

Please share your Guardian/MSFS versions, whether you used mock or live telemetry, your AGL/MSL setting, expected versus actual behavior and your display/capture arrangement. Remove SimBrief identities and personal data from screenshots or logs. Comments and reproducible bug reports are welcome.

Source, downloads and issues: https://github.com/erencevikk/FlightOLEDGuardian

License: MIT. No paid service or donation is required.

---

## Türkçe

**Uzun simülatör oturumlarında OLED ekranınıza mola verin.**

FlightOLED Guardian, uçuş irtifası ve kullanıcı hareketsizliğine göre seçtiğiniz ekranda siyah ekran aralıkları planlamak için geliştirilen ücretsiz, açık kaynaklı bir Windows yardımcı uygulamasıdır. Ayrı bir masaüstü programıdır; tepsi simgesi bulunur ve normal uçuşta sürekli görünen bir katman oluşturmaz.

### Özellikler

- Varsayılan olarak **7.000 FT AGL** üzerinde, **60 saniye kullanıcı girişi olmadığında** etkinleşme.
- Varsayılan döngü: **9 dakika normal görüntü**, bu süreye dahil **10 saniyelik geri sayım**, ardından **1 dakika siyah ekran**.
- Bir kez atla, ertele, manuel koruma ve ekranı hemen açma kontrolleri.
- Klavye, fare veya desteklenen joystick'teki belirgin hareketle ekranı açma.
- IFR, VFR ve özel profiller; ilk açılışta dil seçimiyle İngilizce/Türkçe arayüz.
- SimBrief Alias veya Pilot ID ile **planlanan TOD** rota mesafesi. SimBrief şifresi istenmez. Uçağın FMS/VNAV TOD değeri farklı olabilir.
- Varsayılan gizli, yayın modunda daima maskelenen SimBrief kimliği.
- İsteğe bağlı hareketli düşük parlaklıklı telemetri, 50 NM TOD uyarısı ve uçuş raporları.
- Açılışta/elle GitHub güncelleme kontrolü ve isteğe bağlı MSFS EXE.xml otomatik başlatma.
- İrtifa/TOD değerleri düzenlenebilen, simülatör gerektirmeyen test laboratuvarı.

### Kurulum

1. ZIP dosyasını çıkartın.
2. Çalışan Guardian örneğini tepsi menüsündeki **Çıkış** ile kapatın.
3. **FlightOLEDGuardian-v0.3-Setup.exe** dosyasını çalıştırın veya **Portable/FlightOLEDGuardian.App.exe** dosyasını yanındaki dosyalarla birlikte kullanın.
4. Dil, korunacak monitör ve profil seçimini yapın. Yayın/kayıt sırasında korumayı açmadan önce yakalama ayarlarınızı kontrol edin.

Gereksinimler: **Windows 10/11 x64**, **.NET 8 Desktop Runtime x64** ve canlı MSFS telemetrisi için kurulu resmî **x64 native SimConnect istemcisi**. Microsoft SDK DLL'si pakette yeniden dağıtılmaz. Otomatik algılama bulamazsa resmî istemciyi Ayarlar'dan seçin. Bu bir masaüstü programıdır; **Community klasörüne atılmaz**. Uçuş kumandalarına yazmaz.

Kaldırmadan önce Guardian'daki **MSFS ile başlat** tercihini kapatın. Taşınabilir sürümden kurulum sürümüne geçerken etkin otomatik başlatma ayarını kaydedip EXE.xml yolunu yenileyin. Ayarlar ve uçuş raporları ayrı tutulur ve korunur.

### Genel önizleme — lütfen okuyun

54 otomatik test, 8 tam uygulama arayüz kontrolü ve 4 kurulum açma/render kontrolü geçti. **Gerçek MSFS uçuşunda koruma döngüsü, gerçek kullanıcı hesabında kurulum/güncelleme/kaldırma ve OBS/fiziksel HDMI yakalama hâlâ gerçek ortamda doğrulanmalıdır.** Son masaüstü denemesinde native SimConnect DLL'si yüklendi ancak canlı uçuş telemetrisi alınmadı. MSFS 2020/2024 desteği hedeflenir; her ikisinin de uçtan uca kabul testinden geçtiği iddia edilmez.

**Capture kartı ve klonlanmış HDMI çıkışı siyah katmanı da taşıyabilir.** Windows'un yakalamadan hariç tutma isteğini kabul etmesi fiziksel yakalama uyumluluğunu kanıtlamaz. Kendi düzeninizde test kaydı alın. Program sabit görüntü süresini azaltır; OLED yanığını önlemeyi garanti etmez. SimBrief maskeleme, ekran gizliliğidir; yerel ayar dosyasını şifrelemez.

### Geri bildirim

Guardian/MSFS sürümlerini, mock/canlı telemetri tercihini, AGL/MSL ayarını, beklenen/gerçekleşen davranışı ve ekran/capture düzeninizi paylaşın. Ekran görüntülerinden ve günlüklerden SimBrief kimliğini ve kişisel verileri kaldırın. Yorumlarınızı ve tekrar üretilebilir hata bildirimlerinizi bekliyoruz.

Kaynak kodu, indirmeler ve hata bildirimi: https://github.com/erencevikk/FlightOLEDGuardian

Lisans: MIT. Ücretli servis veya bağış gerekmez.
