using RecipeApp.Services;
namespace RecipeApp.Views;

public record ShoppingListTool(string Title,Func<Task> Action,bool Enabled=true);
public record ShoppingListToolSection(string Title,string Detail,List<ShoppingListTool> Tools);

/// <summary>Secondary shopping actions, grouped outside the list's limited viewport.</summary>
public class ShoppingListToolsPage:ContentPage
{
    public ShoppingListToolsPage(string name,string scope,IEnumerable<ShoppingListToolSection> sections)
    {
        Title="List tools";ShoppingUi.Theme(this);
        var body=new VerticalStackLayout{Padding=16,Spacing=16};
        body.Add(ShoppingUi.Text(name,25,true));body.Add(ShoppingUi.Text(scope,13));
        var first=true;
        foreach(var section in sections)
        {
            var group=new VerticalStackLayout{Spacing=8};

            foreach(var tool in section.Tools){var button=ShoppingUi.Button(tool.Title,tool.Action,true);button.IsEnabled=tool.Enabled;group.Add(button);}
            body.Add(new DisclosureSection(section.Title,section.Detail,group,expanded:first));first=false;
        }
        var premium=new MembershipPrompt();premium.Refresh();body.Add(premium);
        Content=new ScrollView{Content=body};
    }
}
