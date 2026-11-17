using System.Windows;

namespace FlightOLEDGuardian.Windows;

public static class UiText
{
    public static string Language { get; private set; }="en";
    public static event Action? Changed;
    private static readonly Dictionary<string,string> Turkish=new(StringComparer.Ordinal)
    {
        ["Fly longer. Protect your OLED."]="Daha uzun uç. OLED ekranını koru.",
        ["Minimize to tray"]="Sistem tepsisine küçült",["Dashboard"]="Gösterge paneli",["Protection"]="Koruma",["Mock lab"]="Test laboratuvarı",["Reports"]="Raporlar",["Streaming"]="Yayın",["Settings"]="Ayarlar",
        ["PROTECTION STATUS"]="KORUMA DURUMU",["FLIGHT TELEMETRY"]="UÇUŞ TELEMETRİSİ",["TOD RADAR"]="TOD RADARI",
        ["Import SimBrief XML"]="XML dosyası içe aktar",["Fetch SimBrief flight plan"]="SimBrief uçuş planını getir",["Fetching flight plan…"]="Uçuş planı alınıyor…",
        ["SimBrief Alias or Pilot ID"]="SimBrief Alias veya Pilot ID",["Use numeric Pilot ID"]="Sayısal Pilot ID kullan",["No password required. Fetches your latest generated OFP when you click Import."]="Şifre gerekmez. Düğmeye bastığında son oluşturduğun uçuş planı alınır.",
        ["Hide SimBrief identity"]="SimBrief kimliğini gizle",["Streaming mode always hides your Alias/Pilot ID."]="Yayın modu Alias/Pilot ID bilgisini daima gizler.",["Identity hidden for streaming"]="Kimlik yayın için gizlendi",
        ["Protect now"]="Şimdi koru",["Wake / reset"]="Ekranı aç / sıfırla",["Skip once"]="Bir kez atla",["Snooze 10 min"]="10 dakika ertele",["Suspend / resume"]="Durdur / devam et",
        ["Your flight, your protection"]="Uçuşun, ekran koruman",["Aircraft profile"]="Uçuş profili",["Protection mode"]="Koruma modu",["Activation altitude (feet)"]="Aktivasyon irtifası (feet)",
        ["Idle time (seconds)"]="Hareketsizlik süresi (saniye)",["Normal display (seconds)"]="Normal görüntü süresi (saniye)",["Altitude reference"]="İrtifa referansı",["Black display (seconds)"]="Siyah ekran süresi (saniye)",["Countdown (seconds, included in normal time)"]="Geri sayım (saniye; normal süreye dahil)",
        ["Dim moving telemetry during black screen"]="Siyah ekranda düşük parlaklıklı hareketli telemetri göster",["Protected monitor"]="Korunacak monitör",["Apply and save settings"]="Uygula ve kaydet",
        ["Simulator-free test bench"]="Simülatör olmadan test et",["Mock data is explicitly labeled and never presented as a real flight."]="Test verileri açıkça etiketlenir; gerçek uçuş verisi olarak gösterilmez.",
        ["Connect MOCK"]="Test moduna bağlan",["Connect MSFS"]="MSFS'ye bağlan",["Accelerated mock timings"]="Hızlandırılmış test süreleri",["Mock altitude MSL (feet)"]="Test irtifası MSL (feet)",["On ground / touchdown"]="Yerde / iniş",["Simulator paused"]="Simülatör duraklatıldı",["Disconnect telemetry"]="Telemetri bağlantısını kes",["Mock TOD distance (NM)"]="Test TOD mesafesi (NM)",["Simulate idle (accelerated MOCK only)"]="Hareketsizliği simüle et (hızlandırılmış test)",
        ["Accelerated mode: 3s idle + 10s normal (last 3s countdown) + 5s black. Keyboard and mouse still wake protection."]="Hızlandırılmış mod: 3 sn hareketsizlik + 10 sn normal görüntü (son 3 sn geri sayım) + 5 sn siyah ekran. Fare ve klavye ekranı açar.",
        ["Flight protection report"]="Uçuş koruma raporu",["Export 1920 × 1080 PNG"]="1920 × 1080 PNG kaydet",
        ["Landing rate uses the last received airborne vertical-speed sample; it is not a certified touchdown measurement. Reports require observed takeoff and 15 seconds of stable ground below 80 kt."]="İniş hızı son alınan havadaki dikey hız örneğidir; kesin temas ölçümü değildir. Rapor için kalkışın gözlenmesi ve inişten sonra 80 kt altında 15 saniye yerde kalınması gerekir.",
        ["Streaming safe mode"]="Yayın için güvenli mod",["Enable streaming mode"]="Yayın modunu aç",["Capture method"]="Görüntü yakalama yöntemi",["Request Windows capture exclusion"]="Guardian katmanını yakalamadan hariç tutmayı dene",["Disable protection for unverified capture setups"]="Doğrulanmamış yayın düzenlerinde korumayı durdur",["I recorded a test and confirmed that this setup preserves simulator video"]="Test kaydı aldım; bu düzende uçuş görüntüsünün korunduğunu doğruladım",["Apply streaming settings"]="Yayın ayarlarını uygula",
        ["HDMI capture cards and cloned outputs can include the black overlay. Windows capture exclusion does not protect a physical HDMI signal. Use an independent capture display and protect only your OLED."]="HDMI yakalama kartı ve klonlanmış görüntü siyah katmanı da taşıyabilir. Windows yakalama hariç tutma özelliği fiziksel HDMI sinyalini korumaz. Bağımsız bir capture çıkışı kullanıp yalnızca OLED ekranını karart.",
        ["General"]="Genel",["Language"]="Dil",["Start Guardian with MSFS (EXE.xml)"]="Guardian'ı MSFS ile başlat (EXE.xml)",["Select MSFS EXE.xml"]="MSFS EXE.xml dosyasını seç",["MSFS launches Guardian directly; no Windows startup watcher is installed."]="Guardian'ı doğrudan MSFS açar; Windows başlangıcına izleyici eklenmez.",
        ["SimConnect client DLL"]="SimConnect istemci DLL'si",["Find / select SimConnect.dll"]="SimConnect.dll bul / seç",["Automatic installed-client detection"]="Kurulu istemciyi otomatik bul",
        ["TOD 50 NM visual alert"]="TOD'a 50 NM kala görsel uyarı",["TOD spoken notification (Windows voice)"]="TOD sesli bildirimi (Windows sesi)",["Prefer Turkish voice"]="Türkçe sesi tercih et",["Save settings"]="Ayarları kaydet",
        ["No account or internet required. No flight controls are written. OLED protection reduces time displaying static content; burn-in prevention is not guaranteed."]="Temel koruma internetsiz çalışır. SimBrief indirmesi isteğe bağlıdır. Uçuş kumandalarına müdahale edilmez. OLED koruması sabit görüntü süresini azaltır; yanık oluşmamasını garanti etmez.",
        ["IFR / Airliner"]="IFR / Yolcu uçağı",["VFR / General aviation"]="VFR / Genel havacılık",["Custom"]="Özel",["PeriodicBlack"]="Periyodik siyah ekran",["FullProtection"]="Tam koruma",["AdaptiveProtection"]="Uyarlanabilir koruma",["Game"]="Oyun yakalama",["Window"]="Pencere yakalama",["Display"]="Ekran yakalama",["CaptureCard"]="HDMI yakalama kartı",
        ["Disconnected"]="Bağlantı yok",["Ineligible"]="Koşullar uygun değil",["WaitingForIdle"]="Hareketsizlik bekleniyor",["Normal"]="Normal görüntü",["Countdown"]="Geri sayım",["Black"]="Siyah ekran",["Snoozed"]="Ertelendi",["Suspended"]="Koruma durduruldu",["Unavailable"]="Kullanılamıyor",["On ground"]="Yerde",["Airborne"]="Havada",["ON"]="AÇIK",["OFF"]="KAPALI",
        ["TOD unavailable"]="TOD bilgisi yok",["Planned TOD"]="Planlanan TOD",["No verified FMS or planned TOD data"]="Doğrulanmış FMS veya planlanan TOD verisi yok",["Route distance to planned TOD; not aircraft FMS/VNAV"]="Planlanan TOD'a rota mesafesi; uçağın FMS/VNAV değeri değildir",["Off route or active leg ambiguous"]="Rotadan uzakta veya aktif rota bacağı belirsiz",["No usable TOD route"]="TOD hesabı için uygun rota yok",["Simulated test data; not a real FMS TOD"]="Simüle test verisi; gerçek FMS TOD'u değildir",
        ["Open Guardian"]="Guardian'ı aç",["Snooze 10 minutes"]="10 dakika ertele",["Exit"]="Çıkış",["SKIP ONCE"]="BİR KEZ ATLA",["SNOOZE 10 MIN"]="10 DK ERTELE",["OLED PROTECTION"]="OLED KORUMASI",["TOP OF DESCENT APPROACHING"]="ALÇALIŞ BAŞLANGICI YAKLAŞIYOR",
        ["Welcome to FlightOLED Guardian"]="FlightOLED Guardian'a hoş geldin",["Start Guardian automatically when Microsoft Flight Simulator starts?\n\nThis adds Guardian to MSFS EXE.xml. Existing addons are preserved and a backup is created. You can change this in Settings."]="Microsoft Flight Simulator açıldığında Guardian da otomatik açılsın mı?\n\nBu tercih Guardian'ı MSFS EXE.xml dosyasına ekler. Diğer eklentiler korunur ve yedek alınır. Tercihini Ayarlar'dan değiştirebilirsin.",
        ["Settings saved"]="Ayarlar kaydedildi",["Settings not saved"]="Ayarlar kaydedilemedi",["Protection safely suspended"]="Koruma güvenli biçimde durduruldu",["Protection suspended"]="Koruma durduruldu",["Emergency hotkey unavailable"]="Acil kısayol kullanılamıyor",["Connecting to MSFS"]="MSFS'ye bağlanılıyor",["MOCK mode enabled"]="Test modu açıldı",["MOCK telemetry"]="TEST telemetrisi",["MOCK / simulated TOD"]="TEST / simüle TOD",["MOCK accelerated test"]="Hızlandırılmış TEST",["MOCK • no simulator connected"]="TEST • gerçek simülatör bağlı değil",["Mock A320"]="Test A320",
        ["SimConnect DLL loaded; waiting for a running MSFS connection"]="SimConnect DLL yüklendi; çalışan MSFS bağlantısı bekleniyor",["Waiting for MSFS / SimConnect.dll"]="MSFS / SimConnect bağlantısı bekleniyor",["SimConnect connected; waiting for telemetry"]="SimConnect bağlandı; telemetri bekleniyor",["SimConnect • live telemetry"]="SimConnect • canlı telemetri",["MSFS closed"]="MSFS kapandı",
        ["Your report appears after an observed takeoff and a confirmed landing. No completed flight recorded yet."]="Raporun, gözlenen kalkış ve doğrulanmış inişten sonra görünür. Henüz tamamlanan uçuş yok.",
        ["FLIGHT COMPLETED"]="UÇUŞ TAMAMLANDI",["MOCK / TEST DATA"]="TEST VERİSİ",["FLIGHT TIME"]="UÇUŞ SÜRESİ",["OLED BLACK TIME"]="OLED SİYAH EKRAN SÜRESİ",["ELIGIBLE TIME"]="KORUMAYA UYGUN SÜRE",["Longest black interval"]="En uzun siyah ekran",["Landing rate (sampled)"]="İniş hızı (örneklenmiş)",["Ground speed at touchdown"]="İnişte yer hızı",["REAL FLIGHT HISTORY"]="GERÇEK UÇUŞ GEÇMİŞİ",["ACHIEVEMENTS"]="BAŞARIMLAR",
        ["COMPLETED CYCLES"]="TAMAMLANAN DÖNGÜ",["LONGEST BLACK INTERVAL"]="EN UZUN SİYAH EKRAN",["LANDING RATE · SAMPLED"]="İNİŞ HIZI · ÖRNEKLENMİŞ",["Protected with FlightOLED Guardian"]="FlightOLED Guardian ile korundu",["Measured display black time. No burn-in guarantee."]="Ölçülen siyah ekran süresi. Ekran yanığı garantisi değildir.",
        ["SimBrief user or generated flight plan not found. Check your Alias/Pilot ID and generate an OFP first."]="SimBrief kullanıcısı veya oluşturulmuş plan bulunamadı. Alias/Pilot ID'yi kontrol et ve önce uçuş planını oluştur.",["Enter a Navigraph Alias or a valid 1–7 digit SimBrief Pilot ID."]="Navigraph Alias veya geçerli 1–7 haneli SimBrief Pilot ID gir.",
        ["Streaming mode off"]="Yayın modu kapalı",["Not capture-tested. Record a sample in OBS before enabling protection while streaming."]="Yakalama testi yapılmadı. Yayında korumayı açmadan önce OBS ile test kaydı al.",["User-confirmed capture setup. Re-test after changing OBS or display settings."]="Kullanıcı tarafından doğrulanan yayın düzeni. OBS veya ekran ayarlarını değiştirince yeniden test et.",["HDMI / cloned output includes the overlay. Use a separate unprotected output; no hardware compatibility verified."]="HDMI / klonlanmış çıkış katmanı içerebilir. Ayrı, karartılmayan çıkış kullan; donanım uyumluluğu doğrulanmadı.",
        ["Capture exclusion not tested"]="Yakalama hariç tutma denenmedi",["Windows accepted display affinity; OBS/capture hardware still requires a recording test"]="Windows hariç tutma isteğini kabul etti; OBS/capture için kayıt testi yine gerekli",["Windows refused display affinity; capture exclusion unavailable"]="Windows hariç tutma isteğini reddetti; yakalamadan hariç tutma kullanılamıyor"
    };
    public static IReadOnlyDictionary<string,string> Translations => Turkish;
    public static string ResourceKey(string text) => "Ui_"+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)))[..16];
    public static string Get(string text,string? language=null) => (language??Language)=="tr" && Turkish.TryGetValue(text,out var translated)?translated:text;
    public static string Relocalize(string text,string language)
    {
        if(language=="tr")return Get(text,language);
        return Turkish.FirstOrDefault(pair=>pair.Value==text).Key??text;
    }
    public static void Apply(string language)
    {
        if(language is not ("tr" or "en"))throw new ArgumentException("Unsupported language");
        Language=language;
        if(Application.Current!=null)foreach(var key in Turkish.Keys)Application.Current.Resources[ResourceKey(key)]=Get(key);
        Changed?.Invoke();
    }
}
