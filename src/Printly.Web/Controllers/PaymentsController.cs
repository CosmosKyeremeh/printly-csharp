using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printly.Core.DTOs.Requests;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Services;

namespace Printly.Web.Controllers;

[Authorize]
public class PaymentsController : BaseApiController
{
    private readonly IPaymentService _paymentService;
    private readonly CurrentUserService _currentUser;

    public PaymentsController(IPaymentService paymentService, CurrentUserService currentUser)
    {
        _paymentService = paymentService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// POST /api/payments/initiate
    /// Student initiates a MoMo payment for a file.
    /// </summary>
    [HttpPost("initiate")]
    public Task<IActionResult> Initiate([FromBody] InitiatePaymentRequest request)
        => ExecuteAsync(() => _paymentService.InitiateMoMoPaymentAsync(
            request.FileId,
            request.MoMoPhoneNumber,
            _currentUser.UserId,
            _currentUser.OrgId));

    /// <summary>
    /// POST /api/payments/webhook
    /// Called by Hubtel's servers when a payment completes or fails.
    ///
    /// [AllowAnonymous] overrides the class-level [Authorize] —
    /// Hubtel doesn't have a JWT so this endpoint must be public.
    /// We verify the request is genuine using Hubtel's signature instead.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("webhook")]
    public Task<IActionResult> Webhook([FromBody] HubtelWebhookRequest request)
        => ExecuteAsync(() => _paymentService.HandleWebhookAsync(request));

    /// <summary>
    /// PATCH /api/payments/{id}/cash
    /// Admin marks a file as cash-paid manually.
    /// </summary>
    [Authorize(Roles = "Admin,Superadmin,PlatformOwner")]
    [HttpPatch("{id:guid}/cash")]
    public Task<IActionResult> MarkCashPaid(Guid id)
        => ExecuteAsync(() => _paymentService.MarkAsCashPaidAsync(
            id, _currentUser.UserId, _currentUser.OrgId));

    /// <summary>
    /// GET /api/payments
    /// Students see their own payment history.
    /// Admins see the full org payment history.
    /// </summary>
    [HttpGet]
    public Task<IActionResult> GetPayments()
        => ExecuteAsync(() => _currentUser.IsAdmin
            ? _paymentService.GetOrgPaymentsAsync(_currentUser.OrgId)
            : _paymentService.GetUserPaymentsAsync(_currentUser.UserId, _currentUser.OrgId));
}
