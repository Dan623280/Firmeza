namespace Firmeza.Application.Rentals;

public sealed record CreateRentalRequest(Guid VehicleId, DateOnly StartDate, DateOnly EndDate);
public sealed record RentalResponse(Guid Id, Guid CustomerId, Guid VehicleId, DateOnly StartDate, DateOnly EndDate, decimal DailyPrice, decimal Total, string Status);
