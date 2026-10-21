namespace RecipeApp.Views;

public class LoadingIndicator : ContentView
{
    private readonly ActivityIndicator spinner=new(){WidthRequest=26,HeightRequest=26,VerticalOptions=LayoutOptions.Center};
    private readonly Label label=ShoppingUi.Text("",14);
    public LoadingIndicator(string message,bool busy=true)
    {
        spinner.SetDynamicResource(ActivityIndicator.ColorProperty,"ThemeAccent");
        label.VerticalOptions=LayoutOptions.Center;
        var row=new Grid{ColumnDefinitions=new(){new(new GridLength(36)),new(GridLength.Star)},ColumnSpacing=8,Padding=new Thickness(12,8)};
        row.Add(spinner,0);row.Add(label,1);Content=row;Message=message;IsBusy=busy;
    }
    public string Message{get=>label.Text;set{label.Text=value;SemanticProperties.SetDescription(this,value);}}
    public bool IsBusy{get=>spinner.IsRunning;set{spinner.IsRunning=value;IsVisible=value;}}
}

internal static class ActionProgress
{
    public static async Task Run(Button button,Func<Task> action)
    {
        var original=button.Text;const string waiting="⏳ Working…";
        using var cancelled=new CancellationTokenSource();
        async Task Show()
        {
            try{await Task.Delay(300,cancelled.Token);if(button.Text==original)button.Text=waiting;}
            catch(OperationCanceledException){ }
        }
        var progress=Show();
        try{await action();}
        finally{cancelled.Cancel();await progress;if(button.Text==waiting)button.Text=original;}
    }
}
