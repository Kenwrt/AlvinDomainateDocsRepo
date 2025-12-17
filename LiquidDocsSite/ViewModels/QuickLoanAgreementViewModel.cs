using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using LiquidDocsData.Enums;
using LiquidDocsSite.Database;
using LiquidDocsSite.Helpers;
using LiquidDocsSite.State;
using Nextended.Core.Extensions;
using System.Collections.ObjectModel;
using System.Globalization;

namespace LiquidDocsSite.ViewModels;

public partial class QuickLoanAgreementViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.LoanAgreement>? agreementList = new();

    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.DocumentSet>? documentSets = new();

    [ObservableProperty]
    private LiquidDocsData.Models.LoanAgreement editingAgreement = null;

    [ObservableProperty]
    private LiquidDocsData.Models.LoanAgreement selectedAgreement = null;

    private string userId;

    private UserSession userSession;
    private IApplicationStateManager appState;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<LoanAgreementViewModel> logger;

    private int nextLoanNumber = 0;

    public QuickLoanAgreementViewModel(IMongoDatabaseRepo dbApp, ILogger<LoanAgreementViewModel> logger, UserSession userSession, IApplicationStateManager appState)
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
        try
        {
            GetNewRecord();

            AgreementList.Clear();

            AgreementList = new ObservableCollection<LiquidDocsData.Models.LoanAgreement>(dbApp.GetRecords<LiquidDocsData.Models.LoanAgreement>().Where(x => x.UserId == Guid.Parse(userId)));

            if (AgreementList.Count > 0)
            {
                nextLoanNumber = AgreementList.Max(x => Convert.ToInt32(x.LoanNumber.Substring(8))); //"LN-2024-0";
            }


            DocumentSets.Clear();

            if (userSession.UserRole == UserEnums.Roles.Admin.ToString())
            {
                DocumentSets = new ObservableCollection<LiquidDocsData.Models.DocumentSet>(dbApp.GetRecords<LiquidDocsData.Models.DocumentSet>());
            }
            else
            {
                DocumentSets = new ObservableCollection<LiquidDocsData.Models.DocumentSet>(dbApp.GetRecords<LiquidDocsData.Models.DocumentSet>().Where(x => x.UserId == Guid.Parse(userId)));
            }

            await GenerateNewLoanNumberAsync();
        }
        catch (Exception ex)
        {
            string Error = ex.Message;
        }
    }

   

   
    [RelayCommand]
    private async Task UpsertRecord()
    {
        int index = AgreementList.FindIndex(x => x.Id == EditingAgreement.Id);

        if (index > -1)
        {

            AgreementList[index] = EditingAgreement;

        }
        else
        {             
            AgreementList.Add(EditingAgreement);
        }   

        await dbApp.UpSertRecordAsync<LiquidDocsData.Models.LoanAgreement>(EditingAgreement);


    }

    [RelayCommand]
    private async Task DeleteRecord(LiquidDocsData.Models.LoanAgreement r)
    {

        int recordListIndex = AgreementList.FindIndex(x => x.Id == r.Id);

        if (recordListIndex > -1)
        {

           AgreementList.RemoveAt(recordListIndex);

        }

        dbApp.DeleteRecord<LiquidDocsData.Models.LoanAgreement>(r);

    }
    

    [RelayCommand]
    private void SelectAgreement(LiquidDocsData.Models.LoanAgreement r)
    {
        SelectedAgreement = EditingAgreement;
    }

    [RelayCommand]
    private void ClearSelection()
    {
        if (SelectedAgreement != null)
        {
            SelectedAgreement = null;
            GetNewRecord();
        }
    }

    public async Task GenerateNewLoanNumberAsync()
    {
        nextLoanNumber++;
        string loanNumberPrefix = "LN-";
        string uniqueIdentifier = $"{DateTime.UtcNow.ToString("yyyy", CultureInfo.InvariantCulture)}-{nextLoanNumber}";

        EditingAgreement.LoanNumber = $"{loanNumberPrefix}{uniqueIdentifier}";
    }


    [RelayCommand]
    private void GetNewRecord()
    {
        EditingAgreement = new LiquidDocsData.Models.LoanAgreement()
        {
            UserId = Guid.Parse(userId)
        };

    }

    public decimal EstimatedDownPayment => Math.Round(EditingAgreement.PrincipalAmount * (EditingAgreement.DownPaymentPercentage / 100m), 2);

    
}