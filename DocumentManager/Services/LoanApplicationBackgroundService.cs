using DocumentFormat.OpenXml.Wordprocessing;
using DocumentManager.State;
using LiquidDocsData.Enums;
using LiquidDocsData.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenXmlPowerTools;
using System.Reflection.Metadata;
using System.Text;

namespace DocumentManager.Services;

public class LoanApplicationBackgroundService : BackgroundService, ILoanApplicationBackgroundService
{
    private readonly ILogger<LoanApplicationBackgroundService> logger;

    private readonly IOptions<DocumentManagerConfigOptions> options;

    private readonly IDocumentManagerState docState;

    private readonly IWordServices wordServices;

    private readonly IRazorLiteService razorLiteService;

    private readonly IDocumentMergeBackgroundService documentMergeBackgroundService;

    private SemaphoreSlim loanSemaphoreSlim = null;

    public event EventHandler<LoanAgreement> OnLoanCompletedEvent;

    public event EventHandler<LoanAgreement> OnLoanErrorEvent;

    private IWebHostEnvironment env;

    public LoanApplicationBackgroundService(ILogger<LoanApplicationBackgroundService> logger, IOptions<DocumentManagerConfigOptions> options, IDocumentManagerState docState, IWordServices wordServices, IRazorLiteService razorLiteService, IWebHostEnvironment env, IDocumentMergeBackgroundService documentMergeBackgroundService)
    {
        this.logger = logger;
        this.options = options;
        this.docState = docState;
        this.wordServices = wordServices;
        this.razorLiteService = razorLiteService;
        this.env = env;
        this.documentMergeBackgroundService = documentMergeBackgroundService;

        loanSemaphoreSlim = new(options.Value.MaxLoanApplicationThreads);

        docState.IsRunBackgroundLoanApplicationServiceChanged += OnIsRunBackgroundLoanServiceChanged;

        documentMergeBackgroundService.OnDocCompletedEvent += OnDocCompleted;

    }



    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (options.Value.IsActive)
                {
                    if (docState.IsRunBackgroundLoanApplicationService)
                    {
                        List<Task> loanTasks = new();

                        while (docState.LoanProcessQueue.Count > 0 && !stoppingToken.IsCancellationRequested)
                        {
                            logger.LogDebug("Loan Processing Background Service Found Job at: {time} Processong Queue Entry", DateTimeOffset.Now);

                            try
                            {
                                LoanAgreement loan = null;

                                docState.LoanProcessQueue.TryDequeue(out loan);

                                if (loan is not null) loanTasks.Add(Task.Run(async () => await ProcessLoanAsync(loan, stoppingToken)));
                            }
                            catch (Exception ex)
                            {
                                logger.LogError($"{ex.Message}");
                            }
                        }

                        if (loanTasks.Count > 0) await Task.WhenAll(loanTasks);

                        if (docState.LoanProcessQueue.Count == 0) logger.LogDebug("Loan Processing Background Service running at: {time}  Nothing Queued", DateTimeOffset.Now);
                    }
                    else
                    {
                        logger.LogDebug("Loan Processing Background Service PAUSED at: {time}", DateTimeOffset.Now);
                    }
                }
                else
                {
                    logger.LogDebug($"Loan Processing Background Service NOT Active");
                }

