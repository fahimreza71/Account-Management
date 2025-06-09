using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace AccMgt.Pages.Vouchers
{
    [Authorize(Roles = "Admin,Accountant")]
    public class EditModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public EditModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public VoucherInput Voucher { get; set; } = new();

        public class VoucherInput
        {
            public int VoucherId { get; set; }
            [Required]
            public string? VoucherType { get; set; }
            [Required]
            public int AccountId { get; set; }
            [Range(0, double.MaxValue)]
            public decimal Debit { get; set; }
            [Range(0, double.MaxValue)]
            public decimal Credit { get; set; }
            public string Narration { get; set; }
        }
        public List<SelectListItem> VoucherTypes { get; set; } = new()
        {
            new SelectListItem("Journal", "Journal"),
            new SelectListItem("Payment", "Payment"),
            new SelectListItem("Receipt", "Receipt")
        };
        public List<SelectListItem> Accounts { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id.HasValue)
            {
                Voucher.AccountId = id.Value;
            }
            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;

            using SqlConnection conn = new(connectionString);
            await conn.OpenAsync();

            using SqlCommand cmd = new("sp_SaveVoucher", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Action", "GET_BY_ID");
            cmd.Parameters.AddWithValue("@VoucherId", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                Voucher = new VoucherInput
                {
                    VoucherId = (int)reader["VoucherId"],
                    VoucherType = reader["VoucherType"].ToString()!,
                    AccountId = (int)reader["AccountId"],
                    Debit = (decimal)reader["Debit"],
                    Credit = (decimal)reader["Credit"],
                    Narration = reader["Narration"].ToString()!
                };
            }
            else
            {
                return NotFound();
            }

            await reader.CloseAsync();
            await LoadAccountsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;
            using SqlConnection conn = new(connectionString);
            await conn.OpenAsync();

            using SqlCommand cmd = new("sp_SaveVoucher", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Action", "UPDATE");
            cmd.Parameters.AddWithValue("@VoucherId", Voucher.VoucherId);
            cmd.Parameters.AddWithValue("@VoucherType", Voucher.VoucherType ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@AccountId", Voucher.AccountId);
            cmd.Parameters.AddWithValue("@Debit", Voucher.Debit);
            cmd.Parameters.AddWithValue("@Credit", Voucher.Credit);
            cmd.Parameters.AddWithValue("@Narration", Voucher.Narration ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@UserId", DBNull.Value); // Not updating user

            await cmd.ExecuteNonQueryAsync();

            return RedirectToPage("Index", new { accountId = Voucher.AccountId });
        }

        private async Task LoadAccountsAsync()
        {
            var connStr = _configuration.GetConnectionString("DefaultConnection");
            using SqlConnection conn = new(connStr);
            using SqlCommand cmd = new("sp_ManageChartOfAccounts", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Action", "GET_ALL");

            await conn.OpenAsync();
            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Accounts.Add(new SelectListItem
                {
                    Value = reader["Id"].ToString(),
                    Text = $"{reader["Id"]} - {reader["AccountName"]}"
                });
            }
            await conn.CloseAsync();
        }
    }
}
