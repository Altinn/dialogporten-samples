using Altinn.ApiClients.Dialogporten;
using Microsoft.Extensions.Hosting;
using Altinn.ApiClients.Dialogporten.EndUser;
using Altinn.ApiClients.Maskinporten.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

internal class Program
{
    public static async Task<int> Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Configuration.AddUserSecrets<Program>(optional: true);
        var dialogportenSettings = builder.Configuration
                                       .GetSection("DialogportenSettings")
                                       .Get<DialogportenSettings>()
                                   ?? throw new InvalidOperationException(
                                       "Configuration section 'DialogportenSettings' is missing or invalid. " +
                                       "Ensure appsettings.json is present in the output directory and contains a valid DialogportenSettings section.");


        builder.Services.AddDialogportenClient(dialogportenSettings);

        builder.Services.AddHttpClient();

        using var app = builder.Build();

        var endUserApi = app.Services.GetRequiredService<IEndUserApi>().V1;
        var httpClient = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();


/*
 * SystemUser:OrganizationNumber: Must be set to the organization number of the company that owns the system user
 * DialogId: Must be set to the ID of the dialog you want to update
 *
 * Example:
 * string? systemUserOrgNumber = "314251571";
 * Guid dialogId = Guid.Parse("019dd9fa-08b2-7cf2-ae89-a7292758588b");
 */
        string? systemUserOrgNumber = null;
        var dialogId = Guid.Empty;

        if (systemUserOrgNumber == null)
            throw new ArgumentException("SystemUserOrgNumber must be set to the organization number of the company that owns the system user.");

        if (dialogId == Guid.Empty)
            throw new ArgumentException("DialogId must be set to the ID of the dialog you want to change label.");

        var maskinPortenRequest = new MaskinportenRequestContext()
            { SystemUser = new SystemUser { OrganizationNumber = systemUserOrgNumber } };

        var dialog = await endUserApi.GetDialog(dialogId, requestContext:maskinPortenRequest);
        if (!dialog.IsSuccessful)
        {
            await Console.Error.WriteLineAsync($"Failed to get dialog {dialogId}: {dialog.StatusCode}");
            return 1;
        }

        // Print out all the apiActions in the dialog
        if (dialog.Content.ApiActions.Count == 0)
        {
            await Console.Error.WriteLineAsync("No API actions defined");
            return 1;
        }

        foreach (var action in dialog.Content.ApiActions)
        {
            Console.WriteLine($"\nAction ID: {action.Id}");
            Console.WriteLine($"Action name: {(string.IsNullOrEmpty(action.Name) ? "No name" : action.Name)}");
            Console.WriteLine($"Action type: {action.Action}");

            foreach (var endpoint in action.Endpoints)
            {
                Console.WriteLine($"\tEndpoint ID: {endpoint.Id}");
                Console.WriteLine($"\thttpMethod: {endpoint.HttpMethod}");
                Console.WriteLine($"\tURL: {endpoint.Url}\n");

                // Not all actions have name. Thus check on HttpMethod and URL
                if (!endpoint.HttpMethod.ToString().Equals("GET", StringComparison.OrdinalIgnoreCase)) continue;

                // For correspondence messages we assume that the download URL contains "download" and is of type zip
                if (endpoint.Url.ToString().Contains("download"))
                {
                    var downloadResponse = await GetWithToken(endpoint.Url);
                    if (!downloadResponse.IsSuccessStatusCode)
                    {
                        await Console.Error.WriteLineAsync($"Failed to download file from {endpoint.Url}: {downloadResponse.StatusCode}");
                        return 1;
                    }

                    var fileName = $"download_{endpoint.Id}.zip";
                    await using var fileStream = File.Create(fileName);
                    await downloadResponse.Content.CopyToAsync(fileStream);
                    Console.WriteLine($"\tSaved to: {Path.GetFullPath(fileName)}");
                }
                else
                {
                    var response = await GetWithToken(endpoint.Url);
                    if (!response.IsSuccessStatusCode)
                    {
                        await Console.Error.WriteLineAsync($"Failed to get json from {endpoint.Url}: {response.StatusCode}");
                        return 1;
                    }
                    var json = await response.Content.ReadAsStringAsync();
                    var prettyJson = System.Text.Json.JsonSerializer.Serialize(
                        System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json),
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine($"\tFCE content:\n{prettyJson}");
                }
            }
        }

        return 0;

        async Task<HttpResponseMessage> GetWithToken(Uri url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var token = dialog.Content.DialogToken;
            if (token != null)
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return await httpClient.SendAsync(request);
        }
    }
}
