using RecipeApp.Helpers;

namespace RecipeApp.Controls
{
    internal class AccountTabDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate LoginPageTemplate { get; set; }
        public DataTemplate AccountPageTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            return AuthHelper.Client.CurrentUser != null ? AccountPageTemplate : LoginPageTemplate;
        }

    }
}
