using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentManager.Services;
using LiquidDocsData.Models;
using LiquidDocsSite.Database;
using LiquidDocsSite.Helpers;
using LiquidDocsSite.State;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Windows.Forms;

namespace LiquidDocsSite.ViewModels;

public partial class DocumentViewModel : ObservableObject
{
    private IWordServices wordServices;

    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.Document> recordList = new();

    [ObservableProperty]
    private LiquidDocsData.Models.Document? selectedRecord;

    [ObservableProperty]
    private LiquidDocsData.Models.Document editingRecord;

    [ObservableProperty]
    private LiquidDocsData.Models.DocumentLibrary? selectedLibrary;

    [ObservableProperty]
    private LiquidDocsData.Models.DocumentSet? selectedDocumentSet;

    public string? SessionToken { get; set; }

    private string userId;

    private readonly IMongoDatabaseRepo dbApp;
    private DocumentLibraryViewModel dlVm;
    private DocumentSetViewModel dsVm;
    private UserSession userSession;

    private Session session;
    private string downloadFileName = string.Empty;
    private readonly HttpClient httpClient = new HttpClient();
    private IApplicationStateManager appState;
    private IWebHostEnvironment webEnv;

    public DocumentViewModel(IMongoDatabaseRepo dbApp, IWordServices wordServices, UserSession userSession, IApplicationStateManager appState, DocumentLibraryViewModel dlVm, DocumentSetViewModel dsVm, IWebHostEnvironment webEnv)
    {
        this.dsVm = dsVm;
        this.dlVm = dlVm;
        this.wordServices = wordServices;
        this.userSession = userSession;
        this.appState = appState;
        this.webEnv = webEnv;

        SelectedLibrary = dlVm.SelectedLibrary;
        SelectedDocumentSet = dsVm.SelectedDocumentSet;

        userId = userSession.UserId;

        //session = appState.Take<Session>(SessionToken);

        if (SelectedDocumentSet is not null)
        {
            dbApp.GetRecords<LiquidDocsData.Models.Document>().Where(doc => doc.DocSetId == SelectedDocumentSet.Id).ToList().ForEach(doc => RecordList.Add(doc));
        }
        else
        {
            dbApp.GetRecords<LiquidDocsData.Models.Document>().Where(doc => doc.DocLibId == SelectedLibrary.Id).ToList().ForEach(doc => RecordList.Add(doc));
        }

        userId = userSession.UserId;

        this.dbApp = dbApp;

        this.wordServices = wordServices;
        
    }

    [RelayCommand]
    private async Task AddRecord()
    {
        string path;

        byte[] masterTemplateBytes = SelectedLibrary.MasterTemplateBytes;

        string fileName = $"{EditingRecord.Name}--{System.DateTime.UtcNow.ToString("MM-dd-yyyy-HH-MM")}.docm";

        using var ms = new MemoryStream(masterTemplateBytes);

        DocumentTag documentTag = new DocumentTag
        {
            DocumentId = EditingRecord.Id,
            LibraryId = SelectedLibrary.Id,
            DocumentName = fileName,
            //DocumentCollectionId = EditingRecord.DocumentSetId,
            BaseTemplateId = SelectedLibrary.MasterTemplate,
            DocumentStoreId = Guid.Empty
        };

        var editedMs = await wordServices.InsertHiddenTagAsync(ms, "LiquidDocsTag", JsonConvert.SerializeObject(documentTag, Formatting.Indented));

        editedMs.Position = 0; // Reset the stream position to the beginning

        var fileBytes = editedMs.ToArray();

        if (SelectedDocumentSet is not null)
        {
            path = Path.Combine(webEnv.WebRootPath, @$"UploadedTemplates\{SelectedLibrary.Name}\{SelectedDocumentSet.Name}");
        }
        else
        {
            path = Path.Combine(webEnv.WebRootPath, @$"UploadedTemplates\{SelectedLibrary.Name}");
        }

        Directory.CreateDirectory(path);

        var filePathAndName = Path.Combine(path, fileName);

        System.IO.File.WriteAllBytesAsync(filePathAndName, fileBytes).Wait();

        // EditingRecord.UpdatedAt = DateTime.UtcNow;
        EditingRecord.TemplateDocumentBytes = fileBytes;
        EditingRecord.HiddenTagValue = JsonConvert.SerializeObject(documentTag, Formatting.Indented);

        RecordList.Add(EditingRecord); // Add to the local collection

        dbApp.UpSertRecord<LiquidDocsData.Models.Document>((LiquidDocsData.Models.Document)EditingRecord);

        if (SelectedDocumentSet is not null)
        {
            //Add it to the DocumentSet's collection
            SelectedDocumentSet.Documents.Add(EditingRecord);

            dbApp.UpSertRecord<DocumentSet>(SelectedDocumentSet); // Update the DocumentSet in the database
        }
        else
        {
            if (!SelectedLibrary.Documents.Any(x => x.Name == EditingRecord.Name))
            {
                SelectedLibrary.Documents.Add(EditingRecord);
            }

            dbApp.UpSertRecord<LiquidDocsData.Models.DocumentLibrary>(SelectedLibrary); // Update the DocumentTemplate in the database
        }

        EditingRecord = await GetNewRecordAsync();

        SelectedRecord = null;
    }

