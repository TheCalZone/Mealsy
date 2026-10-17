namespace RecipeApp.Services;
public record PlanStatus(bool OwnPlan, bool FamilyEnabled, bool AdFree, long ExpiresAt, bool Owner, string ProductId, string BasePlanId,int ExtraSeats=0);
public static class PremiumEntitlement
{
    public static bool IsActive(PlanStatus plan,long now) => plan is { AdFree:true } && (plan.OwnPlan||plan.FamilyEnabled) && plan.ExpiresAt>now;
}
