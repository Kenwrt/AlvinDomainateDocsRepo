using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentManager.CalculatorsSchedulers;
using DocumentManager.Services;
using DocumentManager.State;
using LiquidDocsData.Enums;
using LiquidDocsData.Models;
using LiquidDocsData.Models.DTOs;
using LiquidDocsSite.Database;
using LiquidDocsSite.Helpers;
using LiquidDocsSite.State;
using Nextended.Core.Extensions;
using System.Collections.ObjectModel;
using System.Globalization;

namespace LiquidDocsSite.ViewModels;

public partial class LoanAgreementViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.LoanAgreement>? agreementList = new();

    [ObservableProperty]
    private ObservableCollection<LiquidDocsData.Models.DTOs.LoanTypeListDTO>? loanTypes = new();

    [ObservableProperty]
    private LiquidDocsData.Models.Borrower selectedBorrower = null;

    [ObservableProperty]
    private LiquidDocsData.Models.Broker selectedBroker = null;

    [ObservableProperty]
    private LiquidDocsData.Models.Guarantor selectedGuarantor = null;

    [ObservableProperty]
    private LiquidDocsData.Models.Lender selectedLender = null;

    [ObservableProperty]
    private LiquidDocsData.Models.PropertyRecord selectedProperty = null;

    [ObservableProperty]
    private LiquidDocsData.Models.LoanAgreement editingAgreement = null;

    [ObservableProperty]
    private LiquidDocsData.Models.LoanAgreement selectedAgreement = null;

    [ObservableProperty]
    private PaymentSchedule? currentSchedule = new();

    [ObservableProperty]
    private BalloonPayments currentBalloonSchedule = new();

    // Mirror fields bound by the UI (keep names obvious)
    [ObservableProperty] private decimal principalAmount;

    [ObservableProperty] private decimal interestRate;
    [ObservableProperty] private decimal initialMargin;
    [ObservableProperty] private decimal estimatedDwnPaymentAmount;
    [ObservableProperty] private int termInMonths;
    [ObservableProperty] private decimal downPaymentPercentage;
    [ObservableProperty] private decimal balloonAmount;
    [ObservableProperty] private int balloonTermMonths;
    [ObservableProperty] private LiquidDocsData.Enums.Payment.AmortizationTypes amorizationType;
    [ObservableProperty] private LiquidDocsData.Enums.Payment.Schedules repaymentSchedule;
    [ObservableProperty] private LiquidDocsData.Enums.Payment.RateTypes rateType;
    [ObservableProperty] private LiquidDocsData.Enums.Payment.Schedules adjustmentInterval;
    [ObservableProperty] private LiquidDocsData.Enums.Payment.IndexPaths assumedIndexPath;
    [ObservableProperty] private LiquidDocsData.Enums.Payment.RateIndexes rateIndex;
    [ObservableProperty] private DateTime? maturityDate;
    [ObservableProperty] private PaymentSchedule paySchedule;
    [ObservableProperty] private BalloonPayments payBalloonSchedule;
    [ObservableProperty] private PaymentSchedule fixedPaymentSchedule;

    private string userId;

    private UserSession session;

    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<LoanAgreementViewModel> logger;
    private readonly UserSession userSession;
    private IApplicationStateManager appState;
    private DashboardViewModel dvm;
    private IDocumentManagerState docState;
    private ILoanScheduler loanScheduler;
    private IBalloonPaymentCalculater balloonPaymentCalculater;
    private IFetchCurrentIndexRatesAndSchedulesService indexRates;

    private int nextLoanNumber = 0;

    public LoanAgreementViewModel(IMongoDatabaseRepo dbApp, ILogger<LoanAgreementViewModel> logger, UserSession userSession, IApplicationStateManager appState, DashboardViewModel dvm, IDocumentManagerState docState, ILoanScheduler loanScheduler, IBalloonPaymentCalculater balloonPaymentCalculater, IFetchCurrentIndexRatesAndSchedulesService indexRates)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;
        this.dvm = dvm;
        this.docState = docState;
        this.loanScheduler = loanScheduler;
        this.balloonPaymentCalculater = balloonPaymentCalculater;
        this.indexRates = indexRates;

        userId = userSession.UserId;

        LoanTypes = new ObservableCollection<LiquidDocsData.Models.DTOs.LoanTypeListDTO>(dbApp.GetRecords<LiquidDocsData.Models.LoanType>().Select(x => new LiquidDocsData.Models.DTOs.LoanTypeListDTO(x.Id, x.Name, x.Description, x.IconKey)));

    }

    [RelayCommand]
    private async Task InitializePage()
    {
        AgreementList = new ObservableCollection<LiquidDocsData.Models.LoanAgreement>(dbApp.GetRecords<LiquidDocsData.Models.LoanAgreement>().Where(x => x.UserId == Guid.Parse(userSession.UserId)));

        if (AgreementList.Count > 0)
        {
            nextLoanNumber = AgreementList.Max(x => Convert.ToInt32(x.LoanNumber.Substring(8))); //"LN-2024-0";
        }

        if (dvm.SelectedAgreement is not null)
        {
            SelectedAgreement = dvm.SelectedAgreement;
            EditingAgreement = SelectedAgreement;
        }
        else
        {
            EditingAgreement = GetNewRecord();
        }

        SyncFromEditingAgreement();
    }

    [RelayCommand]
    private async Task InitializeRecord()
    {
        if (EditingAgreement is null)
        {
            EditingAgreement = GetNewRecord();
        }

        if (EditingAgreement.DownPaymentPercentage > 0)
        {
            EditingAgreement.DownPaymentAmmount = EstimatedDownPayment;

            EstimatedDwnPaymentAmount = EditingAgreement.PrincipalAmount * (EditingAgreement.DownPaymentPercentage / 100m);

            EditingAgreement.DownPaymentAmmount = EstimatedDwnPaymentAmount;
        }

        GetLoanMaturityDate(EditingAgreement.TermInMonths);
    }

    [RelayCommand]
    private void UpsertAgreement(LiquidDocsData.Models.LoanAgreement r)
    {
        try
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

            dbApp.UpSertRecord<LiquidDocsData.Models.LoanAgreement>(r);
        }
        catch (Exception ex)
        {
            string Error = ex.Message;
        }
    }

    [RelayCommand]
    private async Task EditAgreement(LiquidDocsData.Models.LoanAgreement r)
    {
        int index = AgreementList.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            AgreementList[index] = r;
        }

        await dbApp.UpSertRecordAsync<LiquidDocsData.Models.LoanAgreement>(EditingAgreement);

        AgreementList.Clear();

        SelectedAgreement = EditingAgreement;
    }

    [RelayCommand]
    private void DeleteAgreement(LiquidDocsData.Models.LoanAgreement r)
    {
        if (r != null)
        {
            int index = AgreementList.FindIndex(x => x.Id == r.Id);

            if (index > -1)
            {
                AgreementList.RemoveAt(index);
            }

            dbApp.DeleteRecord<LiquidDocsData.Models.LoanAgreement>(SelectedAgreement);

            SelectedAgreement = null;

            EditingAgreement = GetNewRecord();
        }
    }

    [RelayCommand]
    private void SelectAgreement(LiquidDocsData.Models.LoanAgreement r)
    {
        SelectedAgreement = EditingAgreement;

        GetLoanMaturityDate(EditingAgreement.TermInMonths);
    }

    [RelayCommand]
    private async Task ProcessAgreement()
    {
        docState.LoanProcessQueue.Enqueue(EditingAgreement);
    }

    [RelayCommand]
    private void ClearSelection()
    {
        if (SelectedAgreement != null)
        {
            SelectedAgreement = null;
            EditingAgreement = new LiquidDocsData.Models.LoanAgreement();
        }
    }

    [RelayCommand]
    private async Task AddAgreement()
    {
        AgreementList.Add(EditingAgreement);

        dbApp.UpSertRecord<LiquidDocsData.Models.LoanAgreement>(EditingAgreement);
    }

    [RelayCommand]
    private async Task DeleteBorrower(LiquidDocsData.Models.Borrower r)
    {
        int index = EditingAgreement.Borrowers.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            EditingAgreement.Borrowers.RemoveAt(index);
        }

        UpsertAgreement(EditingAgreement);
    }

    [RelayCommand]
    private async Task UpsertBorrower(LiquidDocsData.Models.Borrower r)
    {
        int index = EditingAgreement.Borrowers.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            EditingAgreement.Borrowers[index] = r;
        }
        else
        {
            EditingAgreement.Borrowers.Add(r);
        }

        UpsertAgreement(EditingAgreement);
    }

    [RelayCommand]
    private async Task DeleteBroker(LiquidDocsData.Models.Broker r)
    {
        int index = EditingAgreement.Brokers.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            EditingAgreement.Brokers.RemoveAt(index);
        }

        UpsertAgreement(EditingAgreement);
    }

    [RelayCommand]
    private async Task UpsertBroker(LiquidDocsData.Models.Broker r)
    {
        int index = EditingAgreement.Brokers.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            EditingAgreement.Brokers[index] = r;
        }
        else
        {
            EditingAgreement.Brokers.Add(r);
        }

        UpsertAgreement(EditingAgreement);
    }

    [RelayCommand]
    private async Task DeleteGuarantor(LiquidDocsData.Models.Guarantor r)
    {
        int index = EditingAgreement.Guarantors.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            EditingAgreement.Guarantors.RemoveAt(index);
        }

        UpsertAgreement(EditingAgreement);
    }

    [RelayCommand]
    private async Task UpsertGuarantor(LiquidDocsData.Models.Guarantor r)
    {
        int index = EditingAgreement.Guarantors.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            EditingAgreement.Guarantors[index] = r;
        }
        else
        {
            EditingAgreement.Guarantors.Add(r);
        }

        UpsertAgreement(EditingAgreement);
    }

    [RelayCommand]
    private async Task DeleteLender(LiquidDocsData.Models.Lender r)
    {
        int index = EditingAgreement.Lenders.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            EditingAgreement.Lenders.RemoveAt(index);
        }

        UpsertAgreement(EditingAgreement);
    }

    [RelayCommand]
    private async Task UpsertLender(LiquidDocsData.Models.Lender r)
    {
        int index = EditingAgreement.Lenders.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            EditingAgreement.Lenders[index] = r;
        }
        else
        {
            EditingAgreement.Lenders.Add(r);
        }

        UpsertAgreement(EditingAgreement);
    }

    [RelayCommand]
    private async Task DeleteProperty(LiquidDocsData.Models.PropertyRecord r)
    {
        int index = EditingAgreement.Properties.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            EditingAgreement.Properties.RemoveAt(index);
        }

        UpsertAgreement(EditingAgreement);
    }

    [RelayCommand]
    private async Task UpsertProperty(LiquidDocsData.Models.PropertyRecord r)
    {
        int index = EditingAgreement.Properties.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            EditingAgreement.Properties[index] = r;
        }
        else
        {
            EditingAgreement.Properties.Add(r);
        }

        UpsertAgreement(EditingAgreement);
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

    public decimal EstimatedDownPayment => Math.Round(EditingAgreement.PrincipalAmount * (EditingAgreement.DownPaymentPercentage / 100m), 2);

    public DateOnly GetLoanMaturityDate(int termsInMonths)
    {
        DateTime date = DateTime.Now;

        if (termsInMonths != 0)
        {
            if (EditingAgreement.SignedDate is null)
            {
                date = DateTime.Now;
            }
            else
            {
                date = EditingAgreement.SignedDate.Value;
            }

            MaturityDate = date.AddMonths(termsInMonths);

            date = date.AddMonths(termsInMonths);
        }

        return DateOnly.FromDateTime(date);
    }

    public DateOnly GetBalloonDate(int termsInMonths)
    {
        DateTime date = DateTime.Now;

        if (termsInMonths != 0)
        {
            if (EditingAgreement.SignedDate is null)
            {
                date = DateTime.Now;
            }
            else
            {
                date = EditingAgreement.SignedDate.Value;
            }

            EditingAgreement.BalloonPayments.DueDate = DateOnly.FromDateTime(date.AddMonths(termsInMonths));
        }

        return EditingAgreement.BalloonPayments.DueDate;
    }

    partial void OnEditingAgreementChanged(LiquidDocsData.Models.LoanAgreement value) => SyncFromEditingAgreement();

    //******************************

    private void SyncFromEditingAgreement()
    {
        if (EditingAgreement is null) return;

        PrincipalAmount = EditingAgreement.PrincipalAmount;
        InterestRate = EditingAgreement.InterestRate;
        TermInMonths = EditingAgreement.TermInMonths;
        AmorizationType = EditingAgreement.AmorizationType;
        RepaymentSchedule = EditingAgreement.RepaymentSchedule;
        MaturityDate = EditingAgreement.MaturityDate;
        BalloonTermMonths = EditingAgreement.BalloonPayments.BalloonTermMonths;
        BalloonAmount = EditingAgreement.BalloonPayments.BalloonAmount;
        PayBalloonSchedule = EditingAgreement.BalloonPayments;
        InitialMargin = EditingAgreement.InitialMargin;
        AdjustmentInterval = EditingAgreement.VariableInterestProperties.AdjustmentInterval;
        AssumedIndexPath = EditingAgreement.VariableInterestProperties.AssumedIndexPath;
        RateIndex = EditingAgreement.VariableInterestProperties.RateIndex;
        DownPaymentPercentage = EditingAgreement.DownPaymentPercentage;
        EstimatedDwnPaymentAmount = EditingAgreement.PrincipalAmount * (EditingAgreement.DownPaymentPercentage / 100m);
        RateType = EditingAgreement.RateType;
        PaySchedule = EditingAgreement.VariableInterestProperties.PaymentSchedule;
        FixedPaymentSchedule = EditingAgreement.FixedPaymentSchedule;
    }

    //public PaymentSchedule PaymentSchedule()
    //{
    //    PaymentSchedule result = null;

    //    DateTime startDate = DateTime.Now;
    //    DateTime endDate = DateTime.Now;

    //    if (EditingAgreement.SignedDate is null) startDate = DateTime.Now;

    //    if (EditingAgreement.MaturityDate is null) endDate = DateTime.Now.AddMonths(12);

    //    if (EditingAgreement.RateType == Payment.RateTypes.Fixed)
    //    {
    //        if (EditingAgreement.FixedInterestProperties.InterestRate != 0 && EditingAgreement.PrincipalAmount != 0)
    //        {
    //            result = loanScheduler.GenerateFixed(EditingAgreement.DownPaymentAmmount, EditingAgreement.FixedInterestProperties.InterestRate, EditingAgreement.DownPaymentPercentage, startDate, endDate, EditingAgreement.FixedInterestProperties.AmorizationType);
    //        }
    //    }
    //    else
    //    {
    //        if (EditingAgreement.VariableInterestProperties.InterestRate != 0 && EditingAgreement.PrincipalAmount != 0)
    //        {
    //            result = loanScheduler.GenerateVariable(EditingAgreement.DownPaymentAmmount, EditingAgreement.VariableInterestProperties.InterestRate, startDate, endDate, EditingAgreement.VariableInterestProperties.AmorizationType, EditingAgreement.VariableInterestProperties.RateChangeList);
    //        }

    //    }

    //    return result;
    //}

    //*********************************************************************
    // Push mirrors back into the model and recompute on each change
    partial void OnPrincipalAmountChanged(decimal value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.PrincipalAmount = value;

        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType, EditingAgreement.VariableInterestProperties.RateChangeList);
        }
        else
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType);
        }
    }

    partial void OnPayBalloonScheduleChanged(BalloonPayments value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.BalloonPayments = value;

        // RecomputeSchedule(EditingAgreement.VariableInterestProperties.TermInMonths, EditingAgreement.VariableInterestProperties.InterestRate, EditingAgreement.VariableInterestProperties.RepaymentSchedule, EditingAgreement.VariableInterestProperties.AmorizationType);
    }

    partial void OnPayScheduleChanged(PaymentSchedule value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.VariableInterestProperties.PaymentSchedule = value;
    }

    partial void OnFixedPaymentScheduleChanged(PaymentSchedule value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.FixedPaymentSchedule = value;
    }

    partial void OnBalloonAmountChanged(decimal value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.BalloonPayments.BalloonAmount = value;

        RecomputeBalloonSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType);
    }

    partial void OnInterestRateChanged(decimal value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.InterestRate = value;

        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType, EditingAgreement.VariableInterestProperties.RateChangeList);
        }
        else
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType);
        }
    }

    partial void OnInitialMarginChanged(decimal value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.InitialMargin = value;
    }

    partial void OnTermInMonthsChanged(int value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.TermInMonths = value;

        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType, EditingAgreement.VariableInterestProperties.RateChangeList);
        }
        else
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType);
        }
    }

    partial void OnBalloonTermMonthsChanged(int value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.BalloonPayments.BalloonTermMonths = value;

        RecomputeBalloonSchedule(EditingAgreement.BalloonPayments.BalloonTermMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType);
    }

    partial void OnAmorizationTypeChanged(LiquidDocsData.Enums.Payment.AmortizationTypes value)
    {
        if (EditingAgreement is null) return;

        if (AmorizationType == Payment.AmortizationTypes.PartiallyAmortized || AmorizationType == Payment.AmortizationTypes.Other)
        {
            EditingAgreement.IsBalloonPayment = true;
        }

        EditingAgreement.AmorizationType = value;

        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
        {
            //var latestSofr = indexRates.GetLatestSofrAsync();

            //var curve = indexRates.GetLatestSofrAsync(latestSofr, resetsNeeded: 12);

            var schedule = indexRates.GenerateProjectedSchedule(terms: new FetchCurrentIndexRatesAndSchedulesService.LoanTerms());

            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType, EditingAgreement.VariableInterestProperties.RateChangeList);
        }
        else
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType);
        }
    }

    partial void OnRepaymentScheduleChanged(LiquidDocsData.Enums.Payment.Schedules value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.RepaymentSchedule = value;

        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType, EditingAgreement.VariableInterestProperties.RateChangeList);
        }
        else
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType);
        }
    }

    partial void OnRateTypeChanged(LiquidDocsData.Enums.Payment.RateTypes value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.RateType = value;

        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType, EditingAgreement.VariableInterestProperties.RateChangeList);
        }
        else
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType);
        }
    }

    partial void OnDownPaymentPercentageChanged(decimal value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.DownPaymentPercentage = value;
    }

    partial void OnRateIndexChanged(LiquidDocsData.Enums.Payment.RateIndexes value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.VariableInterestProperties.RateIndex = value;

        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType, EditingAgreement.VariableInterestProperties.RateChangeList);
        }
    }

    partial void OnMaturityDateChanged(DateTime? value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.MaturityDate = value;
    }

    partial void OnAdjustmentIntervalChanged(LiquidDocsData.Enums.Payment.Schedules value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.VariableInterestProperties.AdjustmentInterval = value;

        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType, EditingAgreement.VariableInterestProperties.RateChangeList);
        }
    }

    partial void OnAssumedIndexPathChanged(LiquidDocsData.Enums.Payment.IndexPaths value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.VariableInterestProperties.AssumedIndexPath = value;

        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType, EditingAgreement.VariableInterestProperties.RateChangeList);
        }
    }

    // Single place that decides schedule creation with full null-safety
    private void RecomputeSchedule(int termsInMoths, decimal interestRate, Payment.Schedules paymentSchedule, Payment.AmortizationTypes amortizationType, List<RateChange>? rateChangeList = null)
    {
        try
        {
            if (EditingAgreement is null)
            {
                CurrentSchedule = new();
                return;
            }

            var start = EditingAgreement.SignedDate ?? DateTime.Today;

            var end = EditingAgreement.MaturityDate
                        ?? (EditingAgreement.OriginationDate ?? DateTime.Today).AddMonths(
                            termsInMoths > 0 ? termsInMoths : 12);

            if (EditingAgreement.PrincipalAmount > 0 && EditingAgreement.DownPaymentPercentage > -1 && termsInMoths > 0 && start < end)
            {
                if (EditingAgreement.PrincipalAmount <= 0 || interestRate <= 0 || end <= start)
                {
                    CurrentSchedule = new();

                    if (EditingAgreement.RateType == Payment.RateTypes.Variable)
                    {
                        EditingAgreement.VariableInterestProperties.PaymentSchedule = CurrentSchedule;
                    }
                    else
                    {
                        EditingAgreement.FixedPaymentSchedule = CurrentSchedule;
                    }

                    return;
                }

                PaymentSchedule schedule = null;

                if (EditingAgreement.RateType == LiquidDocsData.Enums.Payment.RateTypes.Fixed)
                {
                    schedule = loanScheduler.GenerateFixed(
                        principal: EditingAgreement.PrincipalAmount - EditingAgreement.DownPaymentAmmount,
                        annualRatePercent: interestRate,
                        downPaymentPercent: EditingAgreement.DownPaymentPercentage,
                        startDate: start,
                        endDate: end,
                        amortizationType: amortizationType,
                        amortizationTermMonths: termsInMoths);

                    EditingAgreement.FixedPaymentSchedule = schedule ?? new();
                }
                else
                {
                    schedule = loanScheduler.GenerateVariable(
                        principal: EditingAgreement.PrincipalAmount - EditingAgreement.DownPaymentAmmount,
                        downPaymentPercent: EditingAgreement.DownPaymentPercentage,
                        startDate: start,
                        endDate: end,
                        amortizationType: amortizationType,
                        rateSchedule: rateChangeList,
                        amortizationTermMonths: termsInMoths);

                    EditingAgreement.VariableInterestProperties.PaymentSchedule = schedule ?? new();
                }

                //CurrentSchedule = schedule ?? new();
            }
        }
        catch (SystemException ex)
        {
            logger.LogError(ex.Message);
        }
    }

    private void RecomputeBalloonSchedule(int termsInMoths, decimal interestRate, Payment.Schedules paymentSchedule, Payment.AmortizationTypes amortizationType)
    {
        try
        {
            if (EditingAgreement is null)
            {
                CurrentBalloonSchedule = new BalloonPayments();
                return;
            }

            var start = EditingAgreement.SignedDate ?? DateTime.Today;

            var end = EditingAgreement.MaturityDate
                        ?? (EditingAgreement.OriginationDate ?? DateTime.Today).AddMonths(
                            termsInMoths > 0 ? termsInMoths : 12);

            if (EditingAgreement.PrincipalAmount > 0 && EditingAgreement.DownPaymentPercentage > -1 && termsInMoths > 0 && start < end)
            {
                if (EditingAgreement.PrincipalAmount <= 0 || interestRate <= 0 || end <= start)
                {
                    return;
                }

                BalloonPayments schedule = null;

                DateTime firstPayment = EditingAgreement.SignedDate ?? DateTime.Today;

                schedule = balloonPaymentCalculater.Generate(
                    principal: EditingAgreement.PrincipalAmount - EditingAgreement.DownPaymentAmmount,
                    annualRatePercent: InterestRate,
                    amortizationTermMonths: TermInMonths,
                    balloonTermMonths: BalloonTermMonths,
                    firstPaymentDate: firstPayment.AddMonths(1),
                    paymentsPerYear: 12);

                PayBalloonSchedule = schedule ?? new();

                PayBalloonSchedule.DueDate = GetBalloonDate(BalloonTermMonths);
            }
        }
        catch (SystemException ex)
        {
            logger.LogError(ex.Message);
        }
    }
}