using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AccMgt.Pages.ChartOfAccounts
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public class AccountViewModel
        {
            public int Id { get; set; }
            public string? AccountName { get; set; }
            public int? ParentAccountId { get; set; }
            public bool IsActive { get; set; }
        }
        public List<AccountViewModel> Accounts { get; set; } = new List<AccountViewModel>();
        public async Task OnGetAsync()
        {
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            using (SqlCommand cmd = new SqlCommand("sp_ManageChartOfAccounts", conn))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "GET_ALL");

                conn.Open();
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Accounts.Add(new AccountViewModel
                        {
                            Id = (int)reader["Id"],
                            AccountName = reader["AccountName"].ToString(),
                            ParentAccountId = reader["ParentAccountId"] as int?,
                            IsActive = (bool)reader["IsActive"]
                        });
                    }
                }
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {

            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;
            using SqlConnection conn = new(connectionString);
            using SqlCommand cmd = new("sp_ManageChartOfAccounts", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Action", "DELETE");
            cmd.Parameters.AddWithValue("@Id", id);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();

            return RedirectToPage("Index");
        }
    }
}
