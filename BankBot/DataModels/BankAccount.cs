using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BankBot.DataModels
{
    public class BankAccount
    {

        [JsonProperty(PropertyName = "Id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "Name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "createdAt")]
        public DateTime Date { get; set; }

        [JsonProperty(PropertyName = "accountTypes")]
        public List<string> Types { get; set; }

        [JsonProperty(PropertyName = "accountAmount")]
        public Dictionary<string, double> Amounts { get; set; }

        [JsonProperty(PropertyName = "accountNumbers")]
        public Dictionary<string, string> AccountNumbers { get; set; }

        [JsonProperty(PropertyName = "payeeAccounts")]
        public List<string> Payees { get; set; }

    }
}