using System.Security.Cryptography;
using System.Text;
using ERP.WorkflowwServices.API.DTOs;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.WorkflowwServices.API.Services;

public sealed class UserInvitationService(
    WorkflowDbContext db,
    IEmailSender emailSender,
    IConfiguration configuration) : IUserInvitationService
{
    public async Task SendInvitationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User was not found.");
        if (user.IsDeleted || user.CompanyId == null)
            throw new InvalidOperationException("Invitations require a user assigned to an active company.");
        if (user.IsEmailVerified)
            throw new InvalidOperationException("This user's email has already been verified.");

        var companySettings = await db.CompanyUserSettings.AsNoTracking()
            .FirstOrDefaultAsync(item => item.CompanyId == user.CompanyId, cancellationToken);
        var expiryDays = companySettings?.InvitationExpiryDays ?? 7;
        if (expiryDays is < 1 or > 90)
            throw new InvalidOperationException("Company invitation expiry must be between 1 and 90 days.");

        var invitationBaseUrl = configuration["Email:InvitationBaseUrl"];
        if (!Uri.TryCreate(invitationBaseUrl, UriKind.Absolute, out var inviteUri)
            || inviteUri.Scheme is not ("https" or "http"))
            throw new EmailDeliveryException("Email:InvitationBaseUrl must be an absolute HTTP or HTTPS URL.");

        var rawToken = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        var now = DateTime.UtcNow;

        var outstanding = await db.UserInvitations
            .Where(item => item.UserId == userId && item.AcceptedAt == null && item.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var oldInvitation in outstanding)
            oldInvitation.RevokedAt = now;

        var invitation = new UserInvitation
        {
            Id = Guid.NewGuid(),
            TenantId = user.TenantId,
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = now.AddDays(expiryDays)
        };
        db.UserInvitations.Add(invitation);
        await db.SaveChangesAsync(cancellationToken);

        var link = new UriBuilder(inviteUri);
        var existingQuery = link.Query.TrimStart('?');
        link.Query = string.IsNullOrEmpty(existingQuery)
            ? $"token={Uri.EscapeDataString(rawToken)}"
            : $"{existingQuery}&token={Uri.EscapeDataString(rawToken)}";
        await emailSender.SendAsync(
            user.Email ?? throw new InvalidOperationException("User has no email address."),
            "Complete your account registration",
            $"Use this secure link to complete your account setup: {link.Uri}\nThis invitation expires on {invitation.ExpiresAt:O}.",
            cancellationToken);
    }

    public async Task<UserInvitationAcceptedDto> AcceptAsync(
        AcceptUserInvitationRequest request,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var invitation = await db.UserInvitations.IgnoreQueryFilters()
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.TokenHash == tokenHash
                && item.AcceptedAt == null && item.RevokedAt == null
                && item.ExpiresAt > DateTime.UtcNow && !item.IsDeleted, cancellationToken);
        if (invitation?.User == null || invitation.User.IsDeleted)
            throw new InvalidOperationException("This invitation is invalid, expired, or already used.");

        var settings = await db.CompanyUserSettings.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(item => item.TenantId == invitation.TenantId
                && item.CompanyId == invitation.User.CompanyId && !item.IsDeleted, cancellationToken);
        invitation.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        invitation.User.PasswordSalt = null;
        invitation.User.IsEmailVerified = true;
        invitation.User.MustChangePassword = false;
        invitation.User.IsActive = settings?.RequireAdminApproval != true;
        invitation.AcceptedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new UserInvitationAcceptedDto(settings?.RequireAdminApproval == true);
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
