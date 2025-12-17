using LiquidDocsData.Models;

namespace DocumentManager.Services;
public interface ILoanApplicationBackgroundService
{
    event EventHandler<LoanAgreement> OnLoanCompletedEvent;
    event EventHandler<LoanAgreement> OnLoanErrorEvent;

    Task DoSomeCleanupAsync();
    Task DoSomeInitializationAsync();
    Task DoSomeRecoveryAsync();
    Task ProcessLoanAsync(LoanAgreement loan, CancellationToken stoppingToken);
    Task ProcessTestDocumentAsync(byte[] documentBytes, LoanAgreement loan, CancellationToken stoppingToken);
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}