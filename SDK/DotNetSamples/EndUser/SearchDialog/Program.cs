using System.Runtime.CompilerServices;
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

async IAsyncEnumerable<DialogListItem> SearchAllDialogs(List<string> parties, AcceptedLanguages acceptLanguage, [EnumeratorCancellation] CancellationToken ct, MaskinportenRequestContext context)
{
    string? continuationToken = null;
    bool hasNextPage;

    var queryParams = new SearchDialogsQueryParams
    {
        Party = parties,
        // Set low limit to force use of continuationToken
        Limit = 4
    };

    do
    {
        ct.ThrowIfCancellationRequested();
        if (continuationToken != null)
            queryParams.ContinuationToken = continuationToken;

        var result = await endUserApi.SearchDialogs(
            queryParams,
            acceptLanguage,
            context);

        if (!result.IsSuccessful || result.Content?.Items == null)
        {
            throw new InvalidOperationException("Failed to search dialogs");
        }

        foreach (var item in result.Content.Items)
            yield return item;

        continuationToken = result.Content.ContinuationToken;
        hasNextPage = result.Content.HasNextPage;
    } while (hasNextPage);
}

/*
 * Party: Must be set to a list of companies or persons that you want to see dialogs from.
 * SystemUser:OrganizationNumber: Must be set to the organization number of the company that owns the system user
 *
 * Example:
 * List<string> party = ["urn:altinn:organization:identifier-no:212485772", "urn:altinn:organization:identifier-no:315236746"];
 * string? systemUserOrgNumber = "314251571";
 */
List<string> party = [];
string? systemUserOrgNumber = null;

if (party.Count == 0)
    throw new ArgumentException("Parties must be set to the company or person that you want to see dialogs from.");

if (systemUserOrgNumber == null)
    throw new ArgumentException("SystemUserOrgNumber must be set to the organization number of the company that owns the system user.");


var maskinPortenRequest = new MaskinportenRequestContext()
    { SystemUser = new SystemUser { OrganizationNumber = systemUserOrgNumber } };

var ct = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await foreach (var item in SearchAllDialogs(
                   party,
                   null!,
                   ct.Token,
                   maskinPortenRequest
               ))
{
    Console.WriteLine(
        $"ID: {item.Id}\n" +
        $"Title: {item.Content?.Title.Value?.FirstOrDefault()?.Value ?? "(no title)"}\n" +
        $"Status: {item.Status}\n" +
        $"Party: {item.Party}\n" +
        $"Nr of transmissions (Party/ServiceOwner): {item.FromPartyTransmissionsCount} / {item.FromServiceOwnerTransmissionsCount}\n");

    var dialog = await endUserApi.GetDialog(item.Id, null!, maskinPortenRequest);
    if (dialog.IsSuccessful)
    {
        if (dialog.Content.Transmissions != null)
            foreach (var transmission in dialog.Content.Transmissions)
            {
                Console.WriteLine(
                    $"ID: {transmission.Id}\n" +
                    $"Title: {transmission.Content.Title.Value?.FirstOrDefault()?.Value ?? "(no title)"}\n");
            }
    }
    else
    {
        Console.Error.WriteLine($"Failed to get dialog {item.Id}");
    }
}


return 0;
