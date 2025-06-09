using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace AccMgt.Pages.ChartOfAccounts
{
    [Authorize(Roles = "Admin,Accountant")]
    public class EditModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<IdentityUser> _userManager;

        public EditModel(IConfiguration configuration, UserManager<IdentityUser> userManager)
        {
            _configuration = configuration;
            _userManager = userManager;

        }

        public class ChartOfAccountInput
        {
            public int Id { get; set; }
            [Required]
            public string AccountName { get; set; }
            public int? ParentAccountId { get; set; }
            public bool IsActive { get; set; }
        }

        [BindProperty]
        public ChartOfAccountInput Account { get; set; }

        public List<SelectListItem> ParentAccounts { get; set; }
        public async Task<IActionResult> OnGetAsync(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;
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
                    AccountName = reader["AccountName"].ToString()!,
                    ParentAccountId = reader["ParentAccountId"] as int?,
                    IsActive = (bool)reader["IsActive"]
                };
            }
            else
            {
                await conn.CloseAsync();
                return NotFound();
            }

            await conn.CloseAsync();

            ParentAccounts = await LoadParentAccounts(connectionString!, id);
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

        private async Task<List<SelectListItem>> LoadParentAccounts(string connectionString, int currentAccountId)
        {
            List<SelectListItem> list = new()
    {
        new SelectListItem { Text = "Main Account", Value = "" }
    };

            using SqlConnection conn = new(connectionString);
            using SqlCommand cmd = new("sp_ManageChartOfAccounts", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Action", "GET_ALL");

            await conn.OpenAsync();
            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    int id = Convert.ToInt32(reader["Id"]);
                    string accountName = reader["AccountName"].ToString();

                    if (id == currentAccountId)
                        continue;

                    list.Add(new SelectListItem
                    {
                        Value = id.ToString(),
                        Text = $"{id} - {accountName}"
                    });
                }
            }

            return list;
        }

    }
}
