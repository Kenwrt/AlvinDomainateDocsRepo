using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiquidDocsSite.Database;
using LiquidDocsSite.Helpers;
using LiquidDocsSite.State;
using System.Collections.ObjectModel;

namespace LiquidDocsSite.ViewModels;

public partial class PropertyViewModel : ObservableObject
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
    private IApplicationStateManager appState;

    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<PropertyViewModel> logger;

    public PropertyViewModel(IMongoDatabaseRepo dbApp, ILogger<PropertyViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;

        userId = userSession.UserId;
    }

    [RelayCommand]
    private async Task InitializePage(List<LiquidDocsData.Models.PropertyRecord> propertyList)
    {
        if (EditingRecord is null) GetNewRecord();

        RecordList.Clear();

        dbApp.GetRecords<LiquidDocsData.Models.PropertyRecord>().Where(x => x.UserId == Guid.Parse(userId)).ToList().ForEach(lf => RecordList.Add(lf));
    }

    [RelayCommand]
    private async Task UpsertRecord()
    {
        int index = RecordList.FindIndex(x => x.Id == EditingRecord.Id);

        if (index > -1)
        {
            RecordList[index] = EditingRecord;
        }
        else
        {
            RecordList.Add(EditingRecord);
        }

        index = MyPropertyList.FindIndex(x => x.Id == EditingRecord.Id);

        if (index > -1)
        {
            MyPropertyList[index] = EditingRecord;
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
        int myPropertyIndex = RecordList.FindIndex(x => x.Id == r.Id);

        if (myPropertyIndex > -1)
        {
            RecordList.RemoveAt(myPropertyIndex);
        }

        dbApp.DeleteRecord<LiquidDocsData.Models.PropertyRecord>(r);
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
        EditingRecord = new LiquidDocsData.Models.PropertyRecord();
    }
}