using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiquidDocsData.Enums;
using LiquidDocsSite.Components.Pages;
using LiquidDocsSite.Database;
using LiquidDocsSite.Helpers;
using LiquidDocsSite.State;
using System.Collections.ObjectModel;

namespace LiquidDocsSite.ViewModels;

public partial class LenderViewModel : ObservableObject
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

    [ObservableProperty]
    private Guid? loanAgreementId = null;

    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<LenderViewModel> logger;
    private IApplicationStateManager appState;
   

    private readonly UserSession userSession;

    public LenderViewModel(IMongoDatabaseRepo dbApp, ILogger<LenderViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;

        this.userSession = userSession;
        this.appState = appState;

        userId = userSession.UserId;

        
    }

    [RelayCommand]
    private async Task InitializePage(List<LiquidDocsData.Models.Lender> lenderList = null)
    {
        if (EditingRecord is null) GetNewRecord();

        RecordList.Clear();

        dbApp.GetRecords<LiquidDocsData.Models.Lender>().ToList().ForEach(lf => RecordList.Add(lf));

        if (lenderList is not null)
        {
            MyLenderList.Clear();
            MyLenderList = lenderList.ToObservableCollection();
        }
    }

    
    [RelayCommand]
    private async Task UpsertRecord()
    {
        if (EditingRecord.EntityType == Entity.Types.Individual && !string.IsNullOrEmpty(EditingRecord.ContactName))
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

        index = MyLenderList.FindIndex(x => x.Id == EditingRecord.Id);

        if (index > -1)
        {

            MyLenderList[index] = EditingRecord;

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

        int lenderIndex = RecordList.FindIndex(x => x.Id == r.Id);

        if (lenderIndex > -1)
        {

            RecordList.RemoveAt(lenderIndex);

        }

        dbApp.DeleteRecord<LiquidDocsData.Models.Lender>(r);

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