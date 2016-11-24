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

        [JsonProperty(PropertyName = "Amount")]
        public double Amount { get; set; }

        [JsonProperty(PropertyName = "accountNumbers")]
        public string AccountNumber { get; set; }

    }
}