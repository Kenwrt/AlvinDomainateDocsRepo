using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentManager.MergeMappings;
using DocumentManager.State;
using LiquidDocsData.Enums;
using LiquidDocsData.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenXmlPowerTools;
using RazorLight;
using System.Text;

namespace DocumentManager.Services;

public class DocumentMergeBackgroundService : BackgroundService, IDocumentMergeBackgroundService
{
    private readonly ILogger<DocumentMergeBackgroundService> logger;

    private readonly IOptions<DocumentManagerConfigOptions> options;

    private IWebHostEnvironment webEnv;

    private IRazorLiteService razorLiteService;

    private readonly IDocumentManagerState docState;

    private SemaphoreSlim docSemaphoreSlim = null;

    public event EventHandler<DocumentMerge> OnDocCompletedEvent;

    public event EventHandler<DocumentMerge> OnDocErrorEvent;

    public DocumentMergeBackgroundService(ILogger<DocumentMergeBackgroundService> logger, IOptions<DocumentManagerConfigOptions> options, IDocumentManagerState docState, IWebHostEnvironment webEnv, IRazorLiteService razorLiteService)
    {
        this.logger = logger;
        this.options = options;
        this.docState = docState;
        this.webEnv = webEnv;
        this.razorLiteService = razorLiteService;

        docSemaphoreSlim = new(options.Value.MaxDocumentMergeThreads);

        docState.IsRunBackgroundDocumentMergeServiceChanged += OnIsRunDocumentBackgroundServiceChanged;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (options.Value.IsActive)
                {
                    if (docState.IsRunBackgroundDocumentMergeService)
                    {
                        List<Task> docTasks = new();

                        while (docState.DocumentProcessingQueue.Count > 0 && !stoppingToken.IsCancellationRequested)
                        {
                            logger.LogDebug("Document Processing Queued Item Found Job at: {time} Processong Queue Entry", DateTimeOffset.Now);

                            try
                            {
                                DocumentMerge documentMerge = null;

                                docState.DocumentProcessingQueue.TryDequeue(out documentMerge);

                                if (documentMerge is not null) docTasks.Add(Task.Run(async () => await MergeDocumentAsync(documentMerge, stoppingToken)));
                            }
                            catch (Exception ex)
                            {
                                logger.LogError($"{ex.Message}");
                            }
                        }

                        if (docTasks.Count > 0) await Task.WhenAll(docTasks);

                        if (docState.DocumentProcessingQueue.Count == 0) logger.LogDebug("Document Merge Processing Background Service running at: {time}  Nothing Queued", DateTimeOffset.Now);
                    }
                    else
                    {
                        logger.LogDebug("Document Merge Processing Background Service PAUSED at: {time}", DateTimeOffset.Now);
                    }
                }
                else
                {
                    logger.LogDebug($"Docment Merge Processing Background Service NOT Active");
                }

                docState.IsReadyForProcessing = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }

    public async Task MergeDocumentAsync(DocumentMerge documentMerge, CancellationToken stoppingToken)
    {
        DocumentMerge documentState = null;

        try
        {
            await docSemaphoreSlim.WaitAsync();

            docState.DocumentList.TryAdd(documentMerge.Id, documentMerge);

            byte[] mergedDocBytes;


            using var ms = new MemoryStream(capacity: documentMerge.Document.TemplateDocumentBytes.Length + 4096); // give it some headroom

            ms.Write(documentMerge.Document.TemplateDocumentBytes, 0, documentMerge.Document.TemplateDocumentBytes.Length);

            ms.Position = 0;


            //Process the document with RazorLite
            MemoryStream msResult = await razorLiteService.ProcessAsync(ms, documentMerge.LoanAgreement);

            if (msResult is not null)
            {
                documentMerge.MergedDocumentBytes = msResult.ToArray();

                OnDocCompletedEvent?.Invoke(this, documentMerge);

                //if (File.Exists(targetPath))
                //{
                //    File.Delete(targetPath);
                //}

                //using (var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
                //{
                //    msResult.Position = 0; // rewind, always
                //    msResult.CopyTo(fileStream);
                //}


            }
            else
            {
                documentMerge.MergedDocumentBytes = null;
                documentMerge.Status = DocumentMergeState.Status.Error;

                OnDocErrorEvent?.Invoke(this, documentMerge);
            }



            docState.StateHasChanged();

            // OnDocCompletedEvent?.Invoke(this, document);
        }
        catch (Exception ex)
        {
            //logger.LogError(ex, $"Error processing document {document.Id}: {ex.Message}");

            //OnDocErrorEvent?.Invoke(this, document);
            //docState.DocumentList.TryRemove(document.Id, out documentState);
            throw;
        }
        finally
        {
            docSemaphoreSlim?.Release();
        }
    }




    private static string GetDocumentText(WordprocessingDocument doc)
    {
        using var reader = new StreamReader(doc.MainDocumentPart.GetStream());
        return reader.ReadToEnd();
    }

    private static void SetDocumentText(WordprocessingDocument doc, string text)
    {
        using var writer = new StreamWriter(doc.MainDocumentPart.GetStream(FileMode.Create));
        writer.Write(text);
    }

    private async Task<string> GetHtmlFromWordDoc(string wordFilePath)
    {
        System.Xml.Linq.XElement html = null;
        string htmlString = string.Empty;
        string htmlOutputPath = System.IO.Path.Combine("Content", "LenderClosingInstructions.mht");

        try
        {
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(wordFilePath, false))
            {
                OpenXmlPowerTools.HtmlConverterSettings settings = new OpenXmlPowerTools.HtmlConverterSettings()
                {
                    PageTitle = "Converted from Word"
                };

                html = OpenXmlPowerTools.HtmlConverter.ConvertToHtml(wordDoc, settings);

                // Save the HTML output
                File.WriteAllText(htmlOutputPath, html.ToStringNewLineOnAttributes(), Encoding.UTF8);

                Console.WriteLine($"HTML file generated: {htmlOutputPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during conversion: " + ex.Message);
        }

        return html.ToStringNewLineOnAttributes();
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
        logger.LogDebug($"Document Merge Processing Background Service Initialization");
    }

    public async Task DoSomeRecoveryAsync()
    {
        logger.LogDebug($"Document Merge Processing Background Service Recovering Tranactions");
    }

    public async Task DoSomeCleanupAsync()
    {
        logger.LogDebug($"Document Merge Processing Background Service Performing Cleanup tasks");
    }

    private void OnIsRunDocumentBackgroundServiceChanged(object? sender, bool e)
    {
        if (docState.IsRunBackgroundDocumentMergeService)
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

public class TextReplacement
{
    public string Search { get; set; }
    public string Replace { get; set; }
}