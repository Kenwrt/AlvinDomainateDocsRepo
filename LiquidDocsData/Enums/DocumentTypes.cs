namespace LiquidDocsData.Enums;

public class DocumentTypes
{
    public enum Types
    {
        [System.ComponentModel.Description("Security")]
        Security,

        [System.ComponentModel.Description("UCC")]
        UCC,

        [System.ComponentModel.Description("Note")]
        Note,

        [System.ComponentModel.Description("LoanAgreement")]
        LoanAgreement,

        [System.ComponentModel.Description("Mortgage")]
        Mortgage,

        [System.ComponentModel.Description("Standard")]
        Standard,

        [System.ComponentModel.Description("Deed of Trust")]
        Deed
    }

    public enum OutputTypes
    {
        [System.ComponentModel.Description("Word")]
        Word,

        [System.ComponentModel.Description("Pdf")]
        Pdf
    }

    public enum TestTypes
    {
        [System.ComponentModel.Description("Document")]
        Document,

        [System.ComponentModel.Description("Document Set")]
        DocumentSet

    }


}