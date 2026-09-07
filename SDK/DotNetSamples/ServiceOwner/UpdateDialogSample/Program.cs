using Altinn.ApiClients.Dialogporten;
using Altinn.ApiClients.Dialogporten.ServiceOwner;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>(optional: true);

var dialogportenSettings = builder.Configuration
        .GetSection("DialogportenSettings")
        .Get<DialogportenSettings>()
 ?? throw new InvalidOperationException(
        "Configuration section 'DialogportenSettings' is missing or invalid. " +
        "Ensure appsettings.json is present in the output directory and contains a valid DialogportenSettings section.");


builder.Services.AddDialogportenClient(dialogportenSettings);

using var app = builder.Build();

var serviceOwnerApi = app.Services.GetRequiredService<IServiceOwnerApi>().V1;

var dialogId = Guid.CreateVersion7();

var dialogToUpdate = await serviceOwnerApi.GetDialog(dialogId);

if (!dialogToUpdate.IsSuccessStatusCode)
{
    Console.WriteLine("Something went wrong");
}
else
{
    var dia = dialogToUpdate.Content;
    if (dia == null)
    {
        throw new Exception("Something went wrong");
    }
    var updateDia = dia.ToUpdateDialog();
    updateDia.Content.Title = new ContentValue
    {
        Value = new List<Localization> { new() { Value = "Ny tittel!", LanguageCode = "nb" } },
        MediaType = "text/plain",
    };

    var updateResponse = await serviceOwnerApi.UpdateDialog(dialogId, updateDia);

    if (updateResponse.IsSuccessStatusCode)
    {
        Console.WriteLine("Dialog updated successfully");
    }
    else
    {
        Console.WriteLine("Something went wrong");
        Console.WriteLine(updateResponse.Error.ReasonPhrase);
        Console.WriteLine(updateResponse.Error.Content);
    }
}