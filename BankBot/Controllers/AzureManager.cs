using BankBot.DataModels;
using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BankBot.Controllers
{
    public class AzureManager
    {

        private static AzureManager instance;
        private MobileServiceClient client;
        private IMobileServiceTable<BankAccount> accountsTable;

        private AzureManager() {
            this.client = new MobileServiceClient("http://bankbotdb.azurewebsites.net/");
            this.accountsTable = this.client.GetTable<BankAccount>();
        }

        public MobileServiceClient AzureClient {
            get { return client; }
        }

        public static AzureManager AzureManagerInstance {
            get {
                if (instance == null) {
                    instance = new AzureManager();
                }

                return instance;
            }
        }

        private async Task<List<BankAccount>> getAccounts() {
            return await this.accountsTable.ToListAsync();
        }

        public async Task UpdateAccount(BankAccount bankAcc) {
            await this.accountsTable.UpdateAsync(bankAcc);
        }

        public async Task<BankAccount> getAccount(string name) {
            BankAccount account = null;
            List<BankAccount> table = await getAccounts();
            foreach(BankAccount ba in table) {
                if (ba.Name.ToLower() == name.ToLower().Trim()) {
                    account = ba;
                }
            }
            return account;
        }

        public async Task<bool> DoesExist(string name) {
            List<BankAccount> table = await getAccounts();
            foreach (BankAccount ba in table) {
                if (ba.Name.ToLower() == name.ToLower().Trim()) {
                    return true;
                }
            }
            return false;
        }

    }
}