using AccMgt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AccMgt.Pages.ChartOfAccounts
{
    [Authorize(Roles = "Admin,Accountant")]
    public class CreateModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public CreateModel(UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        [BindProperty]
        public InputModel Input { get; set; }
        public List<SelectListItem> ParentAccounts { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> UsersList { get; set; } = new List<SelectListItem>();

        public class InputModel
        {
            [Required]
            [Display(Name = "Account Name")]
            public string AccountName { get; set; }

            [Display(Name = "Parent Account")]
            public int? ParentAccountId { get; set; }

            [Display(Name = "Is Active")]
            public bool IsActive { get; set; } = true;
            [Required]
            public string UserId { get; set; }
        }
        public async Task OnGetAsync()
        {
            ParentAccounts.Add(new SelectListItem { Text = "Main Account", Value = "" });

            var userId = _userManager.GetUserId(User);
            var users = await _userManager.Users.ToListAsync();

            UsersList = users.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = u.Email
            }).ToList();

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
                        ParentAccounts.Add(new SelectListItem
                        {
                            Value = reader["Id"].ToString(),
                            Text = $"{reader["Id"]} - {reader["AccountName"]}"
                        });
                    }
                }
            }
        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var userId = _userManager.GetUserId(User);

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                using (SqlCommand cmd = new SqlCommand("sp_ManageChartOfAccounts", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "INSERT");
                    cmd.Parameters.AddWithValue("@AccountName", Input.AccountName);
                    cmd.Parameters.AddWithValue("@ParentAccountId", (object?)Input.ParentAccountId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", Input.IsActive);
                    cmd.Parameters.AddWithValue("@UserId", Input.UserId);

                    conn.Open();
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            return RedirectToPage("/ChartOfAccounts/Index");
        }

    }
}
