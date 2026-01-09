using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiquidDocsSite.Database;
using LiquidDocsSite.Helpers;
using LiquidDocsSite.State;
using System.Collections.ObjectModel;

namespace LiquidDocsSite.ViewModels;

public partial class QuickPropertyViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.PropertyRecord> recordList = new();

    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.PropertyRecord> myPropertyList = new();

    [ObservableProperty]
    private LiquidDocsData.Models.PropertyRecord editingRecord = null;

    [ObservableProperty]
    private LiquidDocsData.Models.PropertyRecord selectedRecord = null;

    private string userId;
    private readonly UserSession userSession;
    private readonly IMongoDatabaseRepo dbApp;
    private IApplicationStateManager appState;
    private readonly ILogger<PropertyViewModel> logger;

    public QuickPropertyViewModel(IMongoDatabaseRepo dbApp, ILogger<PropertyViewModel> logger, UserSession userSession, IApplicationStateManager appState)
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

        SelectedRecord = null;

        RecordList.Clear();

        dbApp.GetRecords<LiquidDocsData.Models.PropertyRecord>().Where(x => x.UserId == Guid.Parse(userId)).ToList().ForEach(lf => RecordList.Add(lf));
    }

    [RelayCommand]
    private async Task UpsertRecord()
    {
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

        int myPropertyIndex = MyPropertyList.FindIndex(x => x.Id == EditingRecord.Id);

        if (myPropertyIndex > -1)
        {
            MyPropertyList[myPropertyIndex] = EditingRecord;
        }
        else
        {
            MyPropertyList.Add(EditingRecord);
        }

        await dbApp.UpSertRecordAsync<LiquidDocsData.Models.PropertyRecord>(EditingRecord);
    }

    [RelayCommand]
    private async Task DeleteRecord(LiquidDocsData.Models.PropertyRecord r)
    {
        int myPropertyIndex = MyPropertyList.FindIndex(x => x.Id == r.Id);

        if (myPropertyIndex > -1)
        {
            MyPropertyList.RemoveAt(myPropertyIndex);
        }
    }

    [RelayCommand]
    private void SelectRecord(LiquidDocsData.Models.PropertyRecord r)
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
        EditingRecord = new LiquidDocsData.Models.PropertyRecord()
        {
            UserId = Guid.Parse(userId)
        };
    }
}