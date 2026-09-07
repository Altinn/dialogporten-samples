using Altinn.ApiClients.Dialogporten;
using Altinn.ApiClients.Dialogporten.ServiceOwner;
using CreateDialogSample;
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

var ct = CancellationToken.None;
// var createDialog = CreateDialogDtoSamples.MinimalDialogDto();
var createDialog = CreateDialogDtoSamples.ComplexDialogDto();

var createDialogResponse = await serviceOwnerApi.CreateDialog(createDialog, ct);
if (createDialogResponse.IsSuccessStatusCode)
{
    Console.WriteLine("Dialog successfully created.");
    Console.WriteLine(createDialogResponse.Content);
}
else
{
    Console.WriteLine("Something went wrong.");
    Console.WriteLine(createDialogResponse.ReasonPhrase);
    Console.WriteLine(createDialogResponse.Error.Content);
}
