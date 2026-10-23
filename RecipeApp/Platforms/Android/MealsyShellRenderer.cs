using Android.Content;
using Android.Content.Res;
using Google.Android.Material.BottomNavigation;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
namespace RecipeApp.Platforms.Android;

public class MealsyShellRenderer:ShellRenderer
{
    protected override IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem)
        =>new MealsyTabAppearance(this,shellItem);
}
internal class MealsyTabAppearance:ShellBottomNavViewAppearanceTracker
{
    public MealsyTabAppearance(IShellContext context,ShellItem item):base(context,item){}
    public override void SetAppearance(BottomNavigationView bottomView,IShellAppearanceElement appearance)
    {
        base.SetAppearance(bottomView,appearance);
        bottomView.LabelVisibilityMode=1; // Material LABEL_VISIBILITY_LABELED: keep all destinations named.
        bottomView.ItemHorizontalTranslationEnabled=false;
        bottomView.ItemTextAppearanceActive=Resource.Style.MealsyTabTextActive;
        bottomView.ItemTextAppearanceInactive=Resource.Style.MealsyTabTextInactive;
        var colors=new ColorStateList(new[]{new[]{global::Android.Resource.Attribute.StateChecked},Array.Empty<int>()},
            new[]{global::Android.Graphics.Color.White.ToArgb(),global::Android.Graphics.Color.ParseColor("#B4C6BA").ToArgb()});
        bottomView.ItemIconTintList=colors;bottomView.ItemTextColor=colors;
        bottomView.ItemActiveIndicatorEnabled=true;
        bottomView.ItemActiveIndicatorColor=ColorStateList.ValueOf(global::Android.Graphics.Color.Argb(64,255,255,255));
        var density=bottomView.Resources.DisplayMetrics.Density;
        bottomView.ItemActiveIndicatorShapeAppearance=new Google.Android.Material.Shape.ShapeAppearanceModel.Builder().SetAllCornerSizes(16*density).Build();
        bottomView.ItemActiveIndicatorWidth=(int)(60*density);
        bottomView.ItemActiveIndicatorHeight=(int)(32*density);
    }
}
