using System.Globalization;
namespace RecipeApp.Services;

/// <summary>UK reference prices. The store's live localized price controls checkout.</summary>
public static class SubscriptionPricing
{
    public const decimal Monthly = 5.99m;
    public const decimal Annual = 49.99m;
    public const decimal ExtraPlaceMonthly = .99m;
    public static string Pounds(decimal value) => "£" + value.ToString("0.00", CultureInfo.InvariantCulture);
    public static string Summary => $"{Pounds(Monthly)}/month or {Pounds(Annual)}/year";
    public static string AnnualDetails => $"{Pounds(Annual / 12)}/month equivalent, billed annually. Save {Pounds(Monthly * 12 - Annual)}/year versus twelve monthly payments at these UK prices.";
    public static string ExtraPlaces(int count)
    {
        if (count is < 1 or > 16) throw new ArgumentOutOfRangeException(nameof(count));
        return $"{count} extra · {Pounds(count * ExtraPlaceMonthly)}/month UK";
    }
}
