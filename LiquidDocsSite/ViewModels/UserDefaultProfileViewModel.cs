using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiquidDocsData.Models.DTOs;
using LiquidDocsSite.Database;
using LiquidDocsSite.Helpers;
using LiquidDocsSite.State;
using Nextended.Core.Extensions;
using System.Collections.ObjectModel;
using System.Globalization;

namespace LiquidDocsSite.ViewModels;

public partial class UserDefaultProfileViewModel : ObservableObject
{
   
    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.DTOs.LoanTypeListDTO>? loanTypes = new();

   
    [ObservableProperty]
    private LiquidDocsData.Models.UserDefaultProfile editingUserDefaultProfile = null;

    [ObservableProperty]
    private LiquidDocsData.Models.UserDefaultProfile selectedProfile = null;

    [ObservableProperty]
    private string role = "Lender";
    
    private string userId;

    private UserSession userSession;
    private IApplicationStateManager appState;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<UserDefaultProfileViewModel> logger;

    private int nextLoanNumber = 0;

    public UserDefaultProfileViewModel(IMongoDatabaseRepo dbApp, ILogger<UserDefaultProfileViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;

        userId = userSession.UserId;

        LoanTypes = new ObservableCollection<LiquidDocsData.Models.DTOs.LoanTypeListDTO>(dbApp.GetRecords<LiquidDocsData.Models.LoanType>().Select(x => new LoanTypeListDTO(x.Id, x.Name, x.Description, x.IconKey)));
    }

    [RelayCommand]
    private async Task InitializePage()
    {
        try
        {
            GetNewRecord();
        
        }
        catch (Exception ex)
        {
            string Error = ex.Message;
        }
    }

    [RelayCommand]
    private async Task UpsertUserDefaultProfile()
    {
       
        await dbApp.UpSertRecordAsync<LiquidDocsData.Models.UserDefaultProfile>(EditingUserDefaultProfile);
    }

    

    [RelayCommand]
    private void SelectProfile(LiquidDocsData.Models.UserDefaultProfile r)
    {
        SelectedProfile = EditingUserDefaultProfile;
    }

    [RelayCommand]
    private void ClearSelection()
    {
        if (SelectedProfile != null)
        {
            SelectedProfile = null;
            GetNewRecord();
        }
    }

   
    [RelayCommand]
    private void GetNewRecord()
    {
        EditingUserDefaultProfile = new LiquidDocsData.Models.UserDefaultProfile()
        {
            UserId = Guid.Parse(userId)
        };
    }

   
}