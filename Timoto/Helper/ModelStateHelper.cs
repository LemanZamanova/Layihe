using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Timoto.Helper
{
    public static class ModelStateHelper
    {
        public static void OverrideFieldValuesWithModel(this ModelStateDictionary modelState, object model)
        {
            var properties = model.GetType().GetProperties();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(model);
                if (value != null)
                {
                    modelState.SetModelValue(prop.Name, new ValueProviderResult(value.ToString()));
                }
            }
        }
    }
}
