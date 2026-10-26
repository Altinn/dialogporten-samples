using System.Diagnostics;
using Altinn.ApiClients.Dialogporten;
using Altinn.ApiClients.Dialogporten.ServiceOwner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

app.MapPost("validateToken", (HttpRequest request, [FromBody] string body) =>
{
    var dialogId = Guid.Parse(body);
    // Read the authorization header to get the dialog token
    var token = request.Headers.Authorization.ToString().Replace("Bearer ", "");

    // Validate handles checking of signature, lifetime(nbf/epf) and optionally dialogId
    // for more information on what to validate see:
    // https://docs.altinn.studio/en/dialogporten/reference/authorization/dialog-tokens/#token-validation-recommendations    
    // Checks if the Lifetime of the token is valid and if it's representing the dialogId 
    var result = dialogTokenValidator.Validate(token, dialogId);
    if (!result.IsValid)
    {
        Console.WriteLine(result.Errors);
        return false;
    }

    var claims = result.ClaimsPrincipal;
    // https://docs.altinn.studio/en/dialogporten/reference/authorization/dialog-tokens/#list-of-dialogporten-specific-claims
    // Check Security level of authentication
    var claim = claims.FindFirst("l");
    if (claim is null)
    {
        // authLevel claim "l" is required and should be present in the token
        throw new UnreachableException("Could not find required claim");
    }

    var authLevel = claim.Value;
    Console.WriteLine(authLevel);
    // for information about auth level see: https://docs.altinn.studio/en/dialogporten/user-guides/getting-dialog-details/#authentication-level
    if (authLevel != "4")
    {
        Console.WriteLine("Auth level too low");
        return false;
    }

    return true;
});
app.Run();
