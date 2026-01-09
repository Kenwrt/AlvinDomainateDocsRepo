using CommunityToolkit.Mvvm.ComponentModel;
using LiquidDocsSite.Database;
using System.Collections.ObjectModel;

namespace LiquidDocsSite.ViewModels;

public partial class DocumentSetAssignmentViewModel : ObservableObject
{
    [ObservableProperty]
    private HashSet<LiquidDocsData.Models.Document> selectedAssignedDocuments = new();

    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.Document> documents = new();

    [ObservableProperty]
    private LiquidDocsData.Models.Document? selectedDocument;

    //[ObservableProperty]
    //private LiquidDocsData.Models.DocumentSet? selectedDocumentSet;

    private DocumentSetViewModel dsVm;
    private UserSession userSession;
    private readonly IMongoDatabaseRepo dbApp;

    public DocumentSetAssignmentViewModel(IMongoDatabaseRepo dbApp, UserSession userSession, DocumentSetViewModel dsVm)
    {
        this.userSession = userSession;
        this.dsVm = dsVm;
        this.dbApp = dbApp;

        //SelectedDocumentSet = dsVm.SelectedDocumentSet;

        dbApp.GetRecords<LiquidDocsData.Models.Document>().ToList().ForEach(d => Documents.Add(d));
    }
}