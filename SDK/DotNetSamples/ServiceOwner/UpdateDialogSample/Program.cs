using System.Diagnostics;
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

// Insert dialog id of an existing dialog
var dialogId = Guid.CreateVersion7();

var getDialogRequest = await serviceOwnerApi.GetDialog(dialogId);

if (!getDialogRequest.IsSuccessStatusCode)
{
    Console.WriteLine("Something went wrong");
    Console.WriteLine(getDialogRequest.Error.ReasonPhrase);
    Console.WriteLine(getDialogRequest.Error.Content);
    return;
}

var dialog = getDialogRequest.Content;
if (dialog == null)
{
    throw new UnreachableException("Something went wrong");
}

var updateDialog = dialog.ToUpdateDialog();
updateDialog.Content.Title = new ContentValue
{
    Value = new List<Localization> { new() { Value = "Ny tittel!", LanguageCode = "nb" } },
    MediaType = "text/plain",
};

var updateResponse = await serviceOwnerApi.UpdateDialog(dialogId, updateDialog);

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
