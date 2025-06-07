using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AccMgt.Pages.ChartOfAccounts
{
    public class EditModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public EditModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public class ChartOfAccountInput
        {
            public int Id { get; set; }
            public string AccountName { get; set; }
            public int? ParentAccountId { get; set; }
            public bool IsActive { get; set; }
        }

        [BindProperty]
        public ChartOfAccountInput Account { get; set; }

        public List<ChartOfAccountInput> ParentAccounts { get; set; }
        public async Task<IActionResult> OnGetAsync(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using SqlConnection conn = new(connectionString);
            using SqlCommand cmd = new("sp_ManageChartOfAccounts", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Action", "GET_BY_ID");
            cmd.Parameters.AddWithValue("@Id", id);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                Account = new ChartOfAccountInput
                {
                    Id = (int)reader["Id"],
                    AccountName = reader["AccountName"].ToString(),
                    ParentAccountId = reader["ParentAccountId"] as int?,
                    IsActive = (bool)reader["IsActive"]
                };
            }

            await conn.CloseAsync();

            // Load list for dropdown
            ParentAccounts = await LoadParentAccounts(connectionString, id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using SqlConnection conn = new(connectionString);
            using SqlCommand cmd = new("sp_ManageChartOfAccounts", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Action", "UPDATE");
            cmd.Parameters.AddWithValue("@Id", Account.Id);
            cmd.Parameters.AddWithValue("@AccountName", Account.AccountName);
            cmd.Parameters.AddWithValue("@ParentAccountId", (object?)Account.ParentAccountId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", Account.IsActive);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();

            return RedirectToPage("./Index");
        }

        private async Task<List<ChartOfAccountInput>> LoadParentAccounts(string connectionString, int currentId)
        {
            List<ChartOfAccountInput> list = new();
            using SqlConnection conn = new(connectionString);
            using SqlCommand cmd = new("sp_ManageChartOfAccounts", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Action", "GET_ALL");

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int id = (int)reader["Id"];
                if (id != currentId)
                {
                    list.Add(new ChartOfAccountInput
                    {
                        Id = id,
                        AccountName = reader["AccountName"].ToString()
                    });
                }
            }
            await conn.CloseAsync();
            return list;
        }
    }
}
