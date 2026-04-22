using Altinn.ApiClients.Dialogporten;
using Microsoft.Extensions.Hosting;
using Altinn.ApiClients.Dialogporten.EndUser;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1;
using Altinn.ApiClients.Maskinporten.Models;
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


/*
 * SystemUser:OrganizationNumber: Must be set to the organization number of the company that owns the system user
 * DialogId: Must be set to the ID of the dialog you want to update
 *
 * Example:
 * string? systemUserOrgNumber = "314251571";
 * Guid dialogId = Guid.Parse("0199f1a3-90ba-7786-92b9-4d8006883d69");
 */
string? systemUserOrgNumber = null;
Guid dialogId = Guid.Empty;

if (systemUserOrgNumber == null)
    throw new ArgumentException("SystemUserOrgNumber must be set to the organization number of the company that owns the system user.");

if (dialogId == Guid.Empty)
    throw new ArgumentException("DialogId must be set to the ID of the dialog you want to change label.");

var maskinPortenRequest = new MaskinportenRequestContext()
    { SystemUser = new SystemUser { OrganizationNumber = systemUserOrgNumber } };

var dialog = await endUserApi.GetDialog(dialogId, requestContext: maskinPortenRequest);
if (!dialog.IsSuccessful)
{
    Console.Error.WriteLine($"Failed to get dialog {dialogId}: {dialog.StatusCode}");
    return 1;
}

var systemLabels = dialog.Content.EndUserContext.SystemLabels ?? new List<SystemLabel>();

Console.Write(
    $"DialogId: {dialog.Content.Id}\n" +
    $"System Labels: {string.Join(", ", systemLabels)}\n");

// Toggle label between Archive and default
var newSystemLabels = new List<SystemLabel>
{
    systemLabels.FirstOrDefault() == SystemLabel.Default
        ? SystemLabel.Archive
        : SystemLabel.Default
};

var results = await endUserApi.SetDialogSystemLabels(dialogId, new SetDialogSystemLabelRequest { AddLabels = newSystemLabels}, requestContext: maskinPortenRequest);
if (!results.IsSuccessful)
{
    Console.Error.WriteLine($"Failed to set system label on dialog {dialogId}: {results.StatusCode}");
    return 1;
}

Console.WriteLine($"System Label set to: {newSystemLabels.First()}\n");

return 0;
