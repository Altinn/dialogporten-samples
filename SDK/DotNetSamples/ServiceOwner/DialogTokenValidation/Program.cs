using Altinn.ApiClients.Dialogporten;
using Altinn.ApiClients.Dialogporten.ServiceOwner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>(optional: true);
var dialogportenSettings = builder.Configuration
        .GetSection("DialogportenSettings")
        .Get<DialogportenSettings>()
 ?? throw new InvalidOperationException(
        "Configuration section 'DialogportenSettings' is missing or invalid. " +
        "Ensure appsettings.json is present in the output directory and contains a valid DialogportenSettings section.");
builder.Services.AddDialogportenClient(dialogportenSettings);

var app = builder.Build();

var dialogTokenValidator = app.Services.GetRequiredService<IDialogTokenValidator>();
app.UseHttpsRedirection();
app.MapPost("validateToken", ([FromBody] string token) =>
{
    var dialogId = Guid.CreateVersion7(); // Use valid dialogId
    // Checks if the Lifetime of the token is valid and if it's representing the dialogId 
    var result = dialogTokenValidator.Validate(token, dialogId);
    if (!result.IsValid)
    {
        Console.WriteLine(result.Errors);
        return false;
    }

    var claims = result.ClaimsPrincipal;
    // https://docs.altinn.studio/nb/dialogporten/reference/authorization/dialog-tokens/#liste-over-dialogporten-spesifikke-claims
    // Check Security level of authentication
    var claim = claims.FindFirst("l");
    if (claim is null)
    {
       throw new Exception("Something went wrong");
    }

    var authLevel = claim.Value;
    Console.WriteLine(authLevel);
    if (authLevel != "4")
    {
        Console.WriteLine("Auth level too low");
        return false;
    }

    return true;
});
app.Run();
