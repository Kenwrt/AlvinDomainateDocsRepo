using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentManager.Services;
using LiquidDocsData.Models;
using LiquidDocsNotify.Enums;
using LiquidDocsNotify.Models;
using LiquidDocsNotify.State;
using LiquidDocsSite.Components.Pages;
using LiquidDocsSite.Database;
using LiquidDocsSite.Helpers;
using LiquidDocsSite.State;
using Org.BouncyCastle.Utilities;
using SharpCompress.Compressors.ADC;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Text;

namespace LiquidDocsSite.ViewModels;

public partial class TestMergeViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.Document> documentList = new();

    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.DocumentSet> documentSetList = new();

    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.LoanAgreement> loanAgreementList = new();

    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.Document> documentMergeList = new();

    [ObservableProperty]
    private EmailMsg editingMailMsg = new();

    [ObservableProperty]
    private bool isZipFile = false;

    [ObservableProperty]
    private LiquidDocsData.Enums.DocumentTypes.TestTypes testType = LiquidDocsData.Enums.DocumentTypes.TestTypes.Document;

    [ObservableProperty]
    private LiquidDocsData.Enums.DocumentTypes.OutputTypes outputType = LiquidDocsData.Enums.DocumentTypes.OutputTypes.Pdf;

    [ObservableProperty]
    private EmailMsg selectedMailMsg = null;

    [ObservableProperty]
    private LiquidDocsData.Models.Document editingDocument = null;

    [ObservableProperty]
    private LiquidDocsData.Models.LoanAgreement loanAgreement = null;

    [ObservableProperty]
    private LiquidDocsData.Models.DocumentSet editingDocumentSet = null;

    [ObservableProperty]
    private LiquidDocsData.Models.Document selectedDocument = null;

    [ObservableProperty]
    private LiquidDocsData.Models.Document selectedDocumentSet = null;

    private string userId;
    private readonly UserSession userSession;
    private IApplicationStateManager appState;
    private readonly IMongoDatabaseRepo dbApp;
    private IRazorLiteService razorLiteService;
    private IWebHostEnvironment env;
    private IWordServices wordServices;
    private readonly ILogger<TestMergeViewModel> logger;

    public INotifyState? NotifyState { get; set; }

    public TestMergeViewModel(IMongoDatabaseRepo dbApp, ILogger<TestMergeViewModel> logger, UserSession userSession, IApplicationStateManager appState, IRazorLiteService razorLiteService, IWebHostEnvironment env, IWordServices wordServices, INotifyState? NotifyState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;
        this.razorLiteService = razorLiteService;
        this.env = env;
        this.wordServices = wordServices;
        this.NotifyState = NotifyState;

        userId = userSession.UserId;
    }

    [RelayCommand]
    private async Task InitializePage()
    {
        GetNewRecord();


        dbApp.GetRecords<LiquidDocsData.Models.LoanAgreement>().ToList().ForEach(la => LoanAgreementList.Add(la));
              
        dbApp.GetRecords<LiquidDocsData.Models.Document>().ToList().ForEach(lf => DocumentList.Add(lf));

        dbApp.GetRecords<LiquidDocsData.Models.DocumentSet>().ToList().ForEach(lf => DocumentSetList.Add(lf));

    }

    [RelayCommand]
    private async Task SendMail(List<EmailAttachment> attachments)
    {
        try
        {
            string ken = "";


            LiquidDocsNotify.Models.EmailMsg email = new EmailMsg()
            {
                To = EditingMailMsg.To,
                ReplyTo = EditingMailMsg.To,
                Subject = EditingMailMsg.Subject,
                PostMarkTemplateId = (int)LiquidDocsNotify.Enums.EmailEnums.Templates.MergeTest,
                TemplateModel = new
                {
                    login_url = "https://liquiddocs.law/account/login",
                    username = EditingMailMsg.Name ?? string.Empty,
                    product_name = "LiquidDocs",
                    support_email = "https://liquiddocs.law/support",
                    help_url = "https://liquiddocs.law/help"
                },

                Attachments = attachments

            };

            NotifyState.EmailMsgProcessingQueue.Enqueue(email);

        }
        catch (System.Exception ex)
        {
            throw;
        }
    }

    [RelayCommand]
    private async Task<List<EmailAttachment>> MergeDocument()
    {
        List<EmailAttachment> attachments = new();

        Dictionary<string, byte[]> files = new();

        string docName = "";


        try
        {
            if (TestType == LiquidDocsData.Enums.DocumentTypes.TestTypes.Document)
            {
                EmailAttachment attach;

                var file = await ProcessDocumentAsync(EditingDocument);
                               
                if (OutputType == LiquidDocsData.Enums.DocumentTypes.OutputTypes.Word)
                {
                    docName = $"{EditingDocument.Name}.docm";
                }
                else
                {
                    docName = $"{EditingDocument.Name}.pdf";
                }
                     
                if (IsZipFile)
                {
                    if (!files.ContainsKey(docName)) files.Add(docName, file.MergedDocumentBytes);

                    var zipStream = CreateZipStream(files);
                    zipStream.Position = 0;

                    attach = new EmailAttachment
                    {
                        FileName = $"{EditingDocument.Name}.zip",
                        ContentType = "application/zip",
                        OutputType = EmailAttachmentEnums.OutputType.ZipFile,

                        SourceType = EmailAttachmentEnums.Type.FileStream,
                        Stream = new MemoryStream(zipStream.ToArray())
                    };
                }
                else
                {
                    if (OutputType == LiquidDocsData.Enums.DocumentTypes.OutputTypes.Word)
                    {
                        attach = new EmailAttachment
                        {
                            FileName = $"{docName}",
                            ContentType = "application/docm",
                            OutputType = EmailAttachmentEnums.OutputType.WordDoc,

                            SourceType = EmailAttachmentEnums.Type.FileStream,
                            Stream = new MemoryStream(file.MergedDocumentBytes)
                        };
                    }
                    else
                    {
                        attach = new EmailAttachment
                        {
                            FileName = $"{docName}",
                            ContentType = "application/pdf",
                            OutputType = EmailAttachmentEnums.OutputType.PDF,
                            SourceType = EmailAttachmentEnums.Type.FileStream,
                            Stream = new MemoryStream(file.MergedDocumentBytes)

                        };
                    }

                }

                attachments.Add(attach);
            }
            else
            {
                foreach (var doc in EditingDocumentSet.Documents)
                {
                    var file = await ProcessDocumentAsync(doc);

                    if (OutputType == LiquidDocsData.Enums.DocumentTypes.OutputTypes.Word)
                    {
                        docName = $"{doc.Name}.docm";
                    }
                    else
                    {
                        docName = $"{doc.Name}.pdf";
                    }


                    if (!files.ContainsKey(docName)) files.Add(docName, file.MergedDocumentBytes);
                     
                }

                if (!IsZipFile)
                {
                    EmailAttachment attach;

                    //Make Attachment for each file
                    foreach (var file in files)
                    {
                        if (OutputType == LiquidDocsData.Enums.DocumentTypes.OutputTypes.Pdf)
                        {

                            attach = new EmailAttachment
                            {
                                FileName = $"{file.Key}",
                                ContentType = "application/pdf",
                                OutputType = EmailAttachmentEnums.OutputType.PDF,
                                SourceType = EmailAttachmentEnums.Type.FileStream,
                                Stream = new MemoryStream(file.Value)

                            };
                        }
                        else
                        {
                            attach = new EmailAttachment
                            {
                                FileName = $"{file.Key}",
                                ContentType = "application/docm",
                                OutputType = EmailAttachmentEnums.OutputType.WordDoc,

                                SourceType = EmailAttachmentEnums.Type.FileStream,
                                Stream = new MemoryStream(file.Value)
                            };
                        }

                        attachments.Add(attach);
                    }

                }
                else
                {
                    //Make Compressed File
                    var zipStream = CreateZipStream(files);
                    zipStream.Position = 0;

                    var attach = new EmailAttachment
                    {
                        FileName = $"{EditingDocumentSet.Name}.zip",
                        ContentType = "application/zip",
                        OutputType = EmailAttachmentEnums.OutputType.ZipFile,

                        SourceType = EmailAttachmentEnums.Type.FileStream,
                        Stream = new MemoryStream(zipStream.ToArray())
                    };

                    attachments.Add(attach);

                }

            }

            SendMail(attachments);
            
        }
        catch (Exception ex)
        {
            logger.LogError($"Error during document merge: {ex.Message}");
            attachments = null;
        }

        return attachments;
    }

    private async Task<LiquidDocsData.Models.Document> ProcessDocumentAsync(LiquidDocsData.Models.Document doc)
    {
        string dir = Path.Combine(env.WebRootPath, "TestMergedDocs");

        Directory.CreateDirectory(dir);

        try
        {
            if (doc.TemplateDocumentBytes is null) return null;

            byte[] mergedDocBytes;

            if (LoanAgreement == null) return null;

            using var ms = new MemoryStream(capacity: doc.TemplateDocumentBytes.Length + 4096); // give it some headroom

            ms.Write(doc.TemplateDocumentBytes, 0, doc.TemplateDocumentBytes.Length);

            ms.Position = 0;

            StringBuilder lenderNames = new StringBuilder();
            StringBuilder borrowerNames = new StringBuilder();
            StringBuilder brokerNames = new StringBuilder();
            StringBuilder propertyAddresses = new StringBuilder();

            LoanAgreement.LenderNames = await BuildLenderNamesAsync(LoanAgreement.Lenders);

            LoanAgreement.BorrowerNames = await BuildPartyNamesAsync<LiquidDocsData.Models.Borrower>(LoanAgreement.Borrowers);

            LoanAgreement.BrokerNames = await BuildPartyNamesAsync<LiquidDocsData.Models.Broker>(LoanAgreement.Brokers);

            LoanAgreement.GuarantorNames = await BuildPartyNamesAsync<LiquidDocsData.Models.Guarantor>(LoanAgreement.Guarantors);

            LoanAgreement.PropertyAddresses = await BuildPropertyAddressAsync<LiquidDocsData.Models.PropertyRecord>(LoanAgreement.Properties);

            LoanAgreement.DocumentTitle = doc.Name;

            //Process the document with RazorLite
            MemoryStream msResult = await razorLiteService.ProcessAsync(ms, LoanAgreement);

            if (msResult is not null)
            {
                //doc.MergedDocumentBytes = wordServices.RemoveBadCompatSetting(msResult.ToArray());
                
                ////Validate Format of Return Byte Array
                //var formatType = wordServices.DetectFormatBySignature(doc.MergedDocumentBytes);

                //logger.LogInformation($"FormatType: {formatType}");

                //using var za = new System.IO.Compression.ZipArchive(new MemoryStream(doc.MergedDocumentBytes), ZipArchiveMode.Read);

                //bool hasContentTypes = za.GetEntry("[Content_Types].xml") != null;

                //logger.LogInformation($"HasContentTypes: {hasContentTypes}");
                //bool hasDocumentXml = za.GetEntry("word/document.xml") != null;

                //logger.LogInformation($"HasDocumentXML: {hasDocumentXml}");

                //var errors = wordServices.ValidateDocx(doc.MergedDocumentBytes);

                //logger.LogInformation($"Validation Errors: {errors.Count()}");

                //wordServices.CheckForDuplicateZipEntries(doc.MergedDocumentBytes);

                //var outPath = @"C:\Temp\processasync-output.docx";
                //File.WriteAllBytes(outPath, doc.MergedDocumentBytes);


                if (OutputType == LiquidDocsData.Enums.DocumentTypes.OutputTypes.Word)
                {
                    //no need to convert already in word format

                    msResult.Position = 0;
                    doc.Name = $"{doc.Name}";

                }
                else
                {
                    MemoryStream pdfStream = await wordServices.ConvertWordToPdfAsync(msResult);

                    pdfStream.Position = 0;

                    doc.MergedDocumentBytes = pdfStream.ToArray();
                    doc.Name = $"{doc.Name}";

                    //Convert to PDF
                    
                    var pdfPath = Path.Combine(dir, $"{DisplayHelper.CapitalizeWordsNoSpaces(doc.Name)}-TestMerge.pdf");
                    await File.WriteAllBytesAsync(pdfPath, pdfStream.ToArray());
                }

            }
            else
            {
                logger.LogError($"RazorLite processing returned null stream. for {doc.Name}");
                return null;
            }
        }
        catch (Exception ex)
        {

            logger.LogError($"Error during document merge: {ex.Message}");
        }

        return doc;
    }



    public static MemoryStream CreateZipStream(Dictionary<string, byte[]> files)
    {
        var zipStream = new MemoryStream();

        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in files)
            {
                var entry = archive.CreateEntry(file.Key, CompressionLevel.Optimal);

                using var entryStream = entry.Open();
                entryStream.Write(file.Value, 0, file.Value.Length);
            }
        }

        zipStream.Position = 0; // critical or your attachment will be empty
        return zipStream;
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

   

    [RelayCommand]
    private void SelectDocumentRecord(LiquidDocsData.Models.Document r)
    {
        if (r != null)
        {
            SelectedDocument = r;
        }
    }

    [RelayCommand]
    private void ClearDocumentSelection()
    {
        SelectedDocument = new();

    }

    [RelayCommand]
    private void GetNewRecord()
    {
        EditingMailMsg = new LiquidDocsNotify.Models.EmailMsg ();

    }
}