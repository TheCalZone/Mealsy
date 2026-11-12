using RecipeApp.Services;
namespace RecipeApp.Views;

/// <summary>Secondary controls expand in the page's own scroll flow, without a nested scroller.</summary>
public sealed class DisclosureSection:ContentView
{
    private readonly Button header;
    private readonly Label summary;
    private readonly ContentView panel;
    private readonly View details;
    private bool open,changing;
    public string Summary {get=>summary.Text;set{summary.Text=value;summary.IsVisible=!string.IsNullOrWhiteSpace(value);}}
    public DisclosureSection(string title,string subtitle,View content,bool expanded=false,Action<bool> changed=null)
    {
        details=content;open=expanded;
        header=new Button{FontAttributes=FontAttributes.Bold,FontSize=17,MinimumHeightRequest=52,HeightRequest=-1,CornerRadius=12,Padding=new Thickness(12,8),LineBreakMode=LineBreakMode.WordWrap};
        header.SetDynamicResource(Button.FontSizeProperty,"ThemeFont17");header.SetDynamicResource(Button.BackgroundColorProperty,"ThemeSubtle");header.SetDynamicResource(Button.TextColorProperty,"ThemeText");
        header.Clicked+=async(_,_)=>await Toggle();
        summary=ShoppingUi.Text(subtitle,13);summary.IsVisible=!string.IsNullOrWhiteSpace(subtitle);
        panel=new ContentView{Content=details,IsVisible=open,InputTransparent=!open,IsClippedToBounds=true,Padding=new Thickness(0,10,0,0)};
        void Caption(){header.Text=title+(open?"  ▴":"  ▾");SemanticProperties.SetDescription(header,title+(open?", expanded":", collapsed"));SemanticProperties.SetHint(header,open?"Hide these options":"Show these options");}
        void Settle(){panel.IsVisible=open;panel.HeightRequest=-1;panel.InputTransparent=!open;}
        async Task Toggle()
        {
            if(changing)return;changing=true;
            try{
                var from=open?panel.Height:0;open=!open;changed?.Invoke(open);Caption();
                var width=panel.Width>0?panel.Width:header.Width;
                if(!MotionEnabled()||width<=0){Settle();return;}
                var natural=details.Measure(width,double.PositiveInfinity,MeasureFlags.IncludeMargins).Request.Height+panel.Padding.VerticalThickness;
                if(!double.IsFinite(natural)||natural<=0){Settle();return;}
                if(!double.IsFinite(from)||from<0)from=0;
                panel.HeightRequest=from;panel.IsVisible=true;panel.InputTransparent=true;
                var done=new TaskCompletionSource<bool>();
                new Animation(value=>panel.HeightRequest=value,from,open?natural:0).Commit(panel,"Disclosure",16,180,Easing.CubicOut,(_,cancelled)=>done.TrySetResult(!cancelled));
                await done.Task;
            }catch{Settle();}finally{Settle();changing=false;}
        }
        Caption();panel.Unloaded+=(_,_)=>panel.AbortAnimation("Disclosure");
        Content=ShoppingUi.Card(new VerticalStackLayout{Spacing=0,Children={header,summary,panel}});
    }
    private static bool MotionEnabled()
    {
        try{
#if ANDROID
            return global::Android.Provider.Settings.Global.GetFloat(global::Android.App.Application.Context.ContentResolver,"animator_duration_scale",1f)>0;
#elif IOS
            return !UIKit.UIAccessibility.IsReduceMotionEnabled;
#else
            return true;
#endif
        }catch{return false;}
    }
}
