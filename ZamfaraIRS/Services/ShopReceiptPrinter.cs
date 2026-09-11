using Android.Accounts;
using System;
using System.Threading.Tasks;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;
namespace ZamfaraIRS.Services
{
    public static class ShopReceiptPrinter
    {
        public static async Task<bool> PrintShopRegistrationReceiptAsync(
            string businessName,
            string payerId,
            string marketName,
            string agentEmail,
            bool isReprint = false)
        {
            var receipt = ReceiptPrinter.CreateBrandedReceipt(); 
            receipt.ReceiptBannerText = isReprint ? "SHOP REGISTRATION (REPRINT)" : "SHOP REGISTRATION";
            receipt.ReceiptNumber = payerId; 
            receipt.AgentName = MainPage.Name; 
            receipt.CollectionPoint = marketName; 
            receipt.BarcodeLabel = $"{BrandConfig.VerifyReceiptUrl}{payerId}"; 

            if (isReprint)
            {
                receipt.FooterLine1 = "*** REPRINTED RECEIPT ***"; 
                receipt.FooterLine2 = $"Reprinted: {DateTime.Now:dd MMM yyyy HH:mm} | POWERED BY OSOFTPAY"; 
            }

            receipt.Items.Add(new ReceiptItem
            {
                Description = "Shop Registration",
                Amount = 0m,
                SubText = businessName
            });

            receipt.Items.Add(new ReceiptItem
            {
                Description = "Payer ID / Shop Code",
                Amount = 0m,
                SubText = payerId
            });

            receipt.Items.Add(new ReceiptItem
            {
                Description = "Market Complex",
                Amount = 0m,
                SubText = marketName
            });

            receipt.Items.Add(new ReceiptItem
            {
                Description = "Date Recorded",
                Amount = 0m,
                SubText = DateTime.Now.ToString("dd MMM yyyy HH:mm")
            });

            return await ReceiptPrinter.PrintAsync(receipt); 
        }

        public static async Task<bool> PrintShopPaymentReceiptAsync(
            string refNo,
            string shopNo,
            string marketName,
            string occupantName,
            string categoryName,
            decimal amountPaid,
            decimal balanceRemaining,
            string agentEmail,
            bool isReprint = false)
        {
            var receipt = ReceiptPrinter.CreateBrandedReceipt(); 
            receipt.ReceiptBannerText = isReprint ? "SHOP REPAYMENT (REPRINT)" : "OFFICIAL REPAYMENT RECEIPT"; 
            receipt.ReceiptNumber = refNo; 
            receipt.AgentName = MainPage.Name; 
            receipt.CollectionPoint = marketName; 
            receipt.TotalAmount = amountPaid + balanceRemaining; 
            receipt.AmountPaid = amountPaid; 
            receipt.AmountLeft = balanceRemaining; 
            receipt.BarcodeLabel = $"{BrandConfig.VerifyReceiptUrl}{refNo}"; 

            if (isReprint)
            {
                receipt.FooterLine1 = "*** REPRINTED RECEIPT ***"; 
                receipt.FooterLine2 = $"Reprinted: {DateTime.Now:dd MMM yyyy HH:mm} | POWERED BY OSOFTPAY"; 
            }

            receipt.Items.Add(new ReceiptItem
            {
                Description = "MARKET",
                Amount = 0m,
                SubText = marketName
            });
   
            receipt.Items.Add(new ReceiptItem
            {
                Description = "SHOP NO",
                Amount = 0m,
                SubText = shopNo
            });

            receipt.Items.Add(new ReceiptItem
            {
                Description = "OCCUPANT",
                Amount = 0m,
                SubText = occupantName
            });

            receipt.Items.Add(new ReceiptItem
            {
                Description = $"SHOP RENT: {categoryName}",
                Amount = amountPaid
            });


            receipt.Items.Add(new ReceiptItem
            {
                Description = "DATE",
                Amount = 0m,
                SubText = DateTime.Now.ToString("dd MMM yyyy HH:mm")
            });

            return await ReceiptPrinter.PrintAsync(receipt); 
        }

        public static async Task<bool> PrintPaymentReceiptAsync(
             string shopNo,
             string marketName,
             string occupantName,
             decimal amountPaid,
             decimal balanceRemaining,
             string refNo, string agentEmail,
             string date,
             bool isReprint = false)
        {
            var receipt = ReceiptPrinter.CreateBrandedReceipt(); 
            receipt.ReceiptBannerText = isReprint ? "SHOP PAYMENT (REPRINT)" : "OFFICIAL PAYMENT RECEIPT";
            receipt.ReceiptNumber = refNo; 
            receipt.AgentName = agentEmail ?? "Agent"; 
            receipt.CollectionPoint = marketName; 
            receipt.TotalAmount = amountPaid + balanceRemaining; 
            receipt.AmountPaid = amountPaid; 
            receipt.AmountLeft = balanceRemaining; 
            receipt.BarcodeLabel = $"{BrandConfig.VerifyReceiptUrl}{refNo}"; 

            if (isReprint)
            {
                receipt.FooterLine1 = "*** REPRINTED RECEIPT ***";
                receipt.FooterLine2 = $"Reprinted: {DateTime.Now:dd MMM yyyy HH:mm} | POWERED BY OSOFTPAY"; 
            }

            receipt.Items.Add(new ReceiptItem
            {
                Description = "MARKET",
                Amount = 0m,
                SubText = marketName
            });
            receipt.Items.Add(new ReceiptItem
            {
                Description = "TENANT",
                Amount = 0m,
                SubText = occupantName
            });
            receipt.Items.Add(new ReceiptItem
            {
                Description = "SHOP NO",
                Amount = 0m,
                SubText = shopNo
            });


            receipt.Items.Add(new ReceiptItem
            {
                Description = "SHOP PAYMENT",
                Amount = amountPaid
            });

        
            receipt.Items.Add(new ReceiptItem
            {
                Description = "DATE",
                Amount = 0m,
                SubText = date
            });

            return await ReceiptPrinter.PrintAsync(receipt); 
        }

      
    }
}
    
