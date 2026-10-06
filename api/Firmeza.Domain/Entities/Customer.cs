using System.Net.Mail;
using Firmeza.Domain.Abstractions;
using Firmeza.Domain.ValueObjects;
namespace Firmeza.Domain.Entities;

public sealed class Customer : AggregateRoot
{
    public string? UserId
    {
        get; private set;
    }
    public string DocumentNumber { get; private set; } = "";
    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string Phone { get; private set; } = "";
    public string Address { get; private set; } = "";
    public bool IsActive { get; private set; } = true;
    private Customer()
    {
    }
    public static Customer Create(string document, string firstName, string lastName, string email, string phone, string address)
    {
        var customer = new Customer();
        customer.Update(document, firstName, lastName, email, phone, address);
        return customer;
    }
    public void Update(string document, string firstName, string lastName, string email, string phone, string address)
    {
        var validDocument = ValueObjects.DocumentNumber.Create(document).Value;
        var first = Guard.Text(firstName, "First name", 100);
        var last = Guard.Text(lastName, "Last name", 100);
        email = Guard.Text(email, "Email", 254).ToLowerInvariant();
        Guard.Require(MailAddress.TryCreate(email, out var parsed) && parsed.Address == email, "Email address is invalid.");
        var validPhone = Guard.Text(phone, "Phone", 25);
        Guard.Require(validPhone.Length >= 7 && validPhone.All(c => char.IsAsciiDigit(c) || c is '+' or '-' or ' ' or '(' or ')'), "Phone number is invalid.");
        var validAddress = Guard.Text(address, "Address", 300);
        DocumentNumber = validDocument;
        FirstName = first;
        LastName = last;
        Email = email;
        Phone = validPhone;
        Address = validAddress;
        Touch();
    }
    public void LinkUser(string userId)
    {
        Guard.Require(UserId is null, "Customer already has an account.");
        UserId = Guard.Text(userId, "User ID", 450);
        Touch();
    }
    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }
}
