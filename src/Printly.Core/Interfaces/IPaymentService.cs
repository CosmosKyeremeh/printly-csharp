using Printly.Core.DTOs.Requests;
using Printly.Core.DTOs.Responses;

namespace Printly.Core.Interfaces;

/// <summary>
/// Handles payment initiation (MoMo via Hubtel) and
/// cash payment recording by admins.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Initiates a Hubtel MoMo USSD push to the student's phone.
    /// Returns a transaction reference so we can track the outcome.
    /// </summary>
    Task<PaymentResponse> InitiateMoMoPaymentAsync(
        Guid fileId,
        string momoPhoneNumber,
        Guid userId,
        Guid orgId);

    /// <summary>
    /// Called by the Hubtel webhook when a payment is confirmed or fails.
    /// Updates the payment status and the file's PaymentStatus automatically.
    /// </summary>
    Task HandleWebhookAsync(HubtelWebhookRequest webhook);

    /// <summary>
    /// Admin manually marks a file as cash-paid.
    /// No external API call — just updates the database.
    /// </summary>
    Task<PaymentResponse> MarkAsCashPaidAsync(
        Guid fileId,
        Guid adminUserId,
        Guid orgId);

    /// <summary>
    /// Get all payments for a specific user (student's history page).
    /// </summary>
    Task<List<PaymentResponse>> GetUserPaymentsAsync(Guid userId, Guid orgId);

    /// <summary>
    /// Get all payments across the org (admin's payments overview).
    /// </summary>
    Task<List<PaymentResponse>> GetOrgPaymentsAsync(Guid orgId);
}
