using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiquidDocsSite.Database;
using System.Collections.ObjectModel;

namespace LiquidDocsSite.ViewModels;

public partial class RegistrationViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.UserProfile> recordList = new();

    [ObservableProperty]
    private LiquidDocsData.Models.UserProfile editingRecord = new();

    [ObservableProperty]
    private LiquidDocsData.Models.UserProfile selectedRecord = null;

    [ObservableProperty]
    private string userId;

    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<RegistrationViewModel> logger;

    public RegistrationViewModel(IMongoDatabaseRepo dbApp, ILogger<RegistrationViewModel> logger)
    {
        this.dbApp = dbApp;
        this.logger = logger;

        //  dbApp.GetRecords<LiquidDocsData.Models.UserProfile>().ToList().ForEach(lf => RecordList.Add(lf));
    }

    [RelayCommand]
    private async Task AddRecord(LiquidDocsData.Models.UserProfile er)
    {
        RecordList.Add(er);

        dbApp.UpSertRecord<LiquidDocsData.Models.UserProfile>(er);

        EditingRecord = new LiquidDocsData.Models.UserProfile();
    }

    [RelayCommand]
    private void EditRecord()
    {
        dbApp.UpSertRecord<LiquidDocsData.Models.UserProfile>(EditingRecord);

        RecordList.Clear();

        dbApp.GetRecords<LiquidDocsData.Models.UserProfile>().ToList().ForEach(r => RecordList.Add(r));

        SelectedRecord = null;
        EditingRecord = new LiquidDocsData.Models.UserProfile();
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
}