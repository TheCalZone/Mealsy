using Android.App;
using Android.Runtime;

namespace RecipeApp;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

    public override void OnConfigurationChanged(global::Android.Content.Res.Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        Platforms.Android.ShoppingQuickAddWidget.UpdateAll(this);
    }

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
