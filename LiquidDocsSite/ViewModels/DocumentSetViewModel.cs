using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentManager;
using DocumentManager.Database;
using LiquidDocsData.Models;
using LiquidDocsSite.Database;
using LiquidDocsSite.State;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;

namespace LiquidDocsSite.ViewModels;

public partial class DocumentSetViewModel : ObservableObject
{
    // [ObservableProperty]
    //  private ObservableCollection<LiquidDocsData.Models.DocumentSet> documentSets = new();

    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.Document> documentsInSet = new();

    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.Document> selectedAssignedDocuments = new();

    //  [ObservableProperty]
    // private LiquidDocsData.Models.DocumentSet? selectedDocumentSet;

    //  [ObservableProperty]
    // private LiquidDocsData.Models.DocumentSet editingDocumentSet = new();

    public string? SessionToken { get; set; }
    private string userId;

    private Session session;

    private readonly IOptions<DocumentManagerConfigOptions> options;
    private readonly IMongoDatabaseRepoDocuments dbDocs;
    private readonly UserSession userSession;
    private readonly IMongoDatabaseRepo dbApp;
    private IApplicationStateManager appState;

    public DocumentSetViewModel(IMongoDatabaseRepo dbApp, UserSession userSession, IApplicationStateManager appState, IOptions<DocumentManagerConfigOptions> options)
    {
        this.dbApp = dbApp;
        this.userSession = userSession;
        this.appState = appState;
        this.options = options;

        userId = userSession.UserId;

        //session = appState.Take<Session>(SessionToken);
        //  dbApp.GetRecords<LiquidDocsData.Models.DocumentSet>().ToList().ForEach(ds => DocumentSets.Add(ds));
    }

    [RelayCommand]
    private async Task GenerateMasterDocumentSet()
    {
        Guid docTestSetId;

        // var docsets = dbApp.GetRecords<LiquidDocsData.Models.DocumentSet>().ToList();

        //if (docsets.Count > 0)
        //{
        //    //Make Test Document set
        //   // dbApp.DeleteRecord<LiquidDocsData.Models.DocumentSet>(dbApp.GetRecords<LiquidDocsData.Models.DocumentSet>().FirstOrDefault(x => x.Name == "Test Document Set"));
        //}

        //  LiquidDocsData.Models.DocumentSet docSet = await GetNewDocumentSetRecordAsync();

        //docSet.Name = "Test Document Set";
        //docSet.Description = "This is a test document set created for demo purposes.";
        //docSet.UserId = Guid.Parse(userId);
        //docTestSetId = docSet.Id;

        //// Assign document to Test Document Set
        //var availableMasterDocuments = dbApp.GetRecords<LiquidDocsData.Models.Document>().Where(x => x.DocSetId == null && x.IsActive == true).ToList();

        //foreach (var colName in options.Value.TestDocumentNames)
        //{
        //    LiquidDocsData.Models.Document doc = dbApp.GetRecords<LiquidDocsData.Models.Document>().FirstOrDefault(x => x.Name == colName && x.IsActive == true);

        //    if (doc is not null)
        //    {
        //       // docSet.Documents.Add(doc);
        //    }
        //}

        // dbApp.UpSertRecord<LiquidDocsData.Models.DocumentSet>(docSet);
    }

    [RelayCommand]
    private async Task AddDocumentSet()
    {
        //  dbApp.UpSertRecord<DocumentSet>(EditingDocumentSet);

        //   DocumentSets.Add(EditingDocumentSet);

        //  EditingDocumentSet = await GetNewDocumentSetRecordAsync();
    }

    [RelayCommand]
    private async Task EditDocumentSet()
    {
        //dbApp.UpSertRecord<LiquidDocsData.Models.DocumentSet>(EditingDocumentSet);

        //var record = DocumentSets.FirstOrDefault(x => x.Id == EditingDocumentSet.Id);

        //if (record != null)
        //{
        //    var index = DocumentSets.IndexOf(record);
        //    DocumentSets[index] = EditingDocumentSet;
        //}

        //   SelectedDocumentSet = null;
    }

    [RelayCommand]
    private async Task DeleteDocumentSet()
    {
        //if (SelectedDocumentSet != null)
        //{
        //    DocumentSets.RemoveWhere(x => x.Id == SelectedDocumentSet.Id);

        //    dbApp.DeleteRecord<DocumentSet>(SelectedDocumentSet);

        //    SelectedDocumentSet = null;

        //    EditingDocumentSet = await GetNewDocumentSetRecordAsync();
        //}
    }

    [RelayCommand]
    private async Task InitializeDocumentSet()
    {
        // EditingDocumentSet = await GetNewDocumentSetRecordAsync();
    }

    //[RelayCommand]
    //private async Task SelectDocumentSet(DocumentSet? ds)
    //{
    //    if (ds != null)
    //    {
    //        SelectedDocumentSet = ds;
    //        EditingDocumentSet = ds;
    //    }
    //}

    //[RelayCommand]
    //private async Task ClearDocumentSetSelection()
    //{
    //    SelectedDocumentSet = null;
    //    EditingDocumentSet = await GetNewDocumentSetRecordAsync();
    //}

    //private async Task<LiquidDocsData.Models.DocumentSet> GetNewDocumentSetRecordAsync()
    //{
    //    EditingDocumentSet = new LiquidDocsData.Models.DocumentSet()
    //    {
    //        UserId = Guid.Parse(userId)
    //    };

    //    return EditingDocumentSet;
    //}
}