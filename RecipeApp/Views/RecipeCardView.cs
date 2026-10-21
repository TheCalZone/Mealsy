using RecipeApp.Models;
using RecipeApp.Services;
namespace RecipeApp.Views;
internal static class RecipeCardView
{
    public static Image Photo(string encoded,double height=170)
    {
        if(string.IsNullOrWhiteSpace(encoded)||encoded.Length>12_000_000)return null;
        try{var bytes=Convert.FromBase64String(encoded);var image=new Image{Source=ImageSource.FromStream(()=>new MemoryStream(bytes)),HeightRequest=height,Aspect=Aspect.AspectFill};image.SetDynamicResource(VisualElement.IsVisibleProperty,"ThemeShowPhotos");return image;}catch(FormatException){return null;}
    }
    public static View Card(Recipe recipe,Func<Task> open,Func<Task> options=null)
    {
        var body=new RecipePressLayout{Spacing=9};
        var busy=false;
        async Task Run(Func<Task> action)
        {
            if(busy)return;busy=true;
            try{await action();}finally{busy=false;}
        }
        var tap=new TapGestureRecognizer();tap.Tapped+=async(_,_)=>await Run(open);body.GestureRecognizers.Add(tap);
        if(options!=null)body.Held+=async()=>await Run(options);
        var photo=Photo(recipe.RecipeImage);if(photo!=null)body.Add(photo);
        body.Add(ShoppingUi.Text(recipe.RecipeName,22,true));if(!string.IsNullOrWhiteSpace(recipe.Description))body.Add(ShoppingUi.Text(recipe.Description,14));
        body.Add(ShoppingUi.Text(RecipePresentation.TimeText(recipe),12,true));
        var tags=RecipeBrowser.Tags(recipe);if(tags.Length>0)body.Add(ShoppingUi.Text(string.Join(" · ",tags.Take(4).Select(t=>t.Equals("favourites",StringComparison.OrdinalIgnoreCase)?"★ Favourite":t)),12));
        var actions=new VerticalStackLayout{Spacing=8};
        actions.Add(ShoppingUi.Button("View & cook",()=>Run(open)));
        if(options!=null)actions.Add(ShoppingUi.Button("⋯ Options",()=>Run(options),true));
        body.Add(actions);return ShoppingUi.Card(body);
    }
}
