using Altinn.ApiClients.Dialogporten;
using Microsoft.Extensions.Hosting;
using Altinn.ApiClients.Dialogporten.EndUser;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

var endUserApi = app.Services.GetRequiredService<IEndUserApi>().V1;


var result = await endUserApi.SearchDialogs(
    new() { Party = ["urn:altinn:person:identifier-no:08844397713"], }, null!);

if (!result.IsSuccessful)
{
    Console.WriteLine("Search not successful");
    return -1;
}

foreach (var dialog in result.Content.Items)
{
    Console.WriteLine(dialog.Id);
}

Console.WriteLine("Done");

return 0;
