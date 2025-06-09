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
        private readonly UserManager<IdentityUser> _userManager;

        public IndexModel(IConfiguration configuration, UserManager<IdentityUser> userManager)
        {
            _configuration = configuration;
            _userManager = userManager;
        }
        public class AccountViewModel
        {
            public int Id { get; set; }
            public string? AccountName { get; set; }
            public int? ParentAccountId { get; set; }
            public bool IsActive { get; set; }
            public decimal Balance { get; set; }
            public string? UserId { get; set; }
        }
        public List<AccountViewModel> Accounts { get; set; } = new List<AccountViewModel>();
        public async Task OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated)
            {
                RedirectToPage("/Identity/Account/Login");
                return;
            }
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            using (SqlCommand cmd = new SqlCommand("sp_ManageChartOfAccounts", conn))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "GET_ALL");

                await conn.OpenAsync();
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Accounts.Add(new AccountViewModel
                        {
                            Id = (int)reader["Id"],
                            AccountName = reader["AccountName"].ToString(),
                            ParentAccountId = reader["ParentAccountId"] as int?,
                            IsActive = (bool)reader["IsActive"],
                            UserId = reader["UserId"] as string
                        });
                    }
                    await reader.CloseAsync();
                }

                foreach (var account in Accounts)
                {
                    using SqlCommand balanceCmd = new("sp_SaveVoucher", conn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    balanceCmd.Parameters.AddWithValue("@Action", "GET_BALANCE_BY_ACCOUNT");
                    balanceCmd.Parameters.AddWithValue("@AccountId", account.Id);

                    using var balanceReader = await balanceCmd.ExecuteReaderAsync();
                    if (await balanceReader.ReadAsync())
                    {
                        account.Balance = balanceReader["Balance"] is DBNull ? 0 : (decimal)balanceReader["Balance"];
                    }

                    await balanceReader.CloseAsync();
                }
                await conn.CloseAsync();
            }

            if (User.IsInRole("Viewer"))
            {
                var user = await _userManager.GetUserAsync(User);

                var mainAcc = Accounts.Where(a => a.UserId == user.Id).ToList();

                var childAcc = Accounts
                    .Where(a => a.ParentAccountId.HasValue && mainAcc.Any(ma => ma.Id == a.ParentAccountId.Value))
                    .ToList();

                Accounts = new List<AccountViewModel>();
                Accounts.AddRange(mainAcc);
                Accounts.AddRange(childAcc);
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
