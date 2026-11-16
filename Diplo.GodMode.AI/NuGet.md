# Diplo God Mode AI

Diplo God Mode AI is the optional [Umbraco.AI](https://docs.umbraco.com/ai-in-umbraco) companion package for **[Diplo God Mode](https://www.nuget.org/packages/Diplo.GodMode)**.

It adds contextual **Explain** buttons to supported God Mode views, helping developers understand diagnostics, service registrations, log patterns, content metadata, data types, templates, partials, tags, and other implementation details from inside the Umbraco 17 backoffice.

## Requirements

This package targets:

* **.NET 10** (`net10.0`)
* **Umbraco 17**
* Published AI `1.0.0`: `Diplo.GodMode` `17.1.0` or later (including `17.4.0`)
* `Umbraco.AI`

The package supports Umbraco `17.x`. Because `Umbraco.AI` currently depends on Umbraco CMS `17.3.0` or later, God Mode AI declares its Umbraco CMS dependencies as `[17.3.0,18.0.0)`.

A local AI rebuild from the core `17.4.0` release declares a minimum God Mode dependency of `17.4.0` through its project reference. AI `1.0.0` is already published; a future AI release needs a new version before uploading.

## Umbraco.AI Setup

God Mode AI uses your [Umbraco.AI](https://docs.umbraco.com/ai-in-umbraco) configuration. Before using the **Explain** buttons, make sure Umbraco.AI has a working provider connection and profile:

1. Install [Umbraco.AI](https://marketplace.umbraco.com/package/umbraco.ai) in the Umbraco site.
2. Install an AI provider package, such as the OpenAI provider.
3. In the Umbraco backoffice, create a connection for your provider.
4. Create an AI profile that uses that connection.
5. Select the model you want the profile to use, for example `gpt-4.1-nano` for OpenAI.
6. Restart the site and open God Mode in the backoffice.

## Configuration

Scheduled Publishing includes an on-demand **Explain scheduling health** action.
It combines the workspace's schedule, publishing-job and server-registration
evidence into an overall assessment and suggested checks. It uses bounded samples
with complete counts, reports unknown evidence, and does not treat a recent job
timestamp as proof of successful publishing. It cannot execute fixes or cleanup.

Settings are optional and read from the `Diplo:GodMode:AI` section of `appsettings.json`:

```json
{
  "Diplo": {
    "GodMode": {
      "AI": {
        "ChatProfileAlias": "my-profile-alias",
        "MaxOutputTokens": 4096
      }
    }
  }
}
```

* `ChatProfileAlias` is the alias of the Umbraco.AI profile used for explanations. When it is not set, Umbraco.AI selects the profile using its own defaults.
* `MaxOutputTokens` and `Temperature` override the profile's chat options for God Mode explanations only.

God Mode requests a structured JSON explanation. A small output budget can cut
off the response and cause an incomplete JSON error. Start with an allowance such
as `4096`, then adjust for the selected model and the size of the explanation.
This allowance completed an explanation in local testing; it is not a guaranteed
minimum for every model. Reasoning models may use part of the allowance before
producing visible output. Higher limits can increase latency and cost.

Leave `Temperature` unset unless the selected model supports that option. After
cloning or upgrading a site, an authentication failure may require re-entering
the provider connection's credentials; changing the token allowance will not fix
authentication errors.

Useful Umbraco.AI docs:

* Getting started: https://docs.umbraco.com/ai-in-umbraco/getting-started/getting-started
* Installation: https://docs.umbraco.com/ai-in-umbraco/getting-started/installation
* First connection: https://docs.umbraco.com/ai-in-umbraco/getting-started/first-connection
* First profile: https://docs.umbraco.com/ai-in-umbraco/getting-started/first-profile

## Screenshots

![Umbraco AI provider setup](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/AI/AI-provider-setup.png)

![Diagnostic explanation](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/AI/diagnostic-explain.png)

![Log Insights analysis](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/AI/log-analyser.png)

![Log error explanation](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/AI/log-error.png)

![Data Type explanation](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/AI/datatype-explain.png)

![Health check explanation](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/AI/health-explain.png)

![Key value explanation](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/AI/key-value-explain.png)

## Links

* God Mode NuGet: https://www.nuget.org/packages/Diplo.GodMode
* God Mode AI NuGet: https://www.nuget.org/packages/Diplo.GodMode.AI
* GitHub: https://github.com/DanDiplo/Umbraco.GodMode
* Umbraco Marketplace: https://marketplace.umbraco.com/package/diplo.godmode
