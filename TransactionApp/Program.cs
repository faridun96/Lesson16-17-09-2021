using System;
using System.Data.SqlClient;

namespace TransactionApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var conString = "" +
                "Data source=localhost; " +
                "Initial catalog=Transaction; " +
                "Integrated Security=True";
            var working = true;
            while (working)
            {
                Console.Write("0. Exit\n1. Create account\n2. Account list\n3. Transfer from acc to acc\nChoice:");
                int.TryParse(Console.ReadLine(), out var choice);
                switch (choice)
                {
                    case 1:
                        {
                            CreateAccount(conString);
                        }
                        break;
                    case 4:
                        break;
                    case 2:
                        {
                            ListAccount(conString);
                        }
                        break;
                    case 3:
                        {
                            Console.Write("From account:");
                            var fromAcc = Console.ReadLine();

                            Console.Write("To account:");
                            var toAcc = Console.ReadLine();
                            Console.Write("Amount:");
                            decimal.TryParse(Console.ReadLine(), out var amount);

                            TransferFromToAcc(fromAcc, toAcc, amount, conString);
                        }
                        break;
                    case 5:
                        {
                            Console.Write("Account:");
                            var account = Console.ReadLine();
                            var balance = GetAccountBalance(conString, account);
                            Console.WriteLine($"Account balance: {balance}");
                        }
                        break;
                    case 0:
                        working = false;
                        break;
                    default:
                        Console.WriteLine("Wrong command.");
                        break;
                }
                Console.WriteLine("Press any key...");
                Console.ReadLine();
                Console.Clear();
            }
        }

        private static decimal GetAccountBalance(string conString, string account)
        {
            var conn = new SqlConnection(conString);
            conn.Open();
            var command = conn.CreateCommand();
            command.CommandText = "select sum( case when t.Amount * -1 else t.Amount end) from Transactions t left join Account a on t.Account_Id = a.Id where a.Is_Active = @fromAcc";
            command.Parameters.AddWithValue("@fromAcc", account);
            var reader = command.ExecuteReader();
            var fromAccBalance = 0m;

            while (reader.Read())
            {
                fromAccBalance = !string.IsNullOrEmpty(reader.GetValue(0)?.ToString()) ? reader.GetDecimal(0) : 0;
            }

            reader.Close();
            command.Parameters.Clear();

            conn.Close();
            return fromAccBalance;
        }

        private static void TransferFromToAcc(string fromAcc, string toAcc, decimal amount, string conString)
        {
            if (string.IsNullOrEmpty(fromAcc) || string.IsNullOrEmpty(toAcc) || amount == 0)
            {
                Console.WriteLine("Something went wrong.");
                return;
            }

            var conn = new SqlConnection(conString);
            conn.Open();

            if (!(conn.State == System.Data.ConnectionState.Open))
            {
                return;
            }

            SqlTransaction sqlTransaction = conn.BeginTransaction();

            var command = conn.CreateCommand();

            command.Transaction = sqlTransaction;

            try
            {
                var fromAccBalance = GetAccountBalance(conString, fromAcc);

                if (fromAccBalance <= 0 || (fromAccBalance - amount) < 0)
                {
                    throw new Exception("From account balance not enough amount");
                }

                var fromAccId = GetAccountId(fromAcc, conString);

                if (fromAccId == 0)
                {
                    throw new Exception("Account not found");
                }

                command.CommandText = "INSERT INTO [dbo].[Transactions]([Amount],[Created_At] ,[Account_Id]) VALUES (@amount , @createdAt, @accountId)";
                command.Parameters.AddWithValue("@amount", amount);
                command.Parameters.AddWithValue("@createdAt", DateTime.Now);
                command.Parameters.AddWithValue("@accountId", fromAccId);

                var result1 = command.ExecuteNonQuery();

                var toAccId = GetAccountId(toAcc, conString);

                if (toAccId == 0)
                {
                    throw new Exception("Account not found");
                }

                command.Parameters.Clear();

                command.CommandText = "INSERT INTO [dbo].[Transactions]([Amount] ,[Created_At] ,[Account_Id]) VALUES (@amount , @createdAt, @accountId)";
                command.Parameters.AddWithValue("@amount", amount);
                command.Parameters.AddWithValue("@createdAt", DateTime.Now);
                command.Parameters.AddWithValue("@accountId", toAccId);

                var result2 = command.ExecuteNonQuery();

                if (result1 == 0 || result2 == 0)
                {
                    throw new Exception("Something went wrong");
                }

                sqlTransaction.Commit();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                sqlTransaction.Rollback();
            }
            finally
            {
                conn.Close();
            }
        }

        private static int GetAccountId(string number, string conString)
        {
            var accNumber = 0;
            var connection = new SqlConnection(conString);
            var query = "SELECT [Id] FROM [dbo].[Account] WHERE [Is_Active] = @number";

            var command = connection.CreateCommand();
            command.Parameters.AddWithValue("@number", number);
            command.CommandText = query;

            connection.Open();

            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                accNumber = reader.GetInt32(0);
            }
            connection.Close();
            reader.Close();

            return accNumber;
        }

        private static void CreateAccount(string conString)
        {
            var client = new Client { Account = "test1", Is_Active = 1, Created_At = DateTime.Now, Updated_At = DateTime.Now };

            var connection = new SqlConnection(conString);
            var query = "INSERT INTO [dbo].[Account]([Account] ,[Is_Active] ,[Created_At], [Updated_At]) VALUES (@account ,@is_active ,@created_at ,@updated_at)";

            var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("@account", client.Account);
            command.Parameters.AddWithValue("@is_active", client.Is_Active);
            command.Parameters.AddWithValue("@created_at", client.Created_At);
            command.Parameters.AddWithValue("@updated_at", client.Updated_At);

            connection.Open();

            var result = command.ExecuteNonQuery();



            if (result > 0)
            {
                Console.WriteLine("Added successfully.");
            }

            connection.Close();
        }

        private static void ListAccount(string conString)
        {
            Client[] clients = new Client[0];

            var connection = new SqlConnection(conString);
            var query = "SELECT [Id] ,[Account] ,[Is_Active] ,[Created_At] ,[Updated_At] FROM [dbo].[Account]";

            var command = connection.CreateCommand();
            command.CommandText = query;

            connection.Open();

            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var client = new Client { };

                client.Id = int.Parse(reader["Id"].ToString());
                client.Account = reader["Acount"].ToString();
                var y = reader["Is_Active"]?.ToString();
                var x = reader["Created_At"]?.ToString();
                client.Created_At = !string.IsNullOrEmpty(reader["Created_At"]?.ToString()) ? DateTime.Parse(reader["Created_At"].ToString()) : null;
                client.Updated_At = !string.IsNullOrEmpty(reader["Updated_At"]?.ToString()) ? DateTime.Parse(reader["Updated_At"].ToString()) : null;
                AddClient(ref clients, client);
            }
            connection.Close();
            foreach (var client in clients)
            {
                Console.WriteLine($"ID:{client.Id}, Account:{client.Account}, Is_Active:{client.Is_Active}, Created_At:{client.Created_At}, Updated_At:{client.Updated_At}");
          
            }
        }

        private static void AddClient(ref Client[] clients, Client client)
        {
            if (clients == null)
            {
                return;
            }

            Array.Resize(ref clients, clients.Length + 1);

            clients[clients.Length - 1] = client;
        }
    }

    public class Client
    {
        public int Id { get; set; }
        public string Account { get; set; }
        public int Is_Active { get; set; }
        public DateTime? Created_At { get; set; }
        public DateTime? Updated_At { get; set; }
    }

    public class Account
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public int ClientId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class Transaction
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Account_Id { get; set; }
    }
}