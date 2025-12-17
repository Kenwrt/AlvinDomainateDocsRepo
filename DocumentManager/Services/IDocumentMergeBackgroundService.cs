using LiquidDocsData.Models;

namespace DocumentManager.Services;
public interface IDocumentMergeBackgroundService
{
    event EventHandler<DocumentMerge> OnDocCompletedEvent;
    event EventHandler<DocumentMerge> OnDocErrorEvent;

    Task DoSomeCleanupAsync();
    Task DoSomeInitializationAsync();
    Task DoSomeRecoveryAsync();
    Task MergeDocumentAsync(DocumentMerge documentMerge, CancellationToken stoppingToken);
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}