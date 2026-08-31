using Altinn.ApiClients.Dialogporten;
using Altinn.ApiClients.Dialogporten.ServiceOwner;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Create;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;
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

// A complex dialog: a building permit case with transmissions, attachments,
// actions and an activity history. Ids are self-defined UUIDv7s so that
// transmissions can reference each other within the same request.
var now = DateTimeOffset.UtcNow;
var requestTransmissionId = Guid.CreateVersion7(now.AddDays(-5));
var submissionTransmissionId = Guid.CreateVersion7(now.AddDays(-2));

var bigDialog = new CreateDialog
{
    Id = Guid.CreateVersion7(now.AddDays(-14)),
    IdempotentKey = "byggesak-2026-0042",
    ServiceResource = "urn:altinn:resource:super-simple-service",
    Party = "urn:altinn:person:identifier-no:08895699684",
    Status = DialogStatusInput.RequiresAttention,
    SystemLabel = SystemLabel.Default,
    Progress = 60,
    ExtendedStatus = "AWAITING_INSPECTION",
    ExternalReference = "SAK-2026-0042",
    Process = "urn:altinn:process:byggesak",
    PrecedingProcess = "urn:altinn:process:forhaandskonferanse",
    IsApiOnly = false,
    CreatedAt = now.AddDays(-14),
    UpdatedAt = now.AddDays(-2),
    DueAt = now.AddDays(14),
    ExpiresAt = now.AddYears(1),

    // Metadata only visible to the service owner, useful for internal routing.
    ServiceOwnerContext = new CreateDialogServiceOwnerContext
    {
        ServiceOwnerLabels =
        [
            new CreateDialogServiceOwnerLabel { Value = "byggesak" },
            new CreateDialogServiceOwnerLabel { Value = "prioritet-høy" }
        ]
    },

    Content = new CreateDialogContent
    {
        Title = new ContentValue
        {
            MediaType = "text/plain",
            Value =
            [
                new Localization { LanguageCode = "nb", Value = "Byggesøknad for Storgata 1" },
                new Localization { LanguageCode = "en", Value = "Building permit application for Storgata 1" }
            ]
        },
        // Shown instead of the title in list views when the user does not meet
        // the required authentication level.
        NonSensitiveTitle = new ContentValue
        {
            MediaType = "text/plain",
            Value =
            [
                new Localization { LanguageCode = "nb", Value = "Byggesøknad" },
                new Localization { LanguageCode = "en", Value = "Building permit application" }
            ]
        },
        Summary = new ContentValue
        {
            MediaType = "text/plain",
            Value =
            [
                new Localization { LanguageCode = "nb", Value = "Søknaden er under behandling. Vi mangler oppdaterte situasjonsplaner fra deg." },
                new Localization { LanguageCode = "en", Value = "The application is being processed. We are missing updated site plans from you." }
            ]
        },
        NonSensitiveSummary = new ContentValue
        {
            MediaType = "text/plain",
            Value =
            [
                new Localization { LanguageCode = "nb", Value = "Saken din er under behandling." },
                new Localization { LanguageCode = "en", Value = "Your case is being processed." }
            ]
        },
        SenderName = new ContentValue
        {
            MediaType = "text/plain",
            Value =
            [
                new Localization { LanguageCode = "nb", Value = "Plan- og bygningsetaten" },
                new Localization { LanguageCode = "en", Value = "Planning and Building Services" }
            ]
        },
        AdditionalInfo = new ContentValue
        {
            MediaType = "text/markdown",
            Value =
            [
                new Localization
                {
                    LanguageCode = "nb",
                    Value = "## Hva skjer nå?\n\n1. Vi går gjennom dokumentasjonen\n2. Naboer varsles\n3. Vedtak fattes innen 12 uker"
                },
                new Localization
                {
                    LanguageCode = "en",
                    Value = "## What happens next?\n\n1. We review the documentation\n2. Neighbours are notified\n3. A decision is made within 12 weeks"
                }
            ]
        },
        // Human readable label for the "ExtendedStatus" field above.
        ExtendedStatus = new ContentValue
        {
            MediaType = "text/plain",
            Value =
            [
                new Localization { LanguageCode = "nb", Value = "Venter på befaring" },
                new Localization { LanguageCode = "en", Value = "Awaiting inspection" }
            ]
        },
        // Front channel embed: the value is an HTTPS url that the frontend
        // fetches and renders inline in the dialog details view.
        MainContentReference = new ContentValue
        {
            MediaType = "application/vnd.dialogporten.frontchannelembed+json;type=markdown",
            Value =
            [
                new Localization { LanguageCode = "nb", Value = "https://example.com/byggesak/2026-0042/embed?lang=nb" },
                new Localization { LanguageCode = "en", Value = "https://example.com/byggesak/2026-0042/embed?lang=en" }
            ]
        }
    },

    // Only used for search in the service owner API, never exposed to end users.
    SearchTags =
    [
        new CreateDialogTag { Value = "byggesak" },
        new CreateDialogTag { Value = "storgata-1" },
        new CreateDialogTag { Value = "2026-0042" }
    ],

    // Dialog level attachments, ie. documents describing the case as a whole.
    Attachments =
    [
        new CreateDialogAttachment
        {
            Name = "soknadsskjema",
            DisplayName =
            [
                new Localization { LanguageCode = "nb", Value = "Søknadsskjema (PDF)" },
                new Localization { LanguageCode = "en", Value = "Application form (PDF)" }
            ],
            Urls =
            [
                new CreateDialogAttachmentUrl
                {
                    Url = new Uri("https://example.com/byggesak/2026-0042/soknad.pdf"),
                    MediaType = "application/pdf",
                    ConsumerType = AttachmentUrlConsumerType.Gui
                },
                new CreateDialogAttachmentUrl
                {
                    Url = new Uri("https://api.example.com/byggesak/2026-0042/soknad"),
                    MediaType = "application/json",
                    ConsumerType = AttachmentUrlConsumerType.Api
                }
            ]
        },
        new CreateDialogAttachment
        {
            Name = "situasjonsplan",
            DisplayName =
            [
                new Localization { LanguageCode = "nb", Value = "Situasjonsplan" },
                new Localization { LanguageCode = "en", Value = "Site plan" }
            ],
            ExpiresAt = now.AddMonths(6),
            Urls =
            [
                new CreateDialogAttachmentUrl
                {
                    Url = new Uri("https://example.com/byggesak/2026-0042/situasjonsplan.pdf"),
                    MediaType = "application/pdf",
                    ConsumerType = AttachmentUrlConsumerType.Gui
                }
            ]
        }
    ],

    // The message history of the dialog. Transmissions are immutable once created.
    Transmissions =
    [
        new CreateDialogTransmission
        {
            Id = requestTransmissionId,
            CreatedAt = now.AddDays(-5),
            Type = DialogTransmissionType.Request,
            ExtendedType = new Uri("urn:altinn:transmission:mangelbrev"),
            ExternalReference = "BREV-2026-0042-01",
            Sender = new Actor { ActorType = ActorType.ServiceOwner },
            Content = new CreateDialogTransmissionContent
            {
                Title = new ContentValue
                {
                    MediaType = "text/plain",
                    Value =
                    [
                        new Localization { LanguageCode = "nb", Value = "Anmodning om tilleggsopplysninger" },
                        new Localization { LanguageCode = "en", Value = "Request for additional information" }
                    ]
                },
                Summary = new ContentValue
                {
                    MediaType = "text/plain",
                    Value =
                    [
                        new Localization { LanguageCode = "nb", Value = "Vi trenger oppdatert situasjonsplan i målestokk 1:500 før vi kan behandle søknaden videre." },
                        new Localization { LanguageCode = "en", Value = "We need an updated site plan at 1:500 scale before we can process the application further." }
                    ]
                }
            },
            Attachments =
            [
                new CreateDialogTransmissionAttachment
                {
                    Name = "mangelbrev",
                    DisplayName =
                    [
                        new Localization { LanguageCode = "nb", Value = "Mangelbrev" },
                        new Localization { LanguageCode = "en", Value = "Deficiency letter" }
                    ],
                    Urls =
                    [
                        new CreateDialogTransmissionAttachmentUrl
                        {
                            Url = new Uri("https://example.com/byggesak/2026-0042/mangelbrev.pdf"),
                            MediaType = "application/pdf",
                            ConsumerType = AttachmentUrlConsumerType.Gui
                        }
                    ]
                }
            ],
            NavigationalActions =
            [
                new CreateDialogTransmissionNavigationalAction
                {
                    Url = new Uri("https://example.com/byggesak/2026-0042/last-opp"),
                    ExpiresAt = now.AddDays(14),
                    Title =
                    [
                        new Localization { LanguageCode = "nb", Value = "Last opp dokumentasjon" },
                        new Localization { LanguageCode = "en", Value = "Upload documentation" }
                    ]
                }
            ]
        },
        new CreateDialogTransmission
        {
            Id = submissionTransmissionId,
            CreatedAt = now.AddDays(-2),
            Type = DialogTransmissionType.Submission,
            // Ties the answer back to the request it replies to.
            RelatedTransmissionId = requestTransmissionId,
            ExternalReference = "SVAR-2026-0042-01",
            Sender = new Actor
            {
                ActorType = ActorType.PartyRepresentative,
                ActorId = "urn:altinn:person:identifier-no:08895699684"
            },
            Content = new CreateDialogTransmissionContent
            {
                Title = new ContentValue
                {
                    MediaType = "text/plain",
                    Value =
                    [
                        new Localization { LanguageCode = "nb", Value = "Innsending av oppdatert situasjonsplan" },
                        new Localization { LanguageCode = "en", Value = "Submission of updated site plan" }
                    ]
                },
                Summary = new ContentValue
                {
                    MediaType = "text/plain",
                    Value =
                    [
                        new Localization { LanguageCode = "nb", Value = "Vedlagt følger oppdatert situasjonsplan i målestokk 1:500." },
                        new Localization { LanguageCode = "en", Value = "Attached is an updated site plan at 1:500 scale." }
                    ]
                }
            },
            Attachments =
            [
                new CreateDialogTransmissionAttachment
                {
                    Name = "situasjonsplan-v2",
                    DisplayName =
                    [
                        new Localization { LanguageCode = "nb", Value = "Situasjonsplan (revidert)" },
                        new Localization { LanguageCode = "en", Value = "Site plan (revised)" }
                    ],
                    Urls =
                    [
                        new CreateDialogTransmissionAttachmentUrl
                        {
                            Url = new Uri("https://example.com/byggesak/2026-0042/situasjonsplan-v2.pdf"),
                            MediaType = "application/pdf",
                            ConsumerType = AttachmentUrlConsumerType.Gui
                        }
                    ]
                }
            ]
        }
    ],

    // Actions rendered as buttons in browser based frontends. At most one
    // action can have Primary priority.
    GuiActions =
    [
        new CreateDialogGuiAction
        {
            Action = "write",
            Priority = DialogGuiActionPriority.Primary,
            HttpMethod = HttpVerb.GET,
            Url = new Uri("https://example.com/byggesak/2026-0042"),
            Title =
            [
                new Localization { LanguageCode = "nb", Value = "Fortsett utfylling" },
                new Localization { LanguageCode = "en", Value = "Continue filling out" }
            ]
        },
        new CreateDialogGuiAction
        {
            Action = "submit",
            Priority = DialogGuiActionPriority.Secondary,
            HttpMethod = HttpVerb.POST,
            Url = new Uri("https://example.com/byggesak/2026-0042/send-inn"),
            // Custom authorization rule in the XACML policy for the service.
            AuthorizationAttribute = "urn:altinn:subresource:innsending",
            Title =
            [
                new Localization { LanguageCode = "nb", Value = "Send inn" },
                new Localization { LanguageCode = "en", Value = "Submit" }
            ],
            Prompt =
            [
                new Localization { LanguageCode = "nb", Value = "Søknaden kan ikke endres etter innsending. Vil du fortsette?" },
                new Localization { LanguageCode = "en", Value = "The application cannot be changed after submission. Do you want to continue?" }
            ]
        },
        new CreateDialogGuiAction
        {
            Action = "delete",
            Priority = DialogGuiActionPriority.Tertiary,
            HttpMethod = HttpVerb.DELETE,
            // Lets frontends render this with the appropriate destructive UX.
            IsDeleteDialogAction = true,
            Url = new Uri("https://example.com/byggesak/2026-0042"),
            Title =
            [
                new Localization { LanguageCode = "nb", Value = "Slett søknaden" },
                new Localization { LanguageCode = "en", Value = "Delete the application" }
            ],
            Prompt =
            [
                new Localization { LanguageCode = "nb", Value = "Dette sletter søknaden permanent." },
                new Localization { LanguageCode = "en", Value = "This will permanently delete the application." }
            ]
        }
    ],

    // Actions for non-browser based integrations, eg. ERP systems.
    ApiActions =
    [
        new CreateDialogApiAction
        {
            Action = "read",
            Name = "hent-byggesak",
            Endpoints =
            [
                new CreateDialogApiActionEndpoint
                {
                    Version = "v2",
                    HttpMethod = HttpVerb.GET,
                    Url = new Uri("https://api.example.com/v2/byggesak/2026-0042"),
                    ResponseSchema = new Uri("https://api.example.com/schemas/v2/byggesak.json"),
                    DocumentationUrl = new Uri("https://docs.example.com/byggesak/v2")
                },
                new CreateDialogApiActionEndpoint
                {
                    Version = "v1",
                    HttpMethod = HttpVerb.GET,
                    Url = new Uri("https://api.example.com/v1/byggesak/2026-0042"),
                    Deprecated = true,
                    SunsetAt = now.AddMonths(6)
                }
            ]
        },
        new CreateDialogApiAction
        {
            Action = "write",
            Name = "send-inn-byggesak",
            AuthorizationAttribute = "urn:altinn:subresource:innsending",
            Endpoints =
            [
                new CreateDialogApiActionEndpoint
                {
                    Version = "v2",
                    HttpMethod = HttpVerb.POST,
                    Url = new Uri("https://api.example.com/v2/byggesak/2026-0042/send-inn"),
                    RequestSchema = new Uri("https://api.example.com/schemas/v2/byggesak-innsending.json"),
                    ResponseSchema = new Uri("https://api.example.com/schemas/v2/kvittering.json")
                }
            ]
        }
    ],

    // Immutable audit trail of what has happened in the dialog.
    Activities =
    [
        new CreateDialogActivity
        {
            Id = Guid.CreateVersion7(now.AddDays(-5)),
            CreatedAt = now.AddDays(-5),
            Type = DialogActivityType.Information,
            PerformedBy = new Actor { ActorType = ActorType.ServiceOwner },
            // Description is only allowed for the "Information" activity type.
            Description =
            [
                new Localization { LanguageCode = "nb", Value = "Saken er tildelt saksbehandler." },
                new Localization { LanguageCode = "en", Value = "The case has been assigned to a case worker." }
            ]
        },
        new CreateDialogActivity
        {
            Id = Guid.CreateVersion7(now.AddDays(-2)),
            CreatedAt = now.AddDays(-2),
            Type = DialogActivityType.FormSubmitted,
            ExtendedType = new Uri("urn:altinn:activity:tilleggsdokumentasjon"),
            PerformedBy = new Actor
            {
                ActorType = ActorType.PartyRepresentative,
                ActorId = "urn:altinn:person:identifier-no:08895699684"
            }
        }
    ]
};

var ct = CancellationToken.None;
var createDialog = await serviceOwnerApi.CreateDialog(bigDialog, ct);
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
