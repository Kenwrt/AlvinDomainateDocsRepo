using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiquidDocsData.Enums;
using LiquidDocsSite.Database;
using LiquidDocsSite.Helpers;
using LiquidDocsSite.State;
using System.Collections.ObjectModel;

namespace LiquidDocsSite.ViewModels;

public partial class QuickGuarantorViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.Guarantor> recordList = new();

    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.Guarantor> myGuarantorList = new();

    [ObservableProperty]
    private LiquidDocsData.Models.Guarantor editingRecord = null;

    [ObservableProperty]
    private LiquidDocsData.Models.Guarantor selectedRecord = null;

    private string userId;
    private readonly UserSession userSession;
    private IApplicationStateManager appState;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<QuickGuarantorViewModel> logger;

    public QuickGuarantorViewModel(IMongoDatabaseRepo dbApp, ILogger<QuickGuarantorViewModel> logger, UserSession userSession, IApplicationStateManager appState)
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

        dbApp.GetRecords<LiquidDocsData.Models.Guarantor>().ToList().ForEach(lf => RecordList.Add(lf));
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

        int myGuarantorIndex = MyGuarantorList.FindIndex(x => x.Id == EditingRecord.Id);

        if (myGuarantorIndex > -1)
        {
            MyGuarantorList[myGuarantorIndex] = EditingRecord;
        }
        else
        {
            MyGuarantorList.Add(EditingRecord);
        }

        await dbApp.UpSertRecordAsync<LiquidDocsData.Models.Guarantor>(EditingRecord);
    }

    [RelayCommand]
    private async Task DeleteRecord(LiquidDocsData.Models.Guarantor r)
    {
        int myGuarantorIndex = MyGuarantorList.FindIndex(x => x.Id == r.Id);

        if (myGuarantorIndex > -1)
        {
            MyGuarantorList.RemoveAt(myGuarantorIndex);
        }
    }

    [RelayCommand]
    private void SelectRecord(LiquidDocsData.Models.Guarantor r)
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
        EditingRecord = new LiquidDocsData.Models.Guarantor()
        {
            UserId = Guid.Parse(userId)
        };
    }
}