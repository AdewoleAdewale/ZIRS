using System;
using System.Text.Json.Serialization;

namespace ZamfaraIRS.Models
{
    public class MarketModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("market_Plaza")]
        public string Market_Plaza { get; set; }

        [JsonPropertyName("mktCode")]
        public string MktCode { get; set; }

        [JsonPropertyName("lga")]
        public string Lga { get; set; }

        [JsonPropertyName("daterecorded")]
        public DateTime? DateRecorded { get; set; }
    }

    public class ShopCategoryModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("shopCategoryName")]
        public string ShopCategoryName { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("mkdId")]
        public int MkdId { get; set; }
    }

    public class ShopItemModel
    {
        [JsonPropertyName("shopNo")]
        public string ShopNo { get; set; }

        [JsonPropertyName("shopCat")]
        public string ShopCat { get; set; }

        [JsonPropertyName("marketName")]
        public string MarketName { get; set; }

        [JsonPropertyName("marketId")]
        public int MarketId { get; set; }

        [JsonPropertyName("dateRecorded")]
        public string DateRecorded { get; set; }

        [JsonPropertyName("recordedBy")]
        public string RecordedBy { get; set; }

        [JsonPropertyName("lastPaymentDate")]
        public string LastPaymentDate { get; set; }

        [JsonPropertyName("expDate")]
        public string ExpDate { get; set; }

        [JsonPropertyName("amtPaidSoFar")]
        public string AmtPaidSoFar { get; set; }

        [JsonPropertyName("currentOccupant")]
        public string CurrentOccupant { get; set; }

        [JsonPropertyName("amount")]
        public string Amount { get; set; }

        [JsonPropertyName("totalAmtPaid")]
        public string TotalAmtPaid { get; set; }

        [JsonPropertyName("balance")]
        public string Balance { get; set; }

        [JsonPropertyName("lga")]
        public string Lga { get; set; }
        public bool IsExpanded { get; internal set; }
        public string ExpandIcon { get; internal set; }
    }

    public class ApiResponse
    {
        [JsonPropertyName("businessName")]
        public string BusinessName { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("payerId")]
        public string PayerId { get; set; }
    }

    public class ShopVerificationModel
    {
        [JsonPropertyName("shopNo")]
        public string ShopNo { get; set; }

        [JsonPropertyName("mktId")]
        public string MktId { get; set; }

        [JsonPropertyName("amountPaid")]
        public string AmountPaid { get; set; }

        [JsonPropertyName("amountOwed")]
        public string AmountOwed { get; set; }

        [JsonPropertyName("lastPaymentDate")]
        public string LastPaymentDate { get; set; }

        [JsonPropertyName("statusCode")]
        public string StatusCode { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class ShopPaymentHistoryModel
    {
        [JsonPropertyName("dateRecorded")]
        public DateTime DateRecorded { get; set; }

        [JsonPropertyName("payer")]
        public string Payer { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
    }

    public class MonthCalculationModel
    {
        [JsonPropertyName("totalAmt")]
        public string TotalAmt { get; set; }
    }

    public class ShopRepaymentVerificationModel
    {
        [JsonPropertyName("shopNo")]
        public string ShopNo { get; set; }

        [JsonPropertyName("mktId")]
        public string MktId { get; set; }

        [JsonPropertyName("amountPaid")]
        public string AmountPaid { get; set; }

        [JsonPropertyName("amountOwed")]
        public string AmountOwed { get; set; }

        [JsonPropertyName("lastPaymentDate")]
        public string LastPaymentDate { get; set; }

        [JsonPropertyName("shopCategory")]
        public string ShopCategory { get; set; }

        [JsonPropertyName("owner")]
        public string Owner { get; set; }

        [JsonPropertyName("shopAmount")]
        public string ShopAmount { get; set; }

        [JsonPropertyName("market")]
        public string Market { get; set; }

        [JsonPropertyName("lga")]
        public string Lga { get; set; }
    }

    public class ShopNoVerificationModel
    {
        [JsonPropertyName("marketId")]
        public int MarketId { get; set; }

        [JsonPropertyName("shopCat")]
        public string ShopCat { get; set; }

        [JsonPropertyName("shopNo")]
        public string ShopNo { get; set; }

        [JsonPropertyName("amount")]
        public string Amount { get; set; }

        [JsonPropertyName("statusCode")]
        public string StatusCode { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}