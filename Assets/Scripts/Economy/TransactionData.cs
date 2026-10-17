using System;
using GameBalap.Core;

namespace GameBalap.Economy
{
    [System.Serializable]
    public class TransactionData
    {
        public string id;
        public string timestamp;
        public TransactionType type;
        public int amount; // Positif untuk kredit (+), negatif untuk debit (-)
        public int balanceAfter;
        public string description;
        public string raceId;

        public TransactionData() { }

        public TransactionData(TransactionType type, int amount, int balanceAfter, string description, string raceId = "")
        {
            this.id = Guid.NewGuid().ToString().Substring(0, 8);
            this.timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            this.type = type;
            this.amount = amount;
            this.balanceAfter = balanceAfter;
            this.description = description;
            this.raceId = raceId;
        }
    }
}
