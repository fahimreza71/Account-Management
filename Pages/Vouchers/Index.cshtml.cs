using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AccMgt.Pages.Vouchers
{
    [Authorize(Roles = "Admin,Accountant,Viewer")]
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public class VoucherViewModel
        {
            public int VoucherId { get; set; }
            public string VoucherType { get; set; } = "";
            public int AccountId { get; set; }
            public string AccountName { get; set; } = "";
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
            public string FullName { get; set; } = "";
            public string Narration { get; set; } = "";
            public DateTime CreatedAt { get; set; }
        }
        public List<VoucherViewModel> Vouchers { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? AccountId { get; set; }
        public async Task<IActionResult> OnGetAsync()
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection")!;
            using SqlConnection conn = new(connStr);
            using SqlCommand cmd = new("sp_SaveVoucher", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Action", "GET_ALL");

            await conn.OpenAsync();
            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int rowAccountId = (int)reader["AccountId"];
                if (AccountId == null || rowAccountId == AccountId)
                {
                    Vouchers.Add(new VoucherViewModel
                    {
                        VoucherId = (int)reader["VoucherId"],
                        VoucherType = reader["VoucherType"].ToString()!,
                        AccountId = rowAccountId,
                        AccountName = reader["AccountName"].ToString()!,
                        Debit = (decimal)reader["Debit"],
                        Credit = (decimal)reader["Credit"],
                        FullName = reader["FullName"].ToString()!,
                        Narration = reader["Narration"].ToString()!,
                        CreatedAt = (DateTime)reader["CreatedAt"]
                    });
                }
            }
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, int accountId)
        {

            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;
            using SqlConnection conn = new(connectionString);
            using SqlCommand cmd = new("sp_SaveVoucher", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Action", "DELETE");
            cmd.Parameters.AddWithValue("@VoucherId", id);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();

            return RedirectToPage("/Vouchers/Index", new { accountId = accountId });
        }
    }
}
