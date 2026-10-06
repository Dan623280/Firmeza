using Firmeza.Application.Common;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
namespace Firmeza.Infrastructure.DataExchange;

internal static class ExcelLicense
{
    public static void Configure(IConfiguration config)
    {
        var value = config["Licenses:EPPlus"] ?? "";
        if (value.StartsWith("Commercial:", StringComparison.Ordinal) && value.Length > 11)
            ExcelPackage.License.SetCommercial(value[11..]);
        else if (value.StartsWith("NonCommercialPersonal:", StringComparison.Ordinal) && value.Length > 22)
            ExcelPackage.License.SetNonCommercialPersonal(value[22..]);
        else if (value.StartsWith("NonCommercialOrganization:", StringComparison.Ordinal) && value.Length > 26)
            ExcelPackage.License.SetNonCommercialOrganization(value[26..]);
        else
            throw new RequestException("excel_not_configured", "Set Licenses__EPPlus to the appropriate licensed configuration.", 503);
    }
}
