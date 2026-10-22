using Altinn.ApiClients.Dialogporten;
using Altinn.ApiClients.Dialogporten.ServiceOwner;
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

// Id to an existing dialog
var dialogId = Guid.Parse("01a16326-2071-75bf-b936-f55886035b4e");

var response = await serviceOwnerApi.DeleteDialog(dialogId);

if (!response.IsSuccessStatusCode)
{
    Console.WriteLine($"Failed to delete dialog with ID {dialogId}. Status code: {response.StatusCode}");
    Console.WriteLine(response.Error.Message);
    return;
}
Console.WriteLine(response.IsSuccessStatusCode);