                docState.IsReadyForProcessing = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            await Task.Delay(TimeSpan.FromMinutes(2));
        }
    }

    public async Task ProcessLoanAsync(LoanAgreement loan, CancellationToken stoppingToken)
    {
        LoanAgreement loanState = null;

        try
        {
            await loanSemaphoreSlim.WaitAsync();

            docState.LoanList.TryAdd(loan.Id, loan);

            docState.StateHasChanged();

            //Create Folder for DocumentSet

            var documentSetPath = Path.Combine(env.WebRootPath, "MergedDocuments", $"{loan.Id.ToString()}_{loan.DocumentSet.Id.ToString()}");


            if (Directory.Exists(documentSetPath)) Directory.CreateDirectory(documentSetPath);

            //Prepare Loan Agreement Names and Signature Lines
            StringBuilder lenderNames = new StringBuilder();
            StringBuilder borrowerNames = new StringBuilder();
            StringBuilder brokerNames = new StringBuilder();
            StringBuilder propertyAddresses = new StringBuilder();

            loan.LenderNames = await BuildLenderNamesAsync(loan.Lenders);

            loan.BorrowerNames = await BuildPartyNamesAsync<LiquidDocsData.Models.Borrower>(loan.Borrowers);

            loan.BrokerNames = await BuildPartyNamesAsync<LiquidDocsData.Models.Broker>(loan.Brokers);

            loan.GuarantorNames = await BuildPartyNamesAsync<LiquidDocsData.Models.Guarantor>(loan.Guarantors);

            loan.PropertyAddresses = await BuildPropertyAddressAsync<LiquidDocsData.Models.PropertyRecord>(loan.Properties);

            // loan.DocumentTitle = documentMerge.Document.Name;



            //Get the Document Set
            foreach (var doc in loan.DocumentSet.Documents)
            {
                DocumentMerge documentMerge = new()
                {
                    LoanAgreement = loan,
                    Document = doc,
                    Status = DocumentMergeState.Status.Queued
                };

                //Send to Document Merge Processing Queue
                docState.DocumentProcessingQueue.Enqueue(documentMerge);

            }

            //Zip Document Set

            //Write Document Store in Azure


            //Make document available for Distribution  Add to the Distribution Queue

            docState.LoanList.Remove(loan.Id, out loanState);

            OnLoanCompletedEvent?.Invoke(this, loan);
        }
        catch (Exception)
        {
            OnLoanErrorEvent?.Invoke(this, loan);
            docState.LoanList.Remove(loan.Id, out loanState);
            throw;
        }
        finally
        {
            loanSemaphoreSlim?.Release();
        }
    }

    private Task<string> BuildPartyNamesAsync<T>(IEnumerable<T> parties, CancellationToken cancellationToken = default) where T : IPartyNames
    {
        if (parties is null) return Task.FromResult(string.Empty);

        var sb = new StringBuilder();
        var first = true;

        foreach (var p in parties)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var isIndividual = p.EntityType == LiquidDocsData.Enums.Entity.Types.Individual;

            string line = isIndividual ? $"{p.EntityName} a {p.EntityType}" : $"{p.EntityName} a {p.StateOfIncorporationDescription} {p.EntityStructureDescription}";

            if (first)
            {
                sb.AppendLine(line);
                first = false;
            }
            else
            {
                sb.AppendLine($", {line}");
            }
        }

        return Task.FromResult(sb.ToString());
    }

    private Task<string> BuildLenderNamesAsync(IEnumerable<LiquidDocsData.Models.Lender> lender, CancellationToken cancellationToken = default)
    {
        if (lender is null) return Task.FromResult(string.Empty);

        var sb = new StringBuilder();
        var first = true;

        foreach (var p in lender)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var isIndividual = p.EntityType == LiquidDocsData.Enums.Entity.Types.Individual;

            string line = "";
            if (p.NmlsLicenseNumber is not null)
            {
                line = isIndividual ? $"{p.EntityName} a {p.EntityType}" : $"{p.EntityName} a {p.StateOfIncorporationDescription} {p.EntityStructureDescription} (CFL License No.{p.NmlsLicenseNumber})";
            }
            else
            {
                line = isIndividual ? $"{p.EntityName} a {p.EntityType}" : $"{p.EntityName} a {p.StateOfIncorporationDescription} {p.EntityStructureDescription}";
            }

            if (first)
            {
                sb.AppendLine(line);
                first = false;
            }
            else
            {
                sb.AppendLine($", {line}");
            }
        }

        return Task.FromResult(sb.ToString());
    }

    private Task<string> BuildSigningPartyNamesAsync<T>(IEnumerable<T> parties, CancellationToken cancellationToken = default) where T : ISigningPartyNames
    {
        if (parties is null) return Task.FromResult(string.Empty);

        var sb = new StringBuilder();
        var first = true;

        foreach (var p in parties)
        {
            if (cancellationToken.IsCancellationRequested) break;

            if (first)
            {
                sb.AppendLine($"{p.Name} as {p.Title}");
                first = false;
            }
            else
            {
                sb.AppendLine($", {p.Name} as {p.Title}");
            }
        }

        return Task.FromResult(sb.ToString());
    }

    private Task<string> BuildAliasPartyNamesAsync<T>(IEnumerable<T> parties, CancellationToken cancellationToken = default) where T : IAliasNames
    {
        if (parties is null) return Task.FromResult(string.Empty);

        var sb = new StringBuilder();
        var first = true;

        foreach (var p in parties)
        {
            if (cancellationToken.IsCancellationRequested) break;

            if (first)
            {
                sb.AppendLine($"{p.Name} as {p.AlsoKnownAs}");
                first = false;
            }
            else
            {
                sb.AppendLine($", {p.Name} as {p.AlsoKnownAs}");
            }
        }

        return Task.FromResult(sb.ToString());
    }

    private Task<string> BuildOwnershipPartyNamesAsync<T>(IEnumerable<T> parties, CancellationToken cancellationToken = default) where T : IOwnershipNames
    {
        if (parties is null) return Task.FromResult(string.Empty);

        var sb = new StringBuilder();
        var first = true;

        foreach (var p in parties)
        {
            if (cancellationToken.IsCancellationRequested) break;

            if (first)
            {
                sb.AppendLine($"{p.Name} a {p.PercentOfOwnership}% owner");
                first = false;
            }
            else
            {
                sb.AppendLine($", {p.Name} a {p.PercentOfOwnership}% owner");
            }
        }

        return Task.FromResult(sb.ToString());
    }

    private Task<string> BuildPropertyAddressAsync<T>(IEnumerable<T> properties, CancellationToken cancellationToken = default) where T : IPropertyAddresses
    {
        if (properties is null) return Task.FromResult(string.Empty);

        var sb = new StringBuilder();
        var first = true;

        foreach (var p in properties)
        {
            if (cancellationToken.IsCancellationRequested) break;

            if (first)
            {
                sb.AppendLine(p.FullAddress);
                first = false;
            }
            else
            {
                sb.AppendLine($", {p.FullAddress}");
            }
        }

        return Task.FromResult(sb.ToString());
    }

    public async Task ProcessTestDocumentAsync(byte[] documentBytes, LoanAgreement loan, CancellationToken stoppingToken)
    {
        LoanAgreement loanState = null;

        try
        {
            await loanSemaphoreSlim.WaitAsync();

            docState.LoanList.TryAdd(loan.Id, loan);

            docState.StateHasChanged();

            //var outputBytes = wordServices.ReplaceTokens(doc.DocumentBytes, borrowerValues.ToDictionary(kv => kv.Key, kv => kv.Value ?? string.Empty, StringComparer.Ordinal));

            using var ms = new MemoryStream(documentBytes, writable: true); // wraps the existing buffer

            MemoryStream mergedSteram = await razorLiteService.ProcessAsync(ms, loan);

            //// Borrowers
            //foreach (var borrower in loan.Borrowers)
            //{
            //    var borrowerValues = BorrowerMergeMap.GetValues(borrower);

            //}

            docState.LoanList.Remove(loan.Id, out loanState);

            OnLoanCompletedEvent?.Invoke(this, loan);
        }
        catch (Exception)
        {
            OnLoanErrorEvent?.Invoke(this, loan);
            docState.LoanList.Remove(loan.Id, out loanState);
            throw;
        }
        finally
        {
            loanSemaphoreSlim?.Release();
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);

        await DoSomeInitializationAsync();

        await DoSomeRecoveryAsync();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        await DoSomeCleanupAsync();
    }

    public async Task DoSomeInitializationAsync()
    {
        logger.LogDebug($"Loan Processing Background Service Initialization");
    }

    public async Task DoSomeRecoveryAsync()
    {
        logger.LogDebug($"Loan Processing Background Service Recovering Tranactions");
    }

    public async Task DoSomeCleanupAsync()
    {
        logger.LogDebug($"Loan Processing Background Service Performing Cleanup tasks");
    }


    private void OnDocCompleted(object sender, DocumentMerge e)
    {
        try
        {
            ////Handle Document Merge Completed Event
            ////Check if all documents for the loan are merged
            //var loan = docMerge.LoanAgreement;
            //bool allMerged = true;
            //foreach (var document in loan.DocumentSet.Documents)
            //{
            //    var matchingDocMerge = docState.DocumentProcessingQueue.FirstOrDefault(d => d.LoanAgreement.Id == loan.Id && d.Document.Id == document.Id);
            //    if (matchingDocMerge is not null)
            //    {
            //        if (matchingDocMerge.Status != DocumentMergeState.Status.Completed)
            //        {
            //            allMerged = false;
            //            break;
            //        }
            //    }
            //}
            //if (allMerged)
            //{
            //    //All documents merged for the loan
            //    //Proceed to next steps like zipping, storing, distributing etc.
            //    logger.LogInformation($"All documents merged for Loan ID: {loan.Id}");
        }
        catch (Exception)
        {

            throw;
        }


    }

    private void OnIsRunBackgroundLoanServiceChanged(object? sender, bool e)
    {
        if (docState.IsRunBackgroundLoanApplicationService)
        {
            DoSomeInitializationAsync();

            DoSomeRecoveryAsync();
        }
        else
        {
            DoSomeCleanupAsync();
        }
    }
}