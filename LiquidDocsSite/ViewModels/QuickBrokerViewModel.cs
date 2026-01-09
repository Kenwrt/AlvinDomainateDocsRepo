using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiquidDocsData.Enums;
using LiquidDocsSite.Database;
using LiquidDocsSite.Helpers;
using LiquidDocsSite.State;
using System.Collections.ObjectModel;

namespace LiquidDocsSite.ViewModels;

public partial class QuickBrokerViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.Broker> recordList = new();

    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.Broker> myBrokerList = new();

    [ObservableProperty]
    private LiquidDocsData.Models.Broker editingRecord = null;

    [ObservableProperty]
    private LiquidDocsData.Models.Broker selectedRecord = null;

    private string userId;
    private readonly UserSession userSession;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<BrokerViewModel> logger;
    private IApplicationStateManager appState;

    public QuickBrokerViewModel(IMongoDatabaseRepo dbApp, ILogger<BrokerViewModel> logger, UserSession userSession, IApplicationStateManager appState)
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

        dbApp.GetRecords<LiquidDocsData.Models.Broker>().ToList().ForEach(lf => RecordList.Add(lf));
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

        int myBrokerIndex = MyBrokerList.FindIndex(x => x.Id == EditingRecord.Id);

        if (myBrokerIndex > -1)
        {
            MyBrokerList[myBrokerIndex] = EditingRecord;
        }
        else
        {
            MyBrokerList.Add(EditingRecord);
        }

        await dbApp.UpSertRecordAsync<LiquidDocsData.Models.Broker>(EditingRecord);
    }

    [RelayCommand]
    private async Task DeleteRecord(LiquidDocsData.Models.Broker r)
    {
        int myBrokerIndex = MyBrokerList.FindIndex(x => x.Id == r.Id);

        if (myBrokerIndex > -1)
        {
            MyBrokerList.RemoveAt(myBrokerIndex);
        }
    }

    [RelayCommand]
    private void SelectRecord(LiquidDocsData.Models.Broker r)
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
        EditingRecord = new LiquidDocsData.Models.Broker()
        {
            UserId = Guid.Parse(userId)
        };
    }
}