using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Firmeza.Application.DTOs;
using Firmeza.Application.Interfaces;
using Firmeza.Domain.Constants;
using Firmeza.Domain.Entities;
using Firmeza.Domain.Exceptions;
using Firmeza.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
namespace Firmeza.Infrastructure.Services;

public sealed class AuthService(UserManager<IdentityUser> users, AppDbContext db, IConfiguration config, IMapper mapper) : IAuthService
{
    public async Task<AuthDto> LoginAsync(LoginInput input, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(input.Email);
        if (user == null || await users.IsLockedOutAsync(user))
            throw new BusinessException("Credenciales inválidas o cuenta temporalmente bloqueada.", 401);
        if (!await users.CheckPasswordAsync(user, input.Password))
        {
            await users.AccessFailedAsync(user);
            throw new BusinessException("Credenciales inválidas.", 401);
        }
        var roles = await users.GetRolesAsync(user);
        var customer = await db.Customers.SingleOrDefaultAsync(c => c.UserId == user.Id, ct);
        if (!roles.Contains(Roles.Administrator) && (customer == null || !customer.Active))
            throw new BusinessException("Cuenta desactivada.", 403);
        await users.ResetAccessFailedCountAsync(user);
        return await Token(user, customer?.Name ?? "Administrador");
    }
    public async Task<AuthDto> RegisterAsync(RegisterInput input, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (await db.Customers.AnyAsync(c => c.Document == input.Customer.Document || c.Email == input.Email, ct))
            throw new BusinessException("Documento o correo ya registrado.", 409);
        var user = new IdentityUser { UserName = input.Email, Email = input.Email };
        var result = await users.CreateAsync(user, input.Password);
        if (!result.Succeeded)
            throw new BusinessException(string.Join(" ", result.Errors.Select(e => e.Description)));
        var role = await users.AddToRoleAsync(user, Roles.Customer);
        if (!role.Succeeded)
            throw new BusinessException("No se pudo asignar el rol.");
        var customer = mapper.Map<Customer>(input.Customer);
        customer.Email = input.Email;
        customer.UserId = user.Id;
        customer.Active = true;
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await Token(user, customer.Name);
    }
    async Task<AuthDto> Token(IdentityUser user, string name)
    {
        var roles = await users.GetRolesAsync(user);
        var expiry = DateTime.UtcNow.AddHours(2);
        List<Claim> claims = [new(JwtRegisteredClaimNames.Sub, user.Id), new(ClaimTypes.NameIdentifier, user.Id), new(ClaimTypes.Name, name), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())];
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        var token = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], claims, expires: expiry, signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token), expiry, name, roles.First());
    }
}
