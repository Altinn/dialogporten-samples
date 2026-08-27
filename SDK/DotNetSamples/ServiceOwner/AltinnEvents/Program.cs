using Altinn.ApiClients.Dialogporten;
using Altinn.ApiClients.Dialogporten.ServiceOwner;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Create;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;
using AltinnEvents;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>(optional: true);
var dialogportenSettings = builder.Configuration
    .GetSection("DialogportenSettings")
    .Get<DialogportenSettings>()!;
builder.Services.AddDialogportenClient(dialogportenSettings);

builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi();
app.UseHttpsRedirection();

app.MapPost("/event", async (
        [FromServices] IServiceOwnerApi serviceOwnerApi,
        [FromServices] ILogger<Program> logger,
        [FromBody] CloudEvent cloudEvent,
        CancellationToken cancellationToken)
    =>
{
#if DEBUG
    foreach (var propertyInfo in cloudEvent.GetType().GetProperties())
    {
        Console.WriteLine($"{propertyInfo.Name}: {propertyInfo.GetValue(cloudEvent)}");
    }
#endif //DEBUG
    switch (cloudEvent.Type)
    {
        case "dialogporten.dialog.created.v1":
            logger.LogInformation("Dialog created event");
            await CreatedHandler(serviceOwnerApi, cloudEvent, cancellationToken);
            break;
        case "dialogporten.dialog.updated.v1":
            logger.LogInformation("Dialog updated event");
            await UpdateHandler(cloudEvent, serviceOwnerApi, cancellationToken);
            break;
        case "platform.events.validatesubscription":
            // For validation of subscription sent by altinn events when creating a subscription
            // No Op. 
            logger.LogInformation("Validating subscription");
            break;
        default:
            logger.LogWarning($"Unsupported event type: {cloudEvent.Type}");
            break;
    }
    return Results.Ok();
});

app.Run();
return;


async Task CreatedHandler(IServiceOwnerApi serviceOwnerApi, CloudEvent cloudEvent, CancellationToken cancellationToken)
{
    if (!Guid.TryParse(cloudEvent.ResourceInstance, out var id))
    {
        throw new InvalidCastException(cloudEvent.ResourceInstance);
    }
    var getDialogResponse = await serviceOwnerApi.V1.GetDialog(id, null!, cancellationToken);
    Console.WriteLine($"StatusCode: {getDialogResponse.StatusCode}");
    var dialog = getDialogResponse.Content;
    if (!getDialogResponse.IsSuccessStatusCode || dialog is null)
    {
        Console.WriteLine($"Something went wrong: {getDialogResponse.ReasonPhrase}");
        return;
    }
    var req = new CreateDialogActivityRequest
    {
        Type = DialogActivityType.Information,
        Description =
        [
            new()
            {
                Value = "Dialog er laget",
                LanguageCode = "nb"
            }
        ],
        PerformedBy = new Actor() // Trenger vi at denne er required?
    };
    var createActivityResponse = await serviceOwnerApi.V1.CreateDialogActivity(id, req, dialog.Revision, cancellationToken);
    Console.WriteLine($" Create activity statusCode: {createActivityResponse.StatusCode}");
}

async Task UpdateHandler(CloudEvent cloudEvent, IServiceOwnerApi serviceOwnerApi, CancellationToken cancellationToken)
{

    if (!Guid.TryParse(cloudEvent.ResourceInstance, out var id))
    {
        throw new InvalidCastException(cloudEvent.ResourceInstance);
    }
    var getDialogResponse = await serviceOwnerApi.V1.GetDialog(id, null!, cancellationToken);
    Console.WriteLine($"StatusCode: {getDialogResponse.StatusCode}");
    var dialog = getDialogResponse.Content;
    if (!getDialogResponse.IsSuccessStatusCode || dialog is null)
    {
        Console.WriteLine($"Something went wrong: {getDialogResponse.ReasonPhrase}");
    }
}
