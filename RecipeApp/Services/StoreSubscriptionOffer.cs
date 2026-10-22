namespace RecipeApp.Services;

public record StorePricePhase(long Micros,string Price,string Currency,string Period,int Recurrence,int Cycles);
public record StoreOffer(string Id,string Token,IReadOnlyList<StorePricePhase> Phases);
public record SubscriptionQuote(string Token,string Price,string Period,bool SevenDayTrial,long Micros,string Currency)
{
    public string Interval=>Period=="P1M"?"month":"year";
    public string DisplayPrice=>SevenDayTrial?"7 days free":$"{Price} per {Interval}";
    public string Disclosure=>SevenDayTrial
        ?$"Free for 7 days, then {Price} per {Interval}, renewing automatically. Cancel in Google Play before the trial ends to avoid being charged. Google Play confirms the first charge date and final price at checkout."
        :$"{Price} per {Interval}, renewing automatically. Cancel in Google Play before renewal to avoid the next charge. No eligible 7-day trial is available for this option.";
}

/// <summary>Only select the configured acquisition offer returned by Play. Never invent eligibility.</summary>
public static class StoreSubscriptionOffer
{
    public static SubscriptionQuote Select(IEnumerable<StoreOffer> offers,string period,string trialOfferId=null)
    {
        var candidates=offers.Where(o=>!string.IsNullOrWhiteSpace(o.Token)&&o.Phases!=null).ToList();
        bool Renewal(StorePricePhase p)=>p.Micros>0&&!string.IsNullOrWhiteSpace(p.Price)&&!string.IsNullOrWhiteSpace(p.Currency)&&p.Period==period&&p.Recurrence==1;
        var trial=trialOfferId==null?null:candidates.FirstOrDefault(o=>o.Id==trialOfferId&&o.Phases.Count==2
            &&o.Phases[0] is {Micros:0,Recurrence:2,Cycles:1} && (o.Phases[0].Period is "P7D" or "P1W")
            &&Renewal(o.Phases[1])&&o.Phases[0].Currency==o.Phases[1].Currency);
        var selected=trial??candidates.FirstOrDefault(o=>string.IsNullOrEmpty(o.Id)&&o.Phases.Count==1&&Renewal(o.Phases[0]));
        if(selected==null)throw new InvalidOperationException("That plan is not available from Google Play yet. Refresh plans and try again.");
        var price=selected.Phases.Last();
        return new(selected.Token,price.Price,price.Period,trial!=null,price.Micros,price.Currency);
    }
}
