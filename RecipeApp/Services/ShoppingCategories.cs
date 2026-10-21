using System.Text.RegularExpressions;
namespace RecipeApp.Services;

/// <summary>Suggest an aisle only for uncategorized new items; never rewrite saved lists.</summary>
public static class ShoppingCategories
{
    public static string Resolve(string name,string category)
    {
        var selected=ShoppingListDocument.CategoryName(category);
        if(selected!="Other")return selected;
        var text=ShoppingItemInput.Parse(name,"").Name.ToLowerInvariant();
        bool Has(string pattern)=>Regex.IsMatch(text,@"\b(?:"+pattern+@")\b",RegexOptions.CultureInvariant);
        if(Has(@"dog food|cat food|pet food|cat litter|dog treats|cat treats"))return "Pet food";
        if(Has(@"washing up liquid|dishwasher|laundry|detergent|bleach|cleaning spray|bin bags|toilet rolls?|toilet paper|kitchen rolls?|kitchen towels?|shampoo|toothpaste|soap"))return "Household";
        if(Has(@"frozen|ice cream|ice lollies"))return "Frozen";
        // Compound foods come before individual ingredient words.
        if(Has(@"stock|broth|bouillon|seasoning|gravy|coconut milk|coconut cream|peanut butter|cocoa butter|nut butter|tinned|canned|tomato puree|tomato purée|tomato paste|chopped tomatoes|baked beans|dried herbs|dried basil|dried parsley|dried coriander|dried cilantro|garlic powder|garlic granules|onion powder|chilli powder|chili powder|ground ginger|fish sauce|oyster sauce|pasta sauce|curry paste|salad cream|cream crackers"))return "Pantry";
        if(Has(@"butternut squash|fresh thyme|fresh rosemary|fresh oregano"))return "Produce";
        if(Has(@"milk|cheese|cheddar|mozzarella|parmesan|feta|halloumi|butter|margarine|cream|yoghurts?|yogurts?|eggs?|crème fraîche|creme fraiche"))return "Dairy";
        if(Has(@"beef|steaks?|mince|minced meat|chicken|turkey|pork|bacon|ham|sausages?|lamb|duck|venison|salmon|tuna|cod|haddock|fish|prawns?|shrimp|mackerel|trout|sardines")){
            if(Has(@"vegan|vegetarian|plant based|plant-based|meat free|meat-free|soya|soy|quorn"))return "Chilled alternatives";
            return "Meat";
        }
        if(Has(@"tofu|tempeh|quorn|seitan"))return "Chilled alternatives";
        if(Has(@"bread|baguettes?|bagels?|croissants?|pitta|pita|tortillas?|bread rolls?|brioche|naan|crumpets?"))return "Bakery";
        if(Has(@"juice|water|cola|lemonade|squash|beer|wine|cider|soda|soft drinks|7up"))return "Drinks";
        if(Has(@"pasta|spaghetti|penne|noodles?|rice|flour|sugar|salt|peppercorns?|black pepper|white pepper|paprika|cumin|cinnamon|turmeric|oregano|thyme|rosemary|bay leaves|bay leaf|mixed herbs|oil|vinegar|sauce|ketchup|mayonnaise|mustard|honey|jam|lentils?|chickpeas?|kidney beans|black beans|couscous|quinoa|oats|cereal|coffee|tea|biscuits?|crisps?|chocolate|nuts|raisins|baking powder|bicarbonate"))return "Pantry";
        if(Has(@"apples?|bananas?|oranges?|lemons?|limes?|pears?|grapes|strawberries|blueberries|raspberries|mango|pineapple|avocados?|tomatoes|tomato|potatoes|potato|onions?|garlic|carrots?|broccoli|cauliflower|spinach|lettuce|cabbage|kale|cucumbers?|courgettes?|zucchini|aubergines?|eggplant|mushrooms?|peppers?|chillies|chilli|chili|celery|leeks?|ginger|spring onions?|spring onion|green beans|peas|sweetcorn|corn|basil|parsley|coriander|cilantro|mint|fresh herbs"))return "Produce";
        return "Other";
    }
}
