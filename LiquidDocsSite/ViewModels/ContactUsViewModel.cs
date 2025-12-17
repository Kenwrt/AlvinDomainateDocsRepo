using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiquidDocsData.Enums;
using LiquidDocsNotify.Models;
using LiquidDocsNotify.Enums;
using LiquidDocsNotify.State;
using PostmarkDotNet.Model;
using System.Collections.Concurrent;
using System.Globalization;
using Twilio.Types;

namespace LiquidDocsSite.ViewModels;

public partial class ContactUsViewModel : ObservableObject
{
  

    [ObservableProperty]
    private EmailMsg editingMailMsg = null;

    [ObservableProperty]
    private EmailMsg selectedMailMsg = null;



    public INotifyState? NotifyState { get; set; }
    
    public ContactUsViewModel(INotifyState? NotifyState)
    {
        this.NotifyState = NotifyState;
    }

    [RelayCommand]
    private async Task InitializePageAsync()
    {
        if (EditingMailMsg is null)
        {
            EditingMailMsg = new();
        }




    }

    [RelayCommand]
    private async Task InitializeRecord()
    {
        //if (EditingAgreement is null)
        //{
        //    EditingAgreement = GetNewRecord();
        //}

        //if (EditingAgreement.DownPaymentPercentage > 0)
        //{
        //    EditingAgreement.DownPaymentAmmount = EstimatedDownPayment;

        //    EstimatedDwnPaymentAmount = EditingAgreement.PrincipalAmount * (EditingAgreement.DownPaymentPercentage / 100m);

        //    EditingAgreement.DownPaymentAmmount = EstimatedDwnPaymentAmount;
        //}

        //if (EditingAgreement.RateType == Payment.RateTypes.Fixed)
        //{
        //    GetLoanMaturityDate(EditingAgreement.FixedInterestProperties.TermInMonths);
        //}
        //else
        //{
        //    GetLoanMaturityDate(EditingAgreement.VariableInterestProperties.TermInMonths);
        //}


    }

    [RelayCommand]
    private void UpsertMailMsg()
    {
        try
        {
           //dbApp.UpSertRecord<LiquidDocsData.Models.LoanAgreement>(EditingAgreement);
        }
        catch (Exception ex)
        {
            string Error = ex.Message;
        }
    }

    [RelayCommand]
    private void EditMailMsg()
    {
        //dbApp.UpSertRecord<LiquidDocsData.Models.LoanAgreement>(EditingAgreement);

        //AgreementList.Clear();

        //dbApp.GetRecords<LiquidDocsData.Models.LoanAgreement>().ToList().ForEach(r => AgreementList.Add(r));

        //SelectedAgreement = EditingAgreement;
    }

    [RelayCommand]
    private void DeleteMailMsg()
    {
        //if (SelectedAgreement != null)
        //{
        //    AgreementList.Remove(SelectedAgreement);

        //    dbApp.DeleteRecord<LiquidDocsData.Models.LoanAgreement>(SelectedAgreement);

        //    SelectedAgreement = null;
        //    EditingAgreement = GetNewRecord();
        //}
    }

    [RelayCommand]
    private void SelectMailMsg(EmailMsg r)
    {
        //SelectedAgreement = EditingAgreement;


        //if (EditingAgreement.RateType == Payment.RateTypes.Fixed)
        //{
        //    GetLoanMaturityDate(EditingAgreement.FixedInterestProperties.TermInMonths);
        //}
        //else
        //{
        //    GetLoanMaturityDate(EditingAgreement.VariableInterestProperties.TermInMonths);
        //}
    }

   
    [RelayCommand]
    private void ClearSelection()
    {
        if (SelectedMailMsg != null)
        {
            SelectedMailMsg = null;

            EditingMailMsg = new();
        }
    }

    [RelayCommand]
    private async Task AddMailMsg()
    {
        //AgreementList.Add(EditingAgreement);

        //dbApp.UpSertRecord<LiquidDocsData.Models.LoanAgreement>(EditingAgreement);
    }

   
    [RelayCommand]
    private void SendMail()
    {
        try
        {
            EmailMsg mailMSG = new()
            {
                To = EditingMailMsg.ReplyTo,
                Subject = "Contact Us Message",
                PostMarkTemplateId = (int)LiquidDocsNotify.Enums.EmailEnums.Templates.ContactUs,
                TemplateModel = new
                {
                    login_url = "https://liquiddocs.law/account/login",
                    username = EditingMailMsg.Name ?? string.Empty,
                    product_name = "LiquidDocs",
                    support_email = "https://liquiddocs.law/support",
                    help_url = "https://liquiddocs.law/help",
                    name = EditingMailMsg.Name,
                    phone = EditingMailMsg.Phone,
                    email = EditingMailMsg.ReplyTo,
                    message = EditingMailMsg.MessageBody
                }

            };

            NotifyState.EmailMsgProcessingQueue.Enqueue(mailMSG);


            mailMSG = new()
            {
                To = "ContactUs@LiquidDocs.law",
                Subject = "User Contact Message",
                MessageBody = $"From: {EditingMailMsg.Name} Phone: {EditingMailMsg.Phone} Message: {EditingMailMsg.MessageBody}",
                PostMarkTemplateId = (int)LiquidDocsNotify.Enums.EmailEnums.Templates.UserContact,
                TemplateModel = new
                {
                     login_url = "https://liquiddocs.law/account/login",
                     username = EditingMailMsg.Name ?? string.Empty,
                     product_name = "LiquidDocs",
                     support_email = "https://liquiddocs.law/support",
                     help_url = "https://liquiddocs.law/help",
                     name = EditingMailMsg.Name,
                     email = EditingMailMsg.ReplyTo,
                     message = EditingMailMsg.MessageBody
                }

            };


            NotifyState.EmailMsgProcessingQueue.Enqueue(mailMSG);




        }
        catch (System.Exception ex)
        {
            throw;
        }
    }
}