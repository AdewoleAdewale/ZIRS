using Android.Accounts;
using System;
using System.Collections.Generic;
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
            receipt.BarcodeLabel = $"https://zamfara.osoftpay.net/SingleCollections/Verify?TransactId={payerId}";

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
            receipt.BarcodeLabel = $"https://zamfara.osoftpay.net/SingleCollections/Verify?TransactId={refNo}";

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
            receipt.BarcodeLabel = $"https://zamfara.osoftpay.net/SingleCollections/Verify?TransactId={refNo}";

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

        //public static async Task<bool> PrintKekeReceiptAsync(
        //      string transactionNo,
        //      string vehiclePlateNo,
        //      string serviceName,
        //      decimal amountPaid,
        //      string lga,
        //      string agentEmail,
        //      bool isReprint = false)
        //{
        //    // Note: Assuming ReceiptPrinter is your underlying ESC/POS or native SDK wrapper
        //    var receipt = ReceiptPrinter.CreateBrandedReceipt();
        //    if (receipt == null) return false;

        //    receipt.StoreName = "ZAMFARA STATE INTERNAL REVENUE SERVICE";
        //    receipt.StoreSubTitle = isReprint ? "KEKE PERMIT (REPRINT)" : "KEKE / TRICYCLE TICKET";
        //    receipt.ReceiptNumber = transactionNo;
        //    receipt.AgentName = MainPage.Name;
        //    receipt.CollectionPoint = lga;
        //    receipt.PrintDate = DateTime.Now;
        //    receipt.AmountPaid = amountPaid;
        //    receipt.TotalAmount = amountPaid;

        //    receipt.Items = new List<ReceiptItem>
        //    {
             
        //        new ReceiptItem { Description = "SERVICE NAME", SubText = serviceName },
        //        new ReceiptItem { Description = "BODY NO:", SubText = vehiclePlateNo, Amount = 0 },
        //         new ReceiptItem { Description = "AMOUNT", Amount = receipt.AmountPaid },
        //    };
        //    receipt.BarcodeLabel = $"https://zamfara.osoftpay.net/SingleCollections/Verify?TransactId={transactionNo}";
        //    receipt.FooterLine1 = isReprint ? "*** REPRINTED RECEIPT ***" : "Status: APPROVED SUCCESSFUL";
        //    receipt.FooterLine2 = "POWERED BY OSOFTPAY";

        //    await ReceiptPrinter.PrintAsync(receipt);
        //    return true;
        //}

        // In ShopReceiptPrinter.cs
        public static async Task<bool> PrintKekeReceiptAsync(
            string transactionNo, string vehiclePlateNo, string serviceName,
            decimal amountPaid, string lga, string agentEmail, bool isReprint = false)
        {
            var receipt = ReceiptPrinter.CreateBrandedReceipt();
            if (receipt == null) return false;

            // Standard properties remain unchanged, retaining all other receipt details
            receipt.StoreName = "ZAMFARA STATE INTERNAL REVENUE SERVICE";
            receipt.StoreSubTitle = isReprint ? "KEKE PERMIT (REPRINT)" : "KEKE / TRICYCLE TICKET";
            receipt.ReceiptNumber = transactionNo;
            receipt.AgentName = agentEmail;
            receipt.CollectionPoint = lga;
            receipt.PrintDate = DateTime.Now;
            receipt.AmountPaid = amountPaid;
            receipt.TotalAmount = amountPaid;

            receipt.Items = new List<ReceiptItem>
    {
        new ReceiptItem { Description = serviceName, Amount = amountPaid },
        new ReceiptItem { Description = "Vehicle Plate No", SubText = vehiclePlateNo, Amount = 0 }
    };

            // The barcode string is correctly mapped here
            receipt.BarcodeLabel = $"https://zamfara.osoftpay.net/SingleCollections/Verify?TransactId={transactionNo}";
            receipt.FooterLine1 = isReprint ? "*** REPRINTED RECEIPT ***" : "Status: APPROVED SUCCESSFUL";
            receipt.FooterLine2 = "POWERED BY OSOFTPAY";

            try
            {
                // Route through the watermark engine instead of the generic queue
                var printer = new BluetoothPrinterService(use80mm: false);
                var result = await printer.PrintReceiptWithWatermarkAsync(
                    receipt,
                    watermarkText: "ZIRS",
                    logoAssetName: "Logo.png"
                );
                return result.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        //public static async Task<bool> PrintTestReceiptAsync()
        //{
        //    try
        //    {
        //        var receipt = ReceiptPrinter.CreateBrandedReceipt();
        //        if (receipt == null) return false;

        //        receipt.StoreName = "PRINTER TEST";
        //        receipt.StoreSubTitle = "TEST SPEED & CONNECTION";
        //        receipt.ReceiptNumber = "TEST-" + DateTime.Now.Ticks.ToString().Substring(0, 8);
        //        receipt.PrintDate = DateTime.Now;
        //        receipt.AmountPaid = 0;
        //        receipt.TotalAmount = 0;
        //        receipt.FooterLine1 = "PRINTER IS CONFIGURED CORRECTLY";

        //        await ReceiptPrinter.PrintAsync(receipt);
        //        return true;
        //    }
        //    catch (Exception)
        //    {
        //        // Silently catch OS-level Bluetooth crashes and return false to the UI
        //        return false;
        //    }
        //}


        // In ShopReceiptPrinter.cs
        public static async Task<bool> TestKekeWatermarkPrintAsync()
        {
            return await PrintKekeReceiptAsync(
                transactionNo: "TEST-KK-" + DateTime.Now.Ticks.ToString().Substring(0, 6),
                vehiclePlateNo: "ZAM-123-KK",
                serviceName: "TEST KEKE TICKET",
                amountPaid: 150m,
                lga: "TEST LGA",
                agentEmail: "testagent@zirs.gov.ng",
                isReprint: false
            );
        }
    }
}
    
