using System.ComponentModel.DataAnnotations.Schema;

namespace AccountServer.DB
{
    public class DataModel
    {
        [Table("Account")]
        public class AccountDb
        {
            public int AccountDbId { get; set; }
            public string AccountName { get; set; }
            public string Passwrd { get; set; }

        }
    }
}
