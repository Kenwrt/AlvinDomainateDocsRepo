using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiquidDocsData.Enums;
using LiquidDocsSite.Database;
using LiquidDocsSite.Helpers;
using LiquidDocsSite.State;
using System.Collections.ObjectModel;

namespace LiquidDocsSite.ViewModels;

public partial class QuickLenderViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.Lender> recordList = new();

    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.Lender> myLenderList = new();

    [ObservableProperty]
    private LiquidDocsData.Models.Lender editingRecord = null;

    [ObservableProperty]
    private LiquidDocsData.Models.Lender selectedRecord = null;

    private string userId;
    private readonly UserSession userSession;
    private IApplicationStateManager appState;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<QuickLenderViewModel> logger;

    public QuickLenderViewModel(IMongoDatabaseRepo dbApp, ILogger<QuickLenderViewModel> logger, UserSession userSession, IApplicationStateManager appState)
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

        dbApp.GetRecords<LiquidDocsData.Models.Lender>().ToList().ForEach(lf => RecordList.Add(lf));
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

        int myLenderIndex = MyLenderList.FindIndex(x => x.Id == EditingRecord.Id);

        if (myLenderIndex > -1)
        {
            MyLenderList[myLenderIndex] = EditingRecord;
        }
        else
        {
            MyLenderList.Add(EditingRecord);
        }

        await dbApp.UpSertRecordAsync<LiquidDocsData.Models.Lender>(EditingRecord);
    }

    [RelayCommand]
    private async Task DeleteRecord(LiquidDocsData.Models.Lender r)
    {
        int myLenderIndex = MyLenderList.FindIndex(x => x.Id == r.Id);

        if (myLenderIndex > -1)
        {
            MyLenderList.RemoveAt(myLenderIndex);
        }
    }

    [RelayCommand]
    private void SelectRecord(LiquidDocsData.Models.Lender r)
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
        EditingRecord = new LiquidDocsData.Models.Lender()
        {
            UserId = Guid.Parse(userId)
        };
    }
}