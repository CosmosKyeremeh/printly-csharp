using Printly.Core.DTOs.Requests;
using Printly.Core.DTOs.Responses;
using Printly.Core.Entities;
using Printly.Core.Enums;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Printly.Infrastructure.Services;

/// <summary>
/// Payment service stub — MoMo initiation returns a placeholder
/// until the Hubtel SDK is integrated in Step 8.
/// Cash payment marking is fully implemented now.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly PrintlyDbContext _context;

    public PaymentService(PrintlyDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentResponse> InitiateMoMoPaymentAsync(
        Guid fileId, string momoPhoneNumber, Guid userId, Guid orgId)
    {
        var file = await _context.FileRecords
            .FirstOrDefaultAsync(f => f.Id == fileId && f.OrgId == orgId)
            ?? throw new InvalidOperationException("File not found.");

        var payment = new Payment
        {
            FileRecordId = fileId,
            UserId = userId,
            OrgId = orgId,
            Amount = file.Price,
            Method = PaymentMethod.MoMo,
            Status = PaymentStatus.Pending,
            MoMoPhoneNumber = momoPhoneNumber
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Hubtel API call goes here in Step 8
        return MapToResponse(payment, file.OriginalFileName);
    }

    public async Task HandleWebhookAsync(HubtelWebhookRequest webhook)
    {
        // Hubtel webhook handling goes here in Step 8
        await Task.CompletedTask;
    }

    public async Task<PaymentResponse> MarkAsCashPaidAsync(
        Guid fileId, Guid adminUserId, Guid orgId)
    {
        var file = await _context.FileRecords
            .FirstOrDefaultAsync(f => f.Id == fileId && f.OrgId == orgId)
            ?? throw new InvalidOperationException("File not found.");

        var payment = new Payment
        {
            FileRecordId = fileId,
            UserId = file.UserId,
            OrgId = orgId,
            Amount = file.Price,
            Method = PaymentMethod.Cash,
            Status = PaymentStatus.Paid,
            PaidAt = DateTime.UtcNow
        };

        file.PaymentStatus = PaymentStatus.Paid;

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return MapToResponse(payment, file.OriginalFileName);
    }

    public async Task<List<PaymentResponse>> GetUserPaymentsAsync(Guid userId, Guid orgId)
    {
        var payments = await _context.Payments
            .Include(p => p.FileRecord)
            .Where(p => p.UserId == userId && p.OrgId == orgId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return payments.Select(p => MapToResponse(p, p.FileRecord.OriginalFileName)).ToList();
    }

    public async Task<List<PaymentResponse>> GetOrgPaymentsAsync(Guid orgId)
    {
        var payments = await _context.Payments
            .Include(p => p.FileRecord)
            .Where(p => p.OrgId == orgId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return payments.Select(p => MapToResponse(p, p.FileRecord.OriginalFileName)).ToList();
    }

    private static PaymentResponse MapToResponse(Payment payment, string fileName)
        => new()
        {
            Id = payment.Id,
            FileRecordId = payment.FileRecordId,
            FileName = fileName,
            Amount = payment.Amount,
            Method = payment.Method.ToString(),
            Status = payment.Status.ToString(),
            HubtelTransactionId = payment.HubtelTransactionId,
            MoMoPhoneNumber = payment.MoMoPhoneNumber,
            PaidAt = payment.PaidAt,
            CreatedAt = payment.CreatedAt
        };
}
