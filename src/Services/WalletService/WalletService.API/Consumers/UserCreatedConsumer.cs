using Common.Contracts.Events;
using MassTransit;
using WalletService.Domain.Entities;
using WalletService.Domain.Repositories;

namespace WalletService.API.Consumers;

public class UserCreatedConsumer : IConsumer<UserCreatedEvent>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<UserCreatedConsumer> _logger;

    public UserCreatedConsumer(
        IWalletRepository walletRepository,
        IPublishEndpoint publishEndpoint,
        ILogger<UserCreatedConsumer> logger)
    {
        _walletRepository = walletRepository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received UserCreatedEvent for UserId: {UserId}", message.UserId);

        try
        {
            // Check if wallet already exists
            var existingWallet = await _walletRepository.GetByUserIdAsync(message.UserId);
            if (existingWallet != null)
            {
                _logger.LogWarning("Wallet already exists for UserId: {UserId}", message.UserId);
                return;
            }

            // Create new wallet
            var wallet = Wallet.Create(message.UserId);
            await _walletRepository.AddAsync(wallet);

            _logger.LogInformation("Created wallet {WalletId} for user {UserId}", wallet.Id, message.UserId);

            // Publish WalletCreatedEvent
            await _publishEndpoint.Publish(new WalletCreatedEvent
            {
                WalletId = wallet.Id,
                UserId = wallet.UserId,
                Currency = wallet.Currency,
                CreatedAt = wallet.CreatedAt
            });

            _logger.LogInformation("Published WalletCreatedEvent for wallet {WalletId}", wallet.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating wallet for UserId: {UserId}", message.UserId);
            throw;
        }
    }
}
