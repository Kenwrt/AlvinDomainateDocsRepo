using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiquidDocsData.Enums;
using LiquidDocsSite.Components.Pages;
using LiquidDocsSite.Database;
using LiquidDocsSite.Helpers;
using LiquidDocsSite.State;
using System.Collections.ObjectModel;

namespace LiquidDocsSite.ViewModels;

public partial class AkaViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.AkaName> myRecordList = new();

    [ObservableProperty]
    private LiquidDocsData.Models.AkaName editingRecord = null;

    [ObservableProperty]
    private LiquidDocsData.Models.AkaName selectedRecord = null;
        
    private readonly ILogger<AkaViewModel> logger;


    public AkaViewModel(ILogger<AkaViewModel> logger)
    {
        this.logger = logger;
                   
    }

    [RelayCommand]
    private async Task InitializeLoadPage()
    {
        if (EditingRecord is null)
        {
            GetNewRecord();
        }   
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
    private async Task DeleteRecord(LiquidDocsData.Models.AkaName r)
    {

        int recordListIndex = MyRecordList.FindIndex(x => x.Id == r.Id);

        if (recordListIndex > -1)
        {

            MyRecordList.RemoveAt(recordListIndex);

        }


    }


    [RelayCommand]
    private void SelectRecord(LiquidDocsData.Models.AkaName r)
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
        EditingRecord = new LiquidDocsData.Models.AkaName();
        
    }
}