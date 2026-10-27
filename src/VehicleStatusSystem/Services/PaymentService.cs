
using System.ComponentModel.DataAnnotations;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    public async Task<PaymentResult> PayAsync(
        int orderId,
        int customerId,
        string method,
        Guid idempotencyKey)
    {
        // 1. 验证订单 ID
        if (orderId <= 0)
        {
            throw new ValidationException(
                "Invalid order id.");
        }

        // 2. 验证用户 ID
        if (customerId <= 0)
        {
            throw new ValidationException(
                "Invalid customer id.");
        }

        // 3. 验证幂等键
        if (idempotencyKey == Guid.Empty)
        {
            throw new ValidationException(
                "Invalid idempotency key.");
        }

        // 4. 验证模拟支付方式
        if (string.IsNullOrWhiteSpace(method))
        {
            throw new ValidationException(
                "Payment method is required.");
        }

        string normalizedMethod = method.Trim();

        if (normalizedMethod != "MockWallet" &&
            normalizedMethod != "MockCard")
        {
            throw new ValidationException(
                "Unsupported payment method.");
        }

        // 5. 调用 Repository
        PaymentResult result =
            await _paymentRepository.PayAsync(
                orderId,
                customerId,
                normalizedMethod,
                idempotencyKey);

        // 6. 处理支付业务结果
        switch (result.Status)
        {
            case PaymentStatus.Succeeded:
                _logger.LogInformation(
                    "Payment succeeded. OrderId={OrderId}, PaymentNo={PaymentNo}",
                    orderId,
                    result.PaymentNo);
                break;

            case PaymentStatus.IdempotentReplay:
                _logger.LogInformation(
                    "Idempotent payment replay. OrderId={OrderId}, PaymentNo={PaymentNo}",
                    orderId,
                    result.PaymentNo);
                break;

            case PaymentStatus.IdempotencyConflict:
                _logger.LogWarning(
                    "Idempotency key conflict. OrderId={OrderId}",
                    orderId);
                break;

            case PaymentStatus.AlreadyPaid:
                _logger.LogInformation(
                    "Order {OrderId} was already paid.",
                    orderId);
                break;

            case PaymentStatus.Expired:
                _logger.LogWarning(
                    "Payment rejected: order {OrderId} expired.",
                    orderId);
                break;

            case PaymentStatus.InvalidOrderStatus:
                _logger.LogWarning(
                    "Payment rejected: invalid order status. OrderId={OrderId}",
                    orderId);
                break;

            case PaymentStatus.NotFound:
                _logger.LogInformation(
                    "Payment requested for an unavailable order.");
                break;

            default:
                throw new InvalidOperationException(
                    "Unknown payment result.");
        }

        return result;
    }
}
