# Dependency licensing and setup

Package versions are pinned in the project files. Verify license eligibility for the legal entity deploying the application. No license key is checked in, and no commercial license has been purchased as part of this implementation.

| Dependency | Version | Configuration |
| --- | --- | --- |
| ASP.NET Core/EF Core | 10.0.12 | Framework packages; EF schema through migrations |
| Npgsql EF provider | 10.0.3 | PostgreSQL connection string |
| AutoMapper | 16.2.0 | `AutoMapper__LicenseKey` or upstream `AUTOMAPPER_LICENSE_KEY` |
| EPPlus | 8.7.1 | `Licenses__EPPlus` |
| QuestPDF | 2026.9.1 | `Licenses__QuestPdf` |
| MailKit | 4.18.1 | SMTP server, username, password and sender |
| System.IO.Hashing | 10.0.12 | Streaming CRC validation for workbook ZIP entries |

EPPlus 8 uses a noncommercial/community and commercial licensing model. A real hardware store requires commercial licensing. Accepted application configurations are `Commercial:<key>`, `NonCommercialPersonal:<eligible person's name>`, and `NonCommercialOrganization:<eligible organization's name>`. Noncommercial usage must actually qualify; it is not an automatic development exemption. See [EPPlus licensing](https://epplussoftware.com/docs/8.0/api/index.html) and [configuration examples](https://github.com/EPPlusSoftware/EPPlus/wiki/Getting-Started).

QuestPDF requires the appropriate configured license category: `Community`, `Professional`, or `Enterprise`. Eligibility varies by entity and use; see [the license selection guide](https://www.questpdf.com/license/guide.html) and [configuration](https://www.questpdf.com/license/configuration.html). Missing configuration returns a clear unavailable-service error for PDFs instead of selecting a license silently.

AutoMapper is dual licensed and accepts a license key in configuration or an environment variable. Its upstream enforcement uses log messages; do not interpret working code as license eligibility. See [official license configuration](https://docs.automapper.io/en/stable/License-configuration.html).

The automated learning tests explicitly name their noncommercial EPPlus use and select QuestPDF Community for learning/evaluation. These test settings do not configure or establish eligibility for a store deployment.

Identity and JWT validation follow the configured closed-system requirements of the supplied project. Before public production rollout, evaluate an OAuth/OpenID Connect identity provider appropriate to deployment requirements; see [Microsoft JWT guidance](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0). No external identity provider is configured here.
