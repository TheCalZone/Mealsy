namespace RecipeApp.Controls;

/// <summary>Optional banner; hidden until consent and ad loading succeed.</summary>
public class AdBanner : ContentView
{
    private ContentPage page;
    public AdBanner()
    {
        HeightRequest = 0; HorizontalOptions = LayoutOptions.Fill;
        Loaded += (_, _) => {
            Element parent = Parent;
            while (parent != null && parent is not ContentPage) parent = parent.Parent;
            if (parent is ContentPage owner && page != owner) { Detach(); page = owner; page.Appearing += Appearing; page.Disappearing += Disappearing; }
        };
        Unloaded += (_, _) => Detach();
    }
    private void Detach()
    {
        if (page != null) { page.Appearing -= Appearing; page.Disappearing -= Disappearing; page = null; }
    }
    private void Appearing(object sender, EventArgs e)
    {
#if ANDROID
        (Handler as RecipeApp.Platforms.Android.AdBannerHandler)?.Resume();
#endif
    }
    private void Disappearing(object sender, EventArgs e)
    {
#if ANDROID
        (Handler as RecipeApp.Platforms.Android.AdBannerHandler)?.Pause();
#endif
    }
}
