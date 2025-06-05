using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;

namespace AccMgt.Pages.ChartOfAccounts
{
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

        public class InputModel
        {
            [Required]
            [Display(Name = "Account Name")]
            public string AccountName { get; set; }

            [Display(Name = "Parent Account")]
            public int? ParentAccountId { get; set; }

            [Display(Name = "Is Active")]
            public bool IsActive { get; set; } = true;
        }
        public void OnGet()
        {
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
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    conn.Open();
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            return RedirectToPage("/ChartOfAccounts/Index"); // Or wherever you list accounts
        }

    }
}
