using BusinessLogicLayer.DTO;
using FluentValidation;

namespace BusinessLogicLayer.Validators;

public class OrderAddRequestValidator : AbstractValidator<OrderAddRequest>
{
    public OrderAddRequestValidator()
    {
        // UserId
        RuleFor(order => order.UserID)
            .NotEmpty().WithMessage("UserID is required.");

        // OrderDate
        RuleFor(order => order.OrderDate)
            .NotEmpty().WithMessage("Order Date is required.");

        // OrderItems
        RuleFor(order => order.OrderItems)
            .NotEmpty().WithMessage("At least one order item is required.");
    }
}