    [RelayCommand]
    private async Task EditRecord()
    {
        if (SelectedRecord != null)
        {
            var index = RecordList.IndexOf(SelectedRecord);
            if (index >= 0)
            {
                RecordList[index] = EditingRecord;
                dbApp.UpSertRecord<LiquidDocsData.Models.Document>((LiquidDocsData.Models.Document)EditingRecord);

                //Edit in the Document in Library
                DocumentLibrary docLib = dbApp.GetRecords<LiquidDocsData.Models.DocumentLibrary>().FirstOrDefault(lib => lib.Id == SelectedRecord.DocLibId);

                if (docLib is not null)
                {
                    var libDocIndex = docLib.Documents.FindIndex(d => d.Id == EditingRecord.Id);

                    if (libDocIndex >= 0)
                    {
                        docLib.Documents[libDocIndex] = EditingRecord;
                        dbApp.UpSertRecord<LiquidDocsData.Models.DocumentLibrary>(docLib);
                    }
                }

                //Edit in the Document in DocumentSet

                DocumentSet docSet = dbApp.GetRecords<LiquidDocsData.Models.DocumentSet>().FirstOrDefault(lib => lib.Id == SelectedRecord.DocSetId);

                if (docSet is not null)
                {
                    var docSetDocIndex = SelectedDocumentSet.Documents.FindIndex(d => d.Id == EditingRecord.Id);
                    
                    if (docSetDocIndex >= 0)
                    {
                        SelectedDocumentSet.Documents[docSetDocIndex] = EditingRecord;
                        dbApp.UpSertRecord<LiquidDocsData.Models.DocumentSet>(SelectedDocumentSet);
                    }

                }   

                SelectedRecord = null;

                EditingRecord = await GetNewRecordAsync();
            }
        }
    }

    [RelayCommand]
    private async Task InitializeRecord()
    {
        EditingRecord = await GetNewRecordAsync();
    }

    [RelayCommand]
    private async Task DeleteRecord(LiquidDocsData.Models.Document doc)
    {
        if (doc != null)
        {
            RecordList.RemoveWhere(x => x.Id == doc.Id);

            dbApp.DeleteRecord<LiquidDocsData.Models.Document>((LiquidDocsData.Models.Document)doc);

            SelectedLibrary.Documents.RemoveAll(x => x.Id == doc.Id);

            if (SelectedDocumentSet is not null && SelectedDocumentSet.Documents.Any(x => x.Id == doc.Id)) SelectedDocumentSet.Documents.RemoveAll(x => x.Id == doc.Id);

            SelectedRecord = null;

            EditingRecord = await GetNewRecordAsync();
        }
    }

    [RelayCommand]
    private async Task SelectRecord(LiquidDocsData.Models.Document? doc)
    {
        if (doc != null)
        {
            SelectedRecord = doc;
            EditingRecord = doc;
        }
    }

    [RelayCommand]
    private async Task ClearEditing()
    {
        SelectedRecord = null;
        EditingRecord = await GetNewRecordAsync();
    }

    private async Task<LiquidDocsData.Models.Document> GetNewRecordAsync()
    {
        EditingRecord = new LiquidDocsData.Models.Document()
        {
            UserId = Guid.Parse(userId),
            DocLibId = SelectedLibrary?.Id ?? null,
            DocSetId = SelectedDocumentSet?.Id ?? null,
        };

        return EditingRecord;
    }
}