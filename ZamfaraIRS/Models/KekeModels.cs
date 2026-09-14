using System;
using Newtonsoft.Json;

namespace ZamfaraIRS.Models
{
    /// <summary>
    /// Maps the response for Daily Ticket Payments and Permit Renewals.
    /// Endpoint: POST /api/KekeTransactions/Post/v3/KekeTransact
    /// </summary>
    public class KekeTransactionResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("transactionNo")]
        public string TransactionNo { get; set; }

        [JsonProperty("respondCode")]
        public string RespondCode { get; set; } // "00" = Success, "01", "02", "06" = Failures[cite: 2]

        [JsonProperty("responseMessage")]
        public string ResponseMessage { get; set; } // Used to display specific failure reasons[cite: 2]

        [JsonProperty("kekeNo")]
        public string KekeNo { get; set; }

        [JsonProperty("lastPaymentDate")]
        public string LastPaymentDate { get; set; }

        [JsonProperty("expDate")]
        public string ExpDate { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; } // e.g., "Paid 3 day(s) a head."[cite: 2]

        [JsonProperty("lga")]
        public string Lga { get; set; }

        [JsonProperty("agent")]
        public string Agent { get; set; }

        [JsonProperty("superAgent")]
        public string SuperAgent { get; set; }

        [JsonProperty("paymentItem")]
        public string PaymentItem { get; set; }
    }

    /// <summary>
    /// Maps the response for Keke Status and Count checks.
    /// Endpoints: GET GetKekeCount and GET GetKekeStatus
    /// </summary>
    public class KekeStatusResponse
    {
        [JsonProperty("dayDiff")]
        public string DayDiff { get; set; }

        [JsonProperty("lastExpDat")]
        public string LastExpDat { get; set; }

        [JsonProperty("kekeNo")]
        public string KekeNo { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; } // "00" = Not Owing, "01" = Owing, "02"/"04" = Errors[cite: 2]

        [JsonProperty("vehicleType")]
        public string VehicleType { get; set; }

        [JsonProperty("serviceAmt")]
        public string ServiceAmt { get; set; }

        [JsonProperty("serviceName")]
        public string ServiceName { get; set; }

        [JsonProperty("lastPaymentDate")]
        public string LastPaymentDate { get; set; }
    }

    /// <summary>
    /// Maps the transaction history for the UI list.
    /// </summary>
    public class KekeTransactionHistoryModel
    {
        [JsonProperty("payer")]
        public string Payer { get; set; }

        [JsonProperty("dateRecorded")]
        public DateTime DateRecorded { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }
    }
}