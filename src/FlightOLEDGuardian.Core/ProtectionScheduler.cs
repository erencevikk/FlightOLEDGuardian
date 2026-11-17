namespace FlightOLEDGuardian.Core;

// All time values are monotonic elapsed seconds, never wall-clock dates.
public sealed class ProtectionScheduler
{
    private GuardianSettings settings;
    private double? aboveSince, normalSince, blackSince;
    private double snoozedUntil;
    private bool altitudeArmed, manual;
    public ProtectionState State { get; private set; } = ProtectionState.Disconnected;
    public double RemainingSeconds { get; private set; }
    public bool Eligible { get; private set; }
    public bool Suspended { get; private set; }
    public int CompletedCycles { get; private set; }
    public int ManualSkips { get; private set; }
    public int WakeEvents { get; private set; }
    public ProtectionScheduler(GuardianSettings settings) { settings.Validate(); this.settings = settings; }
    public void Configure(GuardianSettings value) { value.Validate(); settings = value; Reset(); altitudeArmed = false; aboveSince = null; }
    public void Reset() { State = ProtectionState.Ineligible; normalSince = blackSince = null; manual = false; RemainingSeconds = 0; }
    public void Wake() { if (State == ProtectionState.Black) WakeEvents++; Reset(); }
    public void Skip(double now) { ManualSkips++; Reset(); normalSince = now; }
    public void Snooze(double now, double seconds = 600) { Wake(); snoozedUntil = now + seconds; }
    public void Suspend(bool value) { Suspended = value; Wake(); }
    public void ManualProtect(double now) { if (!Suspended) { manual = true; blackSince = now; State = ProtectionState.Black; } }
    public ProtectionState Tick(double now, double idle, FlightTelemetry flight, bool captureAllowed = true)
    {
        Eligible = false;
        if (!flight.Valid || now - flight.ReceivedAt > settings.StaleSeconds || flight.ReceivedAt > now)
        { Wake(); altitudeArmed = false; aboveSince = null; return State = flight.Connected ? ProtectionState.Ineligible : ProtectionState.Disconnected; }
        if (Suspended || !captureAllowed) { Wake(); return State = ProtectionState.Suspended; }
        if (now < snoozedUntil) { Reset(); RemainingSeconds = snoozedUntil - now; return State = ProtectionState.Snoozed; }
        var altitude = settings.Reference == AltitudeReference.MSL ? flight.AltitudeMSL : flight.AltitudeAGL;
        if (flight.OnGround || altitude < settings.ActivationFeet - settings.HysteresisFeet)
        { altitudeArmed = false; aboveSince = null; }
        else if (altitude >= settings.ActivationFeet)
        { aboveSince ??= now; if (now - aboveSince >= settings.AltitudeConfirmationSeconds) altitudeArmed = true; }
        else if (!altitudeArmed) aboveSince = null;
        Eligible = !flight.OnGround && altitudeArmed && (!settings.SuppressManeuvers || (Math.Abs(flight.BankDegrees) < 15 && Math.Abs(flight.VerticalSpeed) < 1500));
        if (manual) { RemainingSeconds = Math.Max(0, settings.BlackSeconds - (now - blackSince!.Value)); if (RemainingSeconds > 0) return State = ProtectionState.Black; Reset(); }
        if (!Eligible) { Wake(); return State = ProtectionState.Ineligible; }
        if (idle < settings.IdleSeconds) { Wake(); return State = ProtectionState.WaitingForIdle; }
        bool full = settings.Mode == ProtectionMode.FullProtection || settings.Mode == ProtectionMode.AdaptiveProtection && idle >= settings.AdaptiveSeconds;
        if (full)
        {
            normalSince ??= now;
            if (State != ProtectionState.Black && now - normalSince.Value < settings.CountdownSeconds)
            { RemainingSeconds = settings.CountdownSeconds - (now - normalSince.Value); return State = ProtectionState.Countdown; }
            blackSince ??= now; RemainingSeconds = double.PositiveInfinity; return State = ProtectionState.Black;
        }
        normalSince ??= now;
        if (blackSince.HasValue)
        {
            RemainingSeconds = settings.BlackSeconds - (now - blackSince.Value);
            if (RemainingSeconds > 0) return State = ProtectionState.Black;
            CompletedCycles++; blackSince = null; normalSince = now;
        }
        var elapsed = now - normalSince.Value;
        if (elapsed >= settings.NormalSeconds) { blackSince = now; RemainingSeconds = settings.BlackSeconds; return State = ProtectionState.Black; }
        RemainingSeconds = settings.NormalSeconds - elapsed;
        return State = RemainingSeconds <= settings.CountdownSeconds ? ProtectionState.Countdown : ProtectionState.Normal;
    }
}
