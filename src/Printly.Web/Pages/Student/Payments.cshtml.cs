using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Printly.Core.DTOs.Responses;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Services;

namespace Printly.Web.Pages.Student;

[Authorize(Roles = "Student")]
public class StudentPaymentsModel : PageModel
{
    private readonly IPaymentService _paymentService;
    private readonly CurrentUserService _currentUser;

    public StudentPaymentsModel(IPaymentService paymentService, CurrentUserService currentUser)
    {
        _paymentService = paymentService;
        _currentUser = currentUser;
    }

    public List<PaymentResponse> Payments { get; set; } = new();

    public async Task OnGetAsync()
    {
        Payments = await _paymentService.GetUserPaymentsAsync(_currentUser.UserId, _currentUser.OrgId);
    }
}
