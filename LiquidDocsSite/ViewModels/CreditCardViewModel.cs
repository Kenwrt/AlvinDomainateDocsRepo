using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiquidDocsSite.Database;
using LiquidDocsSite.State;
using System.Collections.ObjectModel;

namespace LiquidDocsSite.ViewModels;

public partial class CreditCardViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.UserProfile> recordList = new();

    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.CreditCard> recordCCList = new();

    [ObservableProperty]
    private LiquidDocsData.Models.UserProfile? editingRecord = null;

    [ObservableProperty]
    private LiquidDocsData.Models.UserProfile selectedRecord = null;

    [ObservableProperty]
    private LiquidDocsData.Models.CreditCard selectedCCRecord = null;

    [ObservableProperty]
    private LiquidDocsData.Models.CreditCard editingCCRecord = null;

    private string userId;

    private readonly IMongoDatabaseRepo dbApp;
    private readonly UserSession userSession;
    private IApplicationStateManager appState;
    private readonly ILogger<CreditCardViewModel> logger;

    public CreditCardViewModel(IMongoDatabaseRepo dbApp, ILogger<CreditCardViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;

        userId = userSession.UserId;

        if (appState.IsUseFakeData)
        {
            //No Fake data at this time
        }
        else
        {
            dbApp.GetRecords<LiquidDocsData.Models.UserProfile>().Where(x => x.UserId == Guid.Parse(userId)).ToList().ForEach(lf => RecordList.Add(lf));
        }
    }

    [RelayCommand]
    private async Task AddRecord()
    {
        RecordList.Add(EditingRecord);

        dbApp.UpSertRecord<LiquidDocsData.Models.UserProfile>(EditingRecord);

        EditingRecord = new LiquidDocsData.Models.UserProfile();
    }

    [RelayCommand]
    private async Task LoadRecord(string userName)
    {
        EditingRecord = await dbApp.GetRecordByUserNameAsync<LiquidDocsData.Models.UserProfile>(userName);

        if (EditingRecord is null) EditingRecord = new LiquidDocsData.Models.UserProfile();
    }

    [RelayCommand]
    private void EditRecord()
    {
        dbApp.UpSertRecord<LiquidDocsData.Models.UserProfile>(EditingRecord);

        var record = RecordList.FirstOrDefault(x => x.Id == EditingRecord.Id);

        if (record != null)
        {
            var index = RecordList.IndexOf(record);
            RecordList[index] = EditingRecord;
        }

        SelectedRecord = null;
    }

    [RelayCommand]
    private void DeleteRecord()
    {
        if (SelectedRecord != null)
        {
            RecordList.Remove(SelectedRecord);

            dbApp.DeleteRecord<LiquidDocsData.Models.UserProfile>(SelectedRecord);

            SelectedRecord = null;
            EditingRecord = new LiquidDocsData.Models.UserProfile();
        }
    }

    [RelayCommand]
    private void SelectRecord(LiquidDocsData.Models.UserProfile r)
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
            EditingRecord = new LiquidDocsData.Models.UserProfile();
        }
    }

    [RelayCommand]
    private void ClearCCSelection()
    {
        if (SelectedCCRecord != null)
        {
            SelectedCCRecord = null;
            EditingCCRecord = new LiquidDocsData.Models.CreditCard();
        }
    }

    [RelayCommand]
    private async Task AddCCRecord()
    {
        RecordCCList.Add(EditingCCRecord);

        dbApp.UpSertRecord<LiquidDocsData.Models.CreditCard>(EditingCCRecord);

        EditingCCRecord = new LiquidDocsData.Models.CreditCard();
    }

    [RelayCommand]
    private void SelectCCRecord(LiquidDocsData.Models.CreditCard r)
    {
        if (r != null)
        {
            SelectedCCRecord = r;

            if (EditingRecord.CreditCardSubscriotion.CreditCard is null)
            {
                EditingRecord.CreditCardSubscriotion.CreditCard = new LiquidDocsData.Models.CreditCard();
            }

            EditingRecord.CreditCardSubscriotion.CreditCard = r;
        }
    }

    [RelayCommand]
    private void DeleteCCRecord()
    {
        if (SelectedCCRecord != null)
        {
            RecordCCList.Remove(SelectedCCRecord);

            dbApp.DeleteRecord<LiquidDocsData.Models.CreditCard>(SelectedCCRecord);

            SelectedCCRecord = null;
            EditingCCRecord = new LiquidDocsData.Models.CreditCard();
        }
    }
}