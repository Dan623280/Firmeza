using System.ComponentModel.DataAnnotations;
namespace Firmeza.Application.Customers;

public sealed record CustomerRequest([Required, StringLength(30)] string DocumentNumber, [Required, StringLength(100)] string FirstName, [Required, StringLength(100)] string LastName, [Required, EmailAddress, StringLength(254)] string Email, [Required, StringLength(25)] string Phone, [Required, StringLength(300)] string Address);
public sealed record CustomerResponse(Guid Id, string DocumentNumber, string FirstName, string LastName, string Email, string Phone, string Address, bool IsActive);
