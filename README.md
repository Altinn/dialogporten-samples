# dialogporten-samples

Samples showing how to use the Dialogporten SDK to work with [Dialogporten](https://docs.altinn.studio/en/dialogporten/)
in Altinn.

Each sample is a small, self-contained .NET project that covers one task: creating a dialog, searching dialogs as an end
user, validating a dialog token, and so on. Use them as a starting point for your own integration.

## SDK packages

| Package                                       | Use it for                                                                                    |
|-----------------------------------------------|-----------------------------------------------------------------------------------------------|
| `Altinn.ApiClients.Dialogporten.ServiceOwner` | Service owners that create, update and delete dialogs, and validate dialog tokens             |
| `Altinn.ApiClients.Dialogporten.EndUser`      | End user systems (e.g. ERP systems acting through a system user) that read and act on dialogs |

Both packages register the client with `services.AddDialogportenClient(settings)` and authenticate
with [Maskinporten](https://docs.digdir.no/docs/Maskinporten/).

## Samples

All samples are in `SDK/DotNetSamples` and grouped in the solution `DotNetSamples.slnx`.

### Service owner

| Sample                | Description                                                                                                                                                                                                                                                    |
|-----------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| CreateDialog          | Creates two dialogs: a minimal one with only the required fields, and a complex one that uses most of the create API (transmissions, attachments, GUI/API actions, activities, multilingual content). See `CreateDialogDtoSamples.cs` for the field reference. |
| UpdateDialog          | Fetches an existing dialog, maps it to an update request with `ToUpdateDialog()`, changes the title and saves it.                                                                                                                                              |
| DeleteDialog          | Deletes a dialog by ID.                                                                                                                                                                                                                                        |
| DialogTokenValidation | Minimal API with a `POST /validateToken` endpoint that validates a dialog token using `IDialogTokenValidator` (signature, lifetime and dialog ID), then checks the authentication level claim.                                                                 |
| AltinnEvents          | Minimal API with a `POST /event` webhook that receives Dialogporten events from Altinn Events (CloudEvents). It handles subscription validation and, on `dialog.created`, adds an activity to the dialog.                                                      |

### End user

| Sample            | Description                                                                                                                                                                    |
|-------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| SearchDialog      | Searches dialogs for one or more parties, pages through the results with a continuation token, and lists the transmissions in each dialog.                                     |
| GetCorrespondence | Fetches a dialog, goes through its API actions, and calls the endpoints with the dialog token. Download endpoints are saved as zip files, other endpoints are printed as JSON. |
| SetSystemLabel    | Toggles a dialog's system label between `Default` and `Archive`.                                                                                                               |

The end user samples act as a [system user](https://docs.altinn.studio/en/authorization/what-do-you-get/systemuser/)
through `MaskinportenRequestContext`.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A Maskinporten client with access to the Dialogporten scopes you need, and its JWK
- For the end user samples: a system user that has been granted access to the parties and services involved
- Access to the test environment (TT02) unless you change `BaseUri`

## Configuration

Every sample reads a `DialogportenSettings` section from `appsettings.json`:

```json
"DialogportenSettings": {
  "BaseUri": "https://platform.tt02.altinn.no/dialogporten/",
  "ThrowOnPublicKeyFetchInit": false,
  "Maskinporten": {
    "Environment": "test",
    "ClientId": "some-client-id",
    "Scope": "altinn:dialogporten",
    "EncodedJwk": "ewogICAic...",
    "Kid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
  }
}
```

You can fill in your values directly in `appsettings.json`, but never commit or push real credentials.

Alternatively, keep secrets out of the repository entirely
with [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets). User secrets override
`appsettings.json`, and each project has its own `UserSecretsId`, so set them per project:

```sh
cd SDK/DotNetSamples/ServiceOwner/CreateDialog
dotnet user-secrets set "DialogportenSettings:Maskinporten:ClientId" "<your-client-id>"
dotnet user-secrets set "DialogportenSettings:Maskinporten:Scope" "<your-scopes>"
dotnet user-secrets set "DialogportenSettings:Maskinporten:EncodedJwk" "<base64-encoded-jwk>"
dotnet user-secrets set "DialogportenSettings:Maskinporten:Kid" "<key-id>"
```

## Running a sample

```sh
cd SDK/DotNetSamples/ServiceOwner/CreateDialog
dotnet run
```

Or open `SDK/DotNetSamples/DotNetSamples.slnx` in Rider or Visual Studio and run the project from there.

The two web samples (DialogTokenValidation and AltinnEvents) listen on `http://localhost:5000` by default. Each includes
an `.http` file with example requests:

- `DialogTokenValidation/ValidateExample.http`: replace `@token` with a dialog token and `@dialogId` with the matching
  dialog ID.
- `AltinnEvents/EventRequest.http`: sends a subscription validation event and example `dialog.created` /
  `dialog.updated` events.

## Further reading

- [Dialogporten documentation](https://docs.altinn.studio/en/dialogporten/)
- [Dialog tokens](https://docs.altinn.studio/en/dialogporten/reference/authorization/dialog-tokens/)
- [Dialogporten on GitHub](https://github.com/Altinn/dialogporten)

## License

[MIT](LICENSE)
