using Altinn.ApiClients.Dialogporten;
using Altinn.ApiClients.Dialogporten.ServiceOwner;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Create;
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
var minimalDialog = new CreateDialog
{
    ServiceResource = "urn:altinn:resource:super-simple-service",
    Party = "urn:altinn:person:identifier-no:08895699684",
    Content = new CreateDialogContent
    {
        Title = new ContentValue
        {
            MediaType = "text/plain",
            Value =
            [
                new Localization
                {
                    Value = "Dette er en tittel",
                    LanguageCode = "nb"
                },
                new Localization
                {
                    Value = "This is a title",
                    LanguageCode = "en"
                }
            ]
        }
    }
};

var bigDialog = new CreateDialog
{
    ServiceResource = null,
    Party = null,
    Content =
        new CreateDialogContent
        {
            Title = new ContentValue
            {
                Value =
                [
                    new Localization
                    {
                        Value = "Titel!",
                        LanguageCode = "nb"
                    }
                ],
                MediaType = "text/plain",
            }
        }
};

var ct = CancellationToken.None;
var createDialog = await serviceOwnerApi.CreateDialog(minimalDialog, ct);
if (createDialog.IsSuccessStatusCode)
{
    Console.WriteLine("Weee");
    Console.WriteLine(createDialog.Content);
}
else
{
    Console.WriteLine("Booo");
    Console.WriteLine(createDialog.ReasonPhrase);
    Console.WriteLine(createDialog.Error.Content);
}
