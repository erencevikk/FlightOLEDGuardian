using FlightOLEDGuardian.Core;
using System.Text.Json;

namespace FlightOLEDGuardian.Reporting;

public record FlightReport(string Id, string Aircraft, bool IsMock, DateTimeOffset TouchdownUtc, double FlightSeconds, double EligibleSeconds, double BlackSeconds, double LongestBlackSeconds, int Cycles, int Skips, int Wakes, double? LandingRate, double TouchdownGroundSpeed);

public sealed class FlightRecorder
{
    private bool started, completed;
    private double takeoff, previousTime, blackRun, groundSince;
    private double flightSeconds, eligibleSeconds, blackSeconds, longest;
    private int startCycles, startSkips, startWakes;
    private FlightTelemetry? previous, touchdown;
    private DateTimeOffset touchdownUtc;
    private bool previouslyEligible, previouslyBlack;
    public bool AirborneFlight => started && !completed;
    public FlightReport? Update(double now, DateTimeOffset utc, FlightTelemetry f, ProtectionScheduler scheduler)
    {
        if (!f.Valid || now - f.ReceivedAt > 5) { previous = null; previousTime = now; blackRun = 0; previouslyEligible=previouslyBlack=false; return null; }
        if (!started || completed)
        {
            // Require an observed ground-to-air transition, not connection at cruise.
            if (previous is { OnGround: true } && !f.OnGround)
            {
                started = true; completed = false; takeoff = now; flightSeconds = eligibleSeconds = blackSeconds = longest = blackRun = 0;
                startCycles = scheduler.CompletedCycles; startSkips = scheduler.ManualSkips; startWakes = scheduler.WakeEvents;
                touchdown = null;
            }
            previous = f; previousTime = now; previouslyEligible=scheduler.Eligible;previouslyBlack=scheduler.State==ProtectionState.Black;return null;
        }
        var dt = Math.Clamp(now-previousTime,0,2);
        if (previous is { OnGround: false }) flightSeconds += dt;
        if (previouslyEligible) eligibleSeconds += dt;
        if (previouslyBlack) { blackSeconds += dt; blackRun += dt; longest = Math.Max(longest,blackRun); } else blackRun = 0;
        previouslyEligible=scheduler.Eligible;previouslyBlack=scheduler.State==ProtectionState.Black;
        if (previous is { OnGround: false } && f.OnGround)
        { touchdown = previous with { GroundSpeed = f.GroundSpeed }; touchdownUtc = utc; groundSince = now; }
        if (!f.OnGround) touchdown = null; // Bounce/touch-and-go/go-around cancels pending report.
        previous = f; previousTime = now;
        if (touchdown != null && now-groundSince >= 15 && f.GroundSpeed < 80 && now-takeoff >= 30)
        {
            completed = true;
            return new(Guid.NewGuid().ToString("N"),f.Aircraft,f.IsMock,touchdownUtc,flightSeconds,eligibleSeconds,blackSeconds,longest,
                scheduler.CompletedCycles-startCycles,scheduler.ManualSkips-startSkips,scheduler.WakeEvents-startWakes,
                touchdown.VerticalSpeed,touchdown.GroundSpeed);
        }
        return null;
    }
}

public sealed class ReportStore(string directory)
{
    public IReadOnlyList<FlightReport> Load()
    {
        if (!Directory.Exists(directory)) return [];
        var reports = new List<FlightReport>();
        foreach (var file in Directory.EnumerateFiles(directory,"flight-*.json"))
        {
            try { var r=JsonSerializer.Deserialize<FlightReport>(File.ReadAllText(file)); if(r!=null) reports.Add(r); }
            catch (JsonException) { } // One damaged report must not hide remaining flights.
        }
        return reports.OrderByDescending(r=>r.TouchdownUtc).ToArray();
    }
    public void Save(FlightReport report)
    {
        Directory.CreateDirectory(directory);
        var path=Path.Combine(directory,$"flight-{report.Id}.json");
        File.WriteAllText(path+".tmp",JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
        File.Move(path+".tmp",path,true);
    }
}
