using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiquidDocsData.Enums;
using LiquidDocsSite.Database;
using LiquidDocsSite.Helpers;
using LiquidDocsSite.State;
using System.Collections.ObjectModel;
using System.Globalization;

namespace LiquidDocsSite.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.LoanAgreement>? agreementList = new();

    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.DocumentSet>? documentSets = new();

    [ObservableProperty]
    private LiquidDocsData.Models.LoanAgreement editingAgreement = null;

    [ObservableProperty]
    private LiquidDocsData.Models.LoanAgreement selectedAgreement = null;

    [ObservableProperty]
    private string totalPortfolio = null;

    [ObservableProperty]
    private string activeLoanCount = null;

    [ObservableProperty]
    private string pendingLoanCount = null;

    [ObservableProperty]
    private string averageInterestRate = null;

    private string userId;

    private UserSession userSession;
    private IApplicationStateManager appState;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<DashboardViewModel> logger;

    private int nextLoanNumber = 0;

    public DashboardViewModel(IMongoDatabaseRepo dbApp, ILogger<DashboardViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;

        userId = userSession.UserId;

       
                   
    }

    [RelayCommand]
    private async Task InitDashboard()
    {
        if (userSession.UserRole == UserEnums.Roles.Admin.ToString())
        {
            AgreementList = dbApp.GetRecords<LiquidDocsData.Models.LoanAgreement>().ToObservableCollection();
        }
        else
        {
            AgreementList = dbApp.GetRecords<LiquidDocsData.Models.LoanAgreement>().Where(x => x.UserId == Guid.Parse(userId)).ToObservableCollection();
        }


        DocumentSets = new ObservableCollection<LiquidDocsData.Models.DocumentSet>(dbApp.GetRecords<LiquidDocsData.Models.DocumentSet>().Where(x => x.UserId == Guid.Parse(userSession.UserId)));


        if (userSession.UserRole == UserEnums.Roles.Admin.ToString() || userSession.UserRole == UserEnums.Roles.DevAdmin.ToString())
        {
            if (AgreementList.Count > 0)
            {
                TotalPortfolio = DisplayHelper.FormatDollarsCompact(AgreementList.Sum(c => c.PrincipalAmount));

                ActiveLoanCount = AgreementList.Where(x => x.Status != Loan.Status.Approved).Count().ToString("#,0");

                PendingLoanCount = AgreementList.Where(x => x.Status == Loan.Status.Pending).Count().ToString("#,0");

                AverageInterestRate = ((AgreementList.Where(x => x.Status != Loan.Status.Cancelled).Sum(s => s.InterestRate) / AgreementList.Where(x => x.Status != Loan.Status.Cancelled).Count())).ToString("N2");
            }
        }
        else
        {
            if (AgreementList.Count > 0)
            {
                TotalPortfolio = DisplayHelper.FormatDollarsCompact(AgreementList.Where(x => x.UserId == Guid.Parse(userSession.UserId)).Sum(c => c.PrincipalAmount));

                ActiveLoanCount = AgreementList.Where(x => x.Status != Loan.Status.Approved && x.UserId == Guid.Parse(userSession.UserId)).Count().ToString("#,0");

                PendingLoanCount = AgreementList.Where(x => x.Status != Loan.Status.Pending && x.UserId == Guid.Parse(userSession.UserId)).Count().ToString("#,0");

                AverageInterestRate = ((AgreementList.Where(x => x.Status != Loan.Status.Cancelled && x.UserId == Guid.Parse(userSession.UserId)).Sum(s => s.InterestRate) / AgreementList.Where(x => x.Status != Loan.Status.Cancelled).Count())).ToString("N2");
            }
        }
    }

    [RelayCommand]
    private async Task InitializeRecord()
    {
        EditingAgreement = GetNewRecord();
    }

    [RelayCommand]
    private void UpsertAgreement(LiquidDocsData.Models.LoanAgreement r)
    {
        try
        {
            int index = AgreementList.FindIndex(x => x.Id == r.Id);

            if (index > -1)
            {

                AgreementList.RemoveAt(index);

            }
            else
            {
                AgreementList.Add(r);
            }
            
            dbApp.UpSertRecord<LiquidDocsData.Models.LoanAgreement>(r);
        }
        catch (Exception ex)
        {
            string Error = ex.Message;
        }
    }

    [RelayCommand]
    private void EditAgreement(LiquidDocsData.Models.LoanAgreement r)
    {
        int index = AgreementList.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {

            AgreementList[index] = r;

        }
        else
        {
            AgreementList.Add(r);
        }

        dbApp.UpSertRecord<LiquidDocsData.Models.LoanAgreement>(EditingAgreement);

        SelectedAgreement = null;
    }

    [RelayCommand]
    private void DeleteAgreement(LiquidDocsData.Models.LoanAgreement r)
    { 
        int index = AgreementList.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {

            AgreementList.RemoveAt(index);

        }

        dbApp.DeleteRecord<LiquidDocsData.Models.LoanAgreement>(r);

        SelectedAgreement = null;
        EditingAgreement = GetNewRecord();
        
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
            EditingAgreement = GetNewRecord();
        }
    }

    [RelayCommand]
    private async Task AddAgreement()
    {
        AgreementList.Add(EditingAgreement);

        dbApp.UpSertRecord<LiquidDocsData.Models.LoanAgreement>(EditingAgreement);
    }

    public async Task<string> GenerateNewLoanNumberAsync()
    {
        nextLoanNumber++;
        string loanNumberPrefix = "LN-";
        string uniqueIdentifier = $"{DateTime.UtcNow.ToString("yyyy", CultureInfo.InvariantCulture)}-{nextLoanNumber}";

        EditingAgreement.LoanNumber = $"{loanNumberPrefix}{uniqueIdentifier}";

        return $"{loanNumberPrefix}{uniqueIdentifier}";
    }

    private LiquidDocsData.Models.LoanAgreement GetNewRecord()
    {
        EditingAgreement = new LiquidDocsData.Models.LoanAgreement()
        {
            UserId = Guid.Parse(userId)
        };

        return EditingAgreement;
    }
}