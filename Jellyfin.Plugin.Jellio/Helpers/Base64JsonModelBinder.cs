using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.Jellio.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;

namespace Jellyfin.Plugin.Jellio.Helpers;

public class Base64JsonModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;
        ArgumentNullException.ThrowIfNull(value);

        var json = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(value));
        var result = JsonSerializer.Deserialize<ConfigModel>(json);
        ArgumentNullException.ThrowIfNull(result);

        bindingContext.Result = ModelBindingResult.Success(result);
        return Task.CompletedTask;
    }
}
