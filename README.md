# FlightOLED Guardian v0.3.0

![FlightOLED Guardian](src/FlightOLEDGuardian.App/Assets/Guardian.png)

[English](#english) · [Türkçe](#türkçe) · [Download / İndir](https://github.com/erencevikk/FlightOLEDGuardian/releases/latest)

## English

**Fly longer. Protect your OLED.**

FlightOLED Guardian is a free, open-source Windows companion for Microsoft Flight Simulator. It uses flight altitude and user inactivity to schedule black-screen intervals on your selected OLED display. The application stays in the tray when you choose, with no permanent overlay during normal flight.

**Public preview:** automatic and mock checks have passed; live MSFS flight cycling, actual Windows-account installation/upgrade/uninstallation, and the exact OBS/HDMI capture arrangement still require real-world validation. OLED burn-in prevention is not guaranteed.

### Download and install

Get **FlightOLEDGuardian-v0.3-Setup.exe** from [Releases](https://github.com/erencevikk/FlightOLEDGuardian/releases/latest). Setup installs for the current Windows user, adds a Start menu shortcut and an uninstall entry, and optionally creates a desktop shortcut. Administrator elevation is not required. Close any running Guardian instance using **Exit** in its tray menu before installing or updating.

Requirements: **Windows 10/11 x64** and **.NET 8 Desktop Runtime x64**. If the runtime is missing, Setup opens Microsoft's download page after you click Install. Install the Desktop Runtime and run Setup again.

The **Standalone Windows x64** variant bundles .NET **8.0.34**. Extract its entire ZIP and run `FlightOLEDGuardian.App.exe`; no separate .NET download is needed. Keep all files and subfolders together. The bundled CLR path and all 8 UI smoke checks were verified. See [standalone instructions](docs/STANDALONE-README.txt).

For a portable installation, download the Complete ZIP and use its `App` folder. Keep the EXE, DLL and JSON files together. `Start-Guardian.cmd` starts the normal app; `Start-Mock.cmd` starts the simulator-free test mode. Source code and the installer are included in the ZIP.

### Protection behavior

- Default activation: **7,000 FT AGL** (height above ground), after 60 seconds of user inactivity.
- Cycle: **9 minutes normal display**, including a final **10-second countdown**, then **1 minute black**.
- Altitude confirmation: 3 seconds; hysteresis: 200 FT.
- Skip once, Snooze, manual protection, suspend/resume and immediate wake controls.
- Keyboard, mouse or significant supported joystick input wakes the display.
- Emergency shortcut: **Ctrl+Alt+F12**; fallback: **Ctrl+Shift+F12**. If neither can be registered, protection stays suspended.
- Pause, disconnect, stale/invalid telemetry and monitor/power changes keep or restore the visible screen.
- IFR, VFR and custom profiles; periodic, full and adaptive protection modes.

English is the factory default. The first launch asks you to choose **English or Türkçe** independently of the auto-launch question. Change the language later from the header or Settings. AGL is the default altitude reference; MSL remains available as an explicit choice.

### MSFS startup and SimConnect

The first launch asks whether Guardian should start with MSFS. This can also be configured in Settings using the simulator's **EXE.xml / Launch.Addon** mechanism. Other add-ons are preserved and a backup is made before changes. No new Windows startup watcher is installed.

Install Guardian in a stable location before enabling auto-launch. After moving from portable to Setup, save the enabled auto-launch setting to refresh the executable path. **Disable Start with MSFS inside Guardian before uninstalling.** Settings and reports are preserved separately.

The app looks for an installed, official **x64 native SimConnect.dll**, including known SDK and installed-client locations. You can select the official DLL manually in Settings. Wrong-architecture files are rejected. Restart Guardian after selecting a different DLL if a client is already loaded. Microsoft's SDK client binary is not redistributed here. Guardian reads telemetry and system events; it does not write flight controls.

The last native desktop check confirmed DLL loading and the fallback emergency shortcut. No valid live MSFS samples arrived in that test window. DLL loading alone is not a verified live flight connection.

### SimBrief and TOD

Enter a **Navigraph Alias or 1–7 digit SimBrief Pilot ID**, choose the numeric-ID option when appropriate, and click **Fetch SimBrief flight plan**. No SimBrief password is requested. Generate an OFP in SimBrief first. XML import is also available.

The identity is hidden by default, and streaming mode always masks it. This is display privacy: the identity is stored in the local settings file, not encrypted. Plain and URL-encoded identity are redacted from import errors.

SimBrief is queried only when you click Fetch, with a 20-second timeout and a 2 MB response limit. When the OFP contains a TOD fix, Guardian calculates remaining route distance to the **planned TOD**. This is not the aircraft's live FMS/VNAV TOD. Missing TOD, off-route positions or ambiguous legs are not replaced with fabricated distances. Re-fetch after changing your flight plan. A 50 NM visual/spoken alert and optional dim moving telemetry are available.

### Streaming and reports

Unverified capture configurations suspend protection by default in streaming mode. Windows capture-exclusion acceptance does not establish OBS or physical HDMI compatibility. **A cloned HDMI/capture-card signal can include the black overlay.** Record a test in your exact arrangement; use an independent unprotected capture output where necessary.

Reports require an observed takeoff and a confirmed landing: 15 seconds on the ground below 80 KT. Connecting during flight does not fabricate a takeoff. Mock reports are excluded from real totals. PNG export supports 1920×1080, 1080×1920 and 1080×1080. Landing rate is based on sampled telemetry, not an exact contact measurement.

Settings and JSON reports are stored under `%LOCALAPPDATA%/FlightOLEDGuardian`. Use `--data-dir "C:\path"` to isolate test data.

### Updates, testing and feedback

Guardian checks the repository's latest published non-preview-channel GitHub release on each process start, including tray starts. Settings also provides **Check for updates**. Checks have a timeout/size limit; missing releases or network failures are not presented as “up to date.” Updates are not downloaded or installed automatically. The production endpoint was verified to return `v0.3.0`.

Build using the .NET 8 SDK; no third-party NuGet packages are required:

```powershell
.\build.ps1 -UiTest -Smoke -Publish
.\package.ps1
```

Validation: **21 core + 33 integration/WPF tests**, **8 full-app UI checks**, and **4 installer extraction/render checks** passed. The accelerated mock cycle uses 3 seconds idle, 10 seconds normal (last 3 countdown) and 5 seconds black. Mock timings cannot be saved as production defaults. See [acceptance checks](docs/ACCEPTANCE.md), [user review](docs/V0.3-REVIEW.md) and [development checkpoint](PROGRESS.md) for remaining work.

Please report feedback through [GitHub Issues](https://github.com/erencevikk/FlightOLEDGuardian/issues). Include Guardian/MSFS versions, the selected AGL/MSL threshold, the observed behavior and whether the issue occurs in mock or live mode. Describe your display/capture setup when relevant. Remove your SimBrief identity and other personal data from screenshots and logs.

License: [MIT](LICENSE).

---

## Türkçe

**Bağımsız Windows x64 sürümü:** .NET **8.0.34** pakete dahildir. ZIP'in tamamını çıkartıp `FlightOLEDGuardian.App.exe` dosyasını çalıştırın; ayrıca .NET indirmeniz gerekmez. Bütün dosya ve alt klasörleri birlikte tutun. Gömülü CLR yolu ve 8 arayüz kontrolü doğrulandı. Küçük Setup paketinin .NET gereksinimi aşağıda ayrıca açıklanır.

Fly longer. Protect your OLED. / Daha uzun uç. OLED ekranını koru.

Windows 10/11 x64 için WPF uygulaması. Bu sürüm .NET 8 SDK ile derlenir; çalıştırmak için .NET 8 Desktop Runtime x64 gerekir.

## v0.3.0

Önerilen dağıtım **Setup**, alternatif dağıtım taşınabilir `App` klasörüdür. `FlightOLEDGuardian-v0.3-Setup.exe` bu Windows hesabına kurar; Başlat menüsü, isteğe bağlı masaüstü kısayolu ve Windows kaldırma kaydı oluşturur. Kurulum yönetici yetkisi istemez. Önce çalışan Guardian örneğini tepsi menüsünden kapatın. .NET 8 Desktop Runtime x64 yoksa Setup resmî indirme sayfasını açar; runtime kurulduktan sonra Setup'ı yeniden çalıştırın.

Varsayılan dil English; ilk açılışta ayrıca dil seçimi sorulur. Varsayılan referans AGL'dir. SimBrief kimliği varsayılan gizli, yayın modunda zorunlu gizlidir. Kimlik yerel ayar dosyasında saklanır; gizleme bir ekran gizliliği özelliğidir.

Her başlangıçta arka planda güncelleme kontrol edilir; Ayarlar'dan elle de çalıştırılabilir. Sürümler `erencevikk/FlightOLEDGuardian` deposundaki GitHub Releases üzerinden dağıtılır. v0.3.0 yayımlandı ve uygulamanın kullandığı latest-release uç noktası doğrulandı. Güncelleme indirme veya kurma otomatik yapılmaz.

Yeni uçak/kalkan logosu EXE, pencere, tepsi ve kurulumda kullanılır. Standart küçültme görev çubuğunda kalır; özel tepsi düğmesi tepsiye saklar. Ayrıntılı test sonucu ve eksikler: `docs/V0.3-REVIEW.md`.

Kurulumdan kaldırmadan önce uygulamada MSFS ile başlatmayı kapatın. Taşınabilir sürümden kurulum sürümüne geçerken otomatik başlatma açıksa Ayarlar'ı kaydedin; EXE.xml yolu güncellenir. Ayarlar ve raporlar kurulum klasöründen ayrı tutulur ve kaldırmada korunur.

## Başlatma

Paketin `App` klasöründeki `FlightOLEDGuardian.App.exe` dosyasını açın. EXE, DLL ve JSON dosyalarını birlikte tutun. İlk açılışta **Türkçe / English** seçilir; dil daha sonra üstteki menüden veya Ayarlar'dan anında değiştirilebilir. Dil tercihi kaydedilir.

Simülatör olmadan denemek için `Start-Mock.cmd` dosyasını açın. Normal kullanım için `Start-Guardian.cmd` kullanılabilir.

## v0.2 düzeltmeleri

- Türkçe/İngilizce arayüz, yerelleştirilmiş profil/mod seçenekleri, tepsi menüsü, uyarılar ve PNG rapor etiketleri.
- MSFS'nin kendi **EXE.xml / Launch.Addon** yöntemiyle başlatma. İlk açılışta sorulur ve Ayarlar'dan değiştirilir. Windows başlangıcına yeni izleyici eklenmez.
- SimBrief **Navigraph Alias** veya **Pilot ID** ile son oluşturulan uçuş planını indirme. XML dosyası içe aktarma ikincil seçenek olarak korunur.
- Seçili öğesi ve açılan listesi birlikte koyu temaya sahip ComboBox'lar; beyaz üstüne beyaz metin kaldırıldı. Dil değişiminde profil/monitör seçimi korunur.
- Mock irtifa ve TOD için iki yönlü sayısal giriş; TOD değeri ayrıca büyük ve belirgin gösterilir.
- Kurulu resmi x64 SimConnect istemcisini bulma/yükleme, yanlış mimariyi reddetme ve elle DLL seçimi.

## MSFS ile otomatik başlatma

İlk açılıştaki soruya **Evet** derseniz veya **Ayarlar → Guardian'ı MSFS ile başlat** seçeneğini açıp kaydederseniz, seçilen MSFS EXE.xml dosyasına yalnızca Guardian girdisi eklenir. Diğer eklentiler korunur; değişiklikten önce aynı klasöre tarihli yedek alınır. Aynı girdinin iki defa eklenmesi engellenir. Kapatıldığında yalnızca Guardian girdisi kaldırılır.

MSFS 2020/2024 ve Store/Steam için bilinen kullanıcı yapılandırma konumları kontrol edilir. Gerekirse **MSFS EXE.xml dosyasını seç** ile doğru dosyayı belirtin. Genel EXE.xml başlangıcı devre dışıysa Guardian bunu sessizce açmaz. Uygulamayı sabit bir klasöre yerleştirdikten sonra bu tercihi etkinleştirin; EXE'yi taşırsanız kayıtlı yolu güncelleyin.

v0.1'de Windows izleyicisini etkinleştirmiş kullanıcılar yeni yöntemi Ayarlar'dan kaydettiğinde eski Guardian Run girdisi kaldırılır. Derleme ve testler gerçek başlangıç kaydını veya gerçek MSFS EXE.xml dosyanızı değiştirmez.

## SimBrief

Gösterge panelinde veya Ayarlar'da Alias/Pilot ID girin. Sayısal ID kullanıyorsanız ilgili kutuyu işaretleyin. Ardından **SimBrief uçuş planını getir** düğmesine basın. Şifre istenmez; kimlik yerel ayarlarda tutulur. Daha önce SimBrief'te bir OFP oluşturmuş olmalısınız.

Resmi `xml.fetcher.php` uç noktası ve `client=FlightOLEDGuardian-0.2` tanımlaması kullanılır. İndirme yalnızca düğmeye basıldığında yapılır; arka planda sürekli API sorgusu yoktur. İstek 20 saniyeyle ve yanıt 2 MB ile sınırlıdır. Kullanıcı bulunamama, bağlantı/zaman aşımı ve bozuk plan hataları arayüzde görünür.

OFP'nin navlog/TOD noktası varsa kalan **rota mesafesi** hesaplanır. Bu **Planlanan TOD** bilgisidir; uçağın gerçek FMS/VNAV TOD'u değildir. TOD yoksa plan özeti gösterilir, TOD uydurulmaz. Aynı plan yeniden indirildiğinde kimliği değişmez; 50 NM bildirimi tekrar başlamaz. Rota değişirse yeniden indirin. Rotadan uzaklaşma veya bacak belirsizliğinde mesafe gösterilmez.

## SimConnect

Guardian artık yalnızca EXE'nin yanındaki DLL'ye bakmaz. MSFS SDK ortam değişkenleri ve bilinen SDK konumları, kurulu BeyondATC/MobiFlight istemcileri kontrol edilir. Yalnızca x64 native istemciler ve gerekli native export'ları olan DLL'ler kabul edilir. Eski FSX/x86 DLL'si kullanılmaz. Ayarlar'dan resmi `SimConnect.dll` elle de seçilebilir. Başka DLL zaten yüklenmişse değiştirmek için Guardian yeniden başlatılmalıdır.

Bu bilgisayardaki MSFS 2024 SDK istemcisinin yüklenmesi doğrulandı. Son canlı bağlantı denemesinde MSFS açık değildi; simülatörden canlı telemetri alınması henüz doğrulanmadı. DLL yüklenmişken MSFS kapalı olması, DLL eksikliğiyle aynı hata olarak gösterilmez. SDK istemci DLL'si bu arşivde yeniden dağıtılmaz.

Bağlantı yerel `SIMCONNECT_OPEN_CONFIGINDEX_LOCAL` üzerinden denenir. Uçuş kontrolüne yazılmaz; yalnızca telemetri ve sistem olayları okunur.

## Koruma / test laboratuvarı

Varsayılan: 7.000 FT AGL, 60 saniye hareketsizlik, 540 saniye normal görüntü (son 10 saniye geri sayım) ve 60 saniye siyah. İrtifa doğrulaması 3 saniye, histerezis 200 FT'dir. Full ve Adaptive modları, VFR/IFR profilleri, Skip/Snooze ve manuel koruma vardır.

**Test laboratuvarı → Hızlandırılmış test süreleri** ile 3 sn hareketsizlik + 10 sn normal (son 3 sn geri sayım) + 5 sn siyah kullanılır. İrtifa ve TOD hem sürgüden hem sayısal alandan değiştirilebilir. Hızlandırılmış değerler üretim varsayılanlarına kaydedilemez. Normal değerlere dönmek için yeniden Test/MSFS bağlantısını seçin.

Klavye, fare ve anlamlı WinMM joystick hareketi korumayı kapatır. Acil kısayol Ctrl+Alt+F12; kullanılamazsa Ctrl+Shift+F12 denenir. İkisi de kaydedilemezse koruma askıya alınır. Kısıtlı test ortamında global kısayol kaydı doğrulanamadı. Bağlantı kaybı, duraklatma, bayat/geçersiz telemetri ve monitör/güç değişikliklerinde güvenli kapanma uygulanır.

## Rapor ve yayın

Rapor için gözlenen kalkış ve touchdown sonrası 80 kt altında 15 saniye kararlı yerde kalma gerekir. Tekrar havalanma bekleyen raporu iptal eder. Test raporları gerçek toplamlara dahil edilmez. İniş hızı son alınan havadaki dikey hız örneğidir; kesin temas ölçümü değildir. PNG çıktısı 1920×1080, 1080×1920 veya 1080×1080 olabilir.

Streaming modunda doğrulanmamış capture düzenleri varsayılan olarak korumayı durdurur. Windows capture exclusion isteğinin kabul edilmesi OBS/HDMI uyumluluğu garantisi değildir. Test kaydı şarttır. Klonlanmış HDMI siyah katmanı da taşıyabilir. Bu oturumda OBS/HDMI doğrulaması yapılmadı.

Ayarlar ve bağımsız JSON raporları `%LOCALAPPDATA%/FlightOLEDGuardian` altında tutulur. `--data-dir "C:\path"` ile test verileri ayrı klasöre yönlendirilebilir.

## Geliştirme

```powershell
.\build.ps1 -UiTest -Smoke -Publish
.\package.ps1
```

NuGet paketi gerekmez. Script, işlem ortamı değişkenlerini sonunda geri yükler. `-Test` başsız testleri; `-UiTest` ek WPF/overlay testlerini çalıştırır. `-Smoke` tam uygulama üzerinde TR/EN değişimi, koyu açılır listeler, seçimin korunması ve mock sayısal alanlarını kontrol eder; render dosyaları `work/smoke-v03-final` altına yazılır. Testler hata halinde sıfır dışı çıkış kodu veren bağımsız C# çalıştırıcılarıdır.

Salt okunur canlı SimConnect denemesi:

```powershell
dotnet tests/FlightOLEDGuardian.IntegrationTests/bin/ReleaseVerified/FlightOLEDGuardian.IntegrationTests.dll --live
```

Bu komut koruma başlatmaz, MSFS'yi değiştirmez ve 20 saniye içinde sonuç verir. Gerçek ortam kabul listesi: [docs/ACCEPTANCE.md](docs/ACCEPTANCE.md).
