namespace FlightOLEDGuardian.Core;

public enum ProtectionMode { PeriodicBlack, FullProtection, AdaptiveProtection }
public enum ProtectionState { Disconnected, Ineligible, WaitingForIdle, Normal, Countdown, Black, Snoozed, Suspended }
public enum AltitudeReference { MSL, AGL }
public enum CaptureMethod { Game, Window, Display, CaptureCard }
public record FlightTelemetry
{
    public string Simulator { get; init; } = "Disconnected";
    public string Aircraft { get; init; } = "Unavailable";
    public string SessionId { get; init; } = "";
    public bool Connected { get; init; }
    public bool Running { get; init; } = true;
    public bool Paused { get; init; }
    public bool OnGround { get; init; } = true;
    public double AltitudeMSL { get; init; }
    public double AltitudeAGL { get; init; }
    public double GroundSpeed { get; init; }
    public double VerticalSpeed { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double BankDegrees { get; init; }
    public bool Autopilot { get; init; }
    public bool IsMock { get; init; }
    public double ReceivedAt { get; init; }
    public bool Valid => Connected && Running && !Paused && new[] { AltitudeMSL, AltitudeAGL, GroundSpeed, VerticalSpeed, Latitude, Longitude, BankDegrees, ReceivedAt }.All(double.IsFinite)
        && AltitudeMSL is > -2000 and < 100000 && AltitudeAGL is >= -100 and < 100000 && GroundSpeed is >= 0 and < 2000
        && Math.Abs(Latitude) <= 90 && Math.Abs(Longitude) <= 180;
}

public record GuardianSettings
{
    public int DefaultsRevision { get; init; } = 3;
    public string Language { get; init; } = "en";
    public bool LanguageSelectionCompleted { get; init; }
    public string SimBriefIdentity { get; init; } = "";
    public bool SimBriefUsePilotId { get; init; }
    public bool HideSimBriefIdentity { get; init; } = true;
    public string SimConnectDllPath { get; init; } = "";
    public string MsfsExeXmlPath { get; init; } = "";
    public double ActivationFeet { get; init; } = 7000;
    public AltitudeReference Reference { get; init; } = AltitudeReference.AGL;
    public double HysteresisFeet { get; init; } = 200;
    public double AltitudeConfirmationSeconds { get; init; } = 3;
    public double IdleSeconds { get; init; } = 60;
    public double NormalSeconds { get; init; } = 540;
    public double BlackSeconds { get; init; } = 60;
    public double CountdownSeconds { get; init; } = 10;
    public double AdaptiveSeconds { get; init; } = 1800;
    public double StaleSeconds { get; init; } = 5;
    public ProtectionMode Mode { get; init; }
    public string Profile { get; init; } = "IFR / Airliner";
    public bool SuppressManeuvers { get; init; }
    public bool MiniTelemetry { get; init; }
    public bool Streaming { get; init; }
    public CaptureMethod CaptureMethod { get; init; }
    public bool ExcludeFromCapture { get; init; } = true;
    public bool CaptureVerified { get; init; }
    public bool DisableUnverifiedCapture { get; init; } = true;
    public bool TodAlerts { get; init; } = true;
    public bool TodSound { get; init; } = true;
    public bool TurkishVoice { get; init; } = true;
    public int MonitorIndex { get; init; }
    public bool OnboardingComplete { get; init; }
    public bool AutoLaunch { get; init; }
    public string[] SimulatorProcesses { get; init; } = [];
    public void Validate()
    {
        var values = new[] { ActivationFeet, HysteresisFeet, AltitudeConfirmationSeconds, IdleSeconds, NormalSeconds, BlackSeconds, CountdownSeconds, AdaptiveSeconds, StaleSeconds };
        if (values.Any(v => !double.IsFinite(v)) || ActivationFeet is < 0 or > 60000 || HysteresisFeet is < 0 or > 2000 ||
            AltitudeConfirmationSeconds < 0 || IdleSeconds < 0 || NormalSeconds <= 0 || BlackSeconds <= 0 ||
            CountdownSeconds < 0 || CountdownSeconds > NormalSeconds || AdaptiveSeconds < IdleSeconds || StaleSeconds <= 0 || MonitorIndex < 0 ||
            !Enum.IsDefined(Mode) || !Enum.IsDefined(Reference) || !Enum.IsDefined(CaptureMethod) || Language is not ("tr" or "en"))
            throw new ArgumentException("Invalid protection settings. Countdown must fit inside normal time; durations must be finite and positive.");
    }
    public static GuardianSettings Vfr => new() { Profile = "VFR / General aviation", ActivationFeet = 3000, Reference = AltitudeReference.AGL, IdleSeconds = 120, NormalSeconds = 570, BlackSeconds = 30, SuppressManeuvers = true, TodAlerts = false };
    public static GuardianSettings Demo => new() { Profile = "MOCK accelerated test", IdleSeconds = 3, NormalSeconds = 10, BlackSeconds = 5, CountdownSeconds = 3, AltitudeConfirmationSeconds = 0 };
}

public interface IFlightSimulatorProvider : IDisposable
{
    string Status { get; }
    FlightTelemetry Poll(double now);
}

public sealed class MockTelemetryProvider : IFlightSimulatorProvider
{
    public FlightTelemetry Sample { get; set; } = new() { Simulator = "MOCK • no simulator connected", Aircraft = "Mock A320", Connected = true, IsMock = true, OnGround = false, AltitudeMSL = 35000, AltitudeAGL = 32000, GroundSpeed = 450, Autopilot = true };
    public string Status => "MOCK telemetry";
    public FlightTelemetry Poll(double now) => Sample with { ReceivedAt = now };
    public void Dispose() { }
}
