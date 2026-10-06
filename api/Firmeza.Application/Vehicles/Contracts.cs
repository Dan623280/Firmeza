using System.ComponentModel.DataAnnotations;
namespace Firmeza.Application.Vehicles;

public sealed record VehicleRequest([Required, StringLength(150)] string Name, [Required, StringLength(100)] string Type, [Required, StringLength(100)] string Brand, [Required, StringLength(20)] string LicensePlate, [Range(typeof(decimal), "0.01", "9999999999.99")] decimal DailyPrice);
public sealed record VehicleResponse(Guid Id, string Name, string Type, string Brand, string LicensePlate, decimal DailyPrice, bool IsActive);
