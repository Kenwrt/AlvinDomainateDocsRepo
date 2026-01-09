using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiquidDocsData.Enums;
using LiquidDocsSite.Database;
using LiquidDocsSite.Helpers;
using LiquidDocsSite.State;
using System.Collections.ObjectModel;

namespace LiquidDocsSite.ViewModels;

public partial class GuarantorViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.Guarantor> recordList = new();

    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.Guarantor> myGuarantorList = new();

    [ObservableProperty]
    private LiquidDocsData.Models.Guarantor editingRecord = null;

    [ObservableProperty]
    private LiquidDocsData.Models.Guarantor selectedRecord = null;

    [ObservableProperty]
    private Guid? loanAgreementId = null;

    private string userId;

    private readonly UserSession userSession;
    private IApplicationStateManager appState;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<GuarantorViewModel> logger;

    public GuarantorViewModel(IMongoDatabaseRepo dbApp, ILogger<GuarantorViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;

        userId = userSession.UserId;
    }

    [RelayCommand]
    private async Task InitializePage(List<LiquidDocsData.Models.Guarantor> guarantorList = null)
    {
        if (EditingRecord is null) GetNewRecord();

        RecordList.Clear();

        dbApp.GetRecords<LiquidDocsData.Models.Guarantor>().ToList().ForEach(lf => RecordList.Add(lf));

        if (guarantorList is not null)
        {
            MyGuarantorList.Clear();

            MyGuarantorList = guarantorList.ToObservableCollection();
        }
    }

    [RelayCommand]
    private async Task UpsertRecord()
    {
        if (EditingRecord.EntityType == Entity.Types.Individual && !String.IsNullOrEmpty(EditingRecord.ContactName))
        {
            EditingRecord.EntityName = EditingRecord.ContactName;
        }

        int index = RecordList.FindIndex(x => x.Id == EditingRecord.Id);

        if (index > -1)
        {
            RecordList[index] = EditingRecord;
        }
        else
        {
            RecordList.Add(EditingRecord);
        }

        index = MyGuarantorList.FindIndex(x => x.Id == EditingRecord.Id);

        if (index > -1)
        {
            MyGuarantorList[index] = EditingRecord;
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
        int myGuarantorIndex = RecordList.FindIndex(x => x.Id == r.Id);

        if (myGuarantorIndex > -1)
        {
            RecordList.RemoveAt(myGuarantorIndex);
        }

        dbApp.DeleteRecord<LiquidDocsData.Models.Guarantor>(r);
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