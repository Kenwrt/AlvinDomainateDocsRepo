using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiquidDocsData.Enums;
using LiquidDocsSite.Database;
using LiquidDocsSite.Helpers;
using LiquidDocsSite.State;
using System.Collections.ObjectModel;

namespace LiquidDocsSite.ViewModels;

public partial class QuickBorrowerViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.Borrower> recordList = new();

    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.Borrower> myBorrowerList = new();

    [ObservableProperty]
    private LiquidDocsData.Models.Borrower editingRecord = null;

    [ObservableProperty]
    private LiquidDocsData.Models.Borrower selectedRecord = null;

    private string userId;
    private readonly UserSession userSession;
    private IApplicationStateManager appState;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<QuickBorrowerViewModel> logger;

    public QuickBorrowerViewModel(IMongoDatabaseRepo dbApp, ILogger<QuickBorrowerViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;

        userId = userSession.UserId;
    }

    [RelayCommand]
    private async Task InitializePage()
    {
        if (EditingRecord is null) GetNewRecord();

        RecordList.Clear();

        dbApp.GetRecords<LiquidDocsData.Models.Borrower>().ToList().ForEach(lf => RecordList.Add(lf));
    }

    [RelayCommand]
    private async Task UpsertRecord()
    {
        if (EditingRecord.EntityType == Entity.Types.Individual && !String.IsNullOrEmpty(EditingRecord.ContactName))
        {
            EditingRecord.EntityName = EditingRecord.ContactName;
        }

        //Update All Collections

        int recordListIndex = RecordList.FindIndex(x => x.Id == EditingRecord.Id);

        if (recordListIndex > -1)
        {
            RecordList[recordListIndex] = EditingRecord;
        }
        else
        {
            RecordList.Add(EditingRecord);
        }

        int myBorrowerIndex = MyBorrowerList.FindIndex(x => x.Id == EditingRecord.Id);

        if (myBorrowerIndex > -1)
        {
            MyBorrowerList[myBorrowerIndex] = EditingRecord;
        }
        else
        {
            MyBorrowerList.Add(EditingRecord);
        }

        await dbApp.UpSertRecordAsync<LiquidDocsData.Models.Borrower>(EditingRecord);
    }

    [RelayCommand]
    private async Task DeleteRecord(LiquidDocsData.Models.Borrower r)
    {
        int myBorrowerIndex = MyBorrowerList.FindIndex(x => x.Id == r.Id);

        if (myBorrowerIndex > -1)
        {
            MyBorrowerList.RemoveAt(myBorrowerIndex);
        }
    }

    [RelayCommand]
    private void SelectRecord(LiquidDocsData.Models.Borrower r)
    {
        if (r != null)
        {
            SelectedRecord = r;
            EditingRecord = r;
        }
    }

    [RelayCommand]
    private void ClearSelection()
    {
        if (SelectedRecord != null)
        {
            SelectedRecord = null;
            GetNewRecord();
        }
    }

    [RelayCommand]
    private void GetNewRecord()
    {
        EditingRecord = new LiquidDocsData.Models.Borrower()
        {
            UserId = Guid.Parse(userId)
        };
    }
}