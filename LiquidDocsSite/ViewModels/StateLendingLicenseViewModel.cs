using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiquidDocsSite.Helpers;
using System.Collections.ObjectModel;

namespace LiquidDocsSite.ViewModels;

public partial class StateLendingLicenseViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.StateLendingLicense> myRecordList = new();

    [ObservableProperty]
    private LiquidDocsData.Models.StateLendingLicense editingRecord = null;

    [ObservableProperty]
    private LiquidDocsData.Models.StateLendingLicense selectedRecord = null;

    private readonly ILogger<StateLendingLicenseViewModel> logger;

    public StateLendingLicenseViewModel(ILogger<StateLendingLicenseViewModel> logger)
    {
        this.logger = logger;
    }

    [RelayCommand]
    private async Task InitializeLoadPage(List<LiquidDocsData.Models.StateLendingLicense> stateLicList = null)
    {
        if (EditingRecord is null)
        {
            GetNewRecord();
        }

        if (stateLicList is not null) MyRecordList = new ObservableCollection<LiquidDocsData.Models.StateLendingLicense>(stateLicList);
    }

    [RelayCommand]
    private async Task UpsertRecord()
    {
        //Update All Collections

        int recordListIndex = MyRecordList.FindIndex(x => x.Id == EditingRecord.Id);

        if (recordListIndex > -1)
        {
            MyRecordList[recordListIndex] = EditingRecord;
        }
        else
        {
            MyRecordList.Add(EditingRecord);
        }
    }

    [RelayCommand]
    private async Task DeleteRecord(LiquidDocsData.Models.StateLendingLicense r)
    {
        int recordListIndex = MyRecordList.FindIndex(x => x.Id == r.Id);

        if (recordListIndex > -1)
        {
            MyRecordList.RemoveAt(recordListIndex);
        }
    }

    [RelayCommand]
    private void SelectRecord(LiquidDocsData.Models.StateLendingLicense r)
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
        EditingRecord = new LiquidDocsData.Models.StateLendingLicense();
    }
}