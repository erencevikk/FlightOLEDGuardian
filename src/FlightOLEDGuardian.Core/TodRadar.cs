namespace FlightOLEDGuardian.Core;

public record GeoPoint(double Latitude, double Longitude);
public record TodReading(double? NauticalMiles, string Quality, string PlanId, string Explanation);
public sealed class TodAlert
{
    private string? plan;
    private bool fired;
    public void Reset(){plan=null;fired=false;}
    public bool Evaluate(TodReading value)
    {
        if (plan != value.PlanId) { plan = value.PlanId; fired = false; }
        if (fired || value.NauticalMiles is not double distance || !double.IsFinite(distance) || distance is < 0 or > 50 || value.Quality == "Unavailable") return false;
        fired = true; return true;
    }
}

public static class TodRadar
{
    public static double Distance(GeoPoint a, GeoPoint b)
    {
        double r = Math.PI / 180, dlat = (b.Latitude-a.Latitude)*r, dlon = (b.Longitude-a.Longitude)*r;
        double h = Math.Pow(Math.Sin(dlat/2),2) + Math.Cos(a.Latitude*r)*Math.Cos(b.Latitude*r)*Math.Pow(Math.Sin(dlon/2),2);
        return 3440.065 * 2 * Math.Asin(Math.Sqrt(Math.Clamp(h,0,1)));
    }
    // Project onto each great-circle leg; reject ambiguous/remote legs instead of inventing a distance.
    public static TodReading AlongRoute(GeoPoint position, IReadOnlyList<GeoPoint> route, int todIndex, string planId, int minimumLeg = 0)
    {
        bool ValidPoint(GeoPoint p)=>double.IsFinite(p.Latitude) && double.IsFinite(p.Longitude) && Math.Abs(p.Latitude)<=90 && Math.Abs(p.Longitude)<=180;
        if (todIndex < 1 || todIndex >= route.Count || minimumLeg >= todIndex || !ValidPoint(position) || route.Any(p=>!ValidPoint(p))) return new(null,"Unavailable",planId,"No usable TOD route");
        double bestCross = double.MaxValue, remaining = 0;
        var candidates=new List<(double Cross,double Remaining)>();
        for (int i = Math.Max(0,minimumLeg); i < todIndex; i++)
        {
            double leg = Distance(route[i],route[i+1]); if (leg < .01) continue;
            double d = Distance(route[i],position)/3440.065;
            double delta = Bearing(route[i],position)-Bearing(route[i],route[i+1]);
            double cross = Math.Asin(Math.Clamp(Math.Sin(d)*Math.Sin(delta),-1,1))*3440.065;
            double along = Math.Atan2(Math.Sin(d)*Math.Cos(delta),Math.Cos(d))*3440.065;
            if (along < -2 || along > leg+2) continue;
            double legRemaining=leg-Math.Clamp(along,0,leg);
            for (int j=i+1;j<todIndex;j++) legRemaining += Distance(route[j],route[j+1]);
            candidates.Add((Math.Abs(cross),legRemaining));
            if(Math.Abs(cross)<bestCross){bestCross=Math.Abs(cross);remaining=legRemaining;}
        }
        bool ambiguous=candidates.Any(c=>c.Cross<=bestCross+.5 && Math.Abs(c.Remaining-remaining)>10);
        return bestCross > 10 || ambiguous ? new(null,"Unavailable",planId,"Off route or active leg ambiguous") : new(remaining,"Planned TOD",planId,"Route distance to planned TOD; not aircraft FMS/VNAV");
    }
    private static double Bearing(GeoPoint a, GeoPoint b)
    {
        double r=Math.PI/180, p=a.Latitude*r,q=b.Latitude*r,l=(b.Longitude-a.Longitude)*r;
        return Math.Atan2(Math.Sin(l)*Math.Cos(q),Math.Cos(p)*Math.Sin(q)-Math.Sin(p)*Math.Cos(q)*Math.Cos(l));
    }
}
