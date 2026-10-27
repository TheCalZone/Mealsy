namespace RecipeApp.Services;

public static class HouseholdMemberColors
{
    public static readonly string[] Names={"Blue","Berry","Olive","Amber","Violet","Teal","Rose","Slate"};
    public static readonly string[] Values={"#165D91","#7A325F","#426320","#915015","#51449A","#176865","#8B3544","#4F5965"};
    public static string Resolve(string member,string selected=null)
    {
        if(Values.Contains(selected))return selected;
        uint hash=2166136261;foreach(var ch in member??"")hash=unchecked((hash^ch)*16777619);
        return Values[hash%(uint)Values.Length];
    }
}
