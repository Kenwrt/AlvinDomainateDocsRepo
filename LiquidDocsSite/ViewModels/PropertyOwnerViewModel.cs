using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiquidDocsData.Enums;
using LiquidDocsSite.Database;
using LiquidDocsSite.Helpers;
using LiquidDocsSite.State;
using System.Collections.ObjectModel;

namespace LiquidDocsSite.ViewModels;

public partial class PropertyOwnerViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.PropertyOwner> recordList = new();

    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.PropertyOwner> myOwnerList = new();

    [ObservableProperty]
    private LiquidDocsData.Models.PropertyOwner editingRecord = null;

    [ObservableProperty]
    private LiquidDocsData.Models.PropertyOwner selectedRecord = null;

    private string userId;
    private readonly UserSession userSession;
    private IApplicationStateManager appState;

    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<PropertyOwnerViewModel> logger;

    public PropertyOwnerViewModel(IMongoDatabaseRepo dbApp, ILogger<PropertyOwnerViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;

        userId = userSession.UserId;
    }

    [RelayCommand]
    private async Task InitializePage(List<LiquidDocsData.Models.PropertyOwner> ownerList = null)
    {
        if (EditingRecord is null) GetNewRecord();

        RecordList.Clear();

        dbApp.GetRecords<LiquidDocsData.Models.PropertyOwner>().ToList().ForEach(lf => RecordList.Add(lf));

        if (ownerList is not null)
        {
            MyOwnerList.Clear();
            MyOwnerList = ownerList.ToObservableCollection();
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

        index = MyOwnerList.FindIndex(x => x.Id == EditingRecord.Id);

        if (index > -1)
        {
            MyOwnerList[index] = EditingRecord;
        }
        else
        {
            MyOwnerList.Add(EditingRecord);
        }

        await dbApp.UpSertRecordAsync<LiquidDocsData.Models.PropertyOwner>(EditingRecord);
    }

    [RelayCommand]
    private async Task DeleteRecord(LiquidDocsData.Models.PropertyOwner r)
    {
        int myOwnerIndex = MyOwnerList.FindIndex(x => x.Id == r.Id);

        if (myOwnerIndex > -1)
        {
            MyOwnerList.RemoveAt(myOwnerIndex);
        }

        dbApp.DeleteRecord<LiquidDocsData.Models.PropertyOwner>(r);
    }

    [RelayCommand]
    private void SelectRecord(LiquidDocsData.Models.PropertyOwner r)
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
        EditingRecord = new LiquidDocsData.Models.PropertyOwner()
        {
            UserId = Guid.Parse(userId)
        };
    }
}