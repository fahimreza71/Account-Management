using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace AccMgt.Pages.Vouchers
{
    [Authorize(Roles = "Admin,Accountant")]
    public class CreateModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<IdentityUser> _userManager;

        public CreateModel(IConfiguration configuration, UserManager<IdentityUser> userManager)
        {
            _configuration = configuration;
            _userManager = userManager;
        }

        public class VoucherInputModel
        {
            [Required]
            public string VoucherType { get; set; } = "";

            [Required]
            public int AccountId { get; set; }

            [Range(0, double.MaxValue)]
            public decimal Debit { get; set; }

            [Range(0, double.MaxValue)]
            public decimal Credit { get; set; }

            public string? Narration { get; set; }
        }

        [BindProperty]
        public VoucherInputModel Voucher { get; set; } = new();

        public List<SelectListItem> VoucherTypes { get; set; } = new()
        {
            new SelectListItem("Journal", "Journal"),
            new SelectListItem("Payment", "Payment"),
            new SelectListItem("Receipt", "Receipt")
        };

        public List<SelectListItem> Accounts { get; set; } = new();
        public async Task<IActionResult> OnGetAsync(int? accountId)
        {
            if (accountId.HasValue)
            {
                Voucher.AccountId = accountId.Value;
            }
            await LoadAccountsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadAccountsAsync();
                return Page();
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var connStr = _configuration.GetConnectionString("DefaultConnection");
            using SqlConnection conn = new(connStr);
            using SqlCommand cmd = new("sp_SaveVoucher", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Action", "INSERT");
            cmd.Parameters.AddWithValue("@VoucherType", Voucher.VoucherType);
            cmd.Parameters.AddWithValue("@AccountId", Voucher.AccountId);
            cmd.Parameters.AddWithValue("@Debit", Voucher.Debit);
            cmd.Parameters.AddWithValue("@Credit", Voucher.Credit);
            cmd.Parameters.AddWithValue("@Narration", (object?)Voucher.Narration ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserId", userId);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();

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
