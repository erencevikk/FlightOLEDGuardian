# FlightOLED Guardian v0.3.0

![Guardian](src/FlightOLEDGuardian.App/Assets/Guardian.png)

[Download / İndir](https://github.com/erencevikk/FlightOLEDGuardian/releases/latest) · [Source code / Kaynak kodu](https://github.com/erencevikk/FlightOLEDGuardian)

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
