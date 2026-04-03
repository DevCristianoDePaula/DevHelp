using DevHelp.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DevHelp.Areas.Identity.Pages.Account
{
    // Permite acesso anônimo para orientar confirmação do cadastro por e-mail.
    [AllowAnonymous]
    public class RegisterConfirmationModel(UserManager<ApplicationUser> userManager) : PageModel
    {
        // Gerencia operações de usuário para confirmar existência de conta.
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        // E-mail que receberá o link de confirmação.
        public string Email { get; private set; } = string.Empty;

        // Carrega os dados da tela de confirmação de cadastro.
        public async Task<IActionResult> OnGetAsync(string? email, string? returnUrl = null)
        {
            // Interrompe quando o e-mail não foi informado na URL.
            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToPage("./Register");
            }

            // Garante que exista usuário com o e-mail informado.
            var user = await _userManager.FindByEmailAsync(email);

            // Redireciona para cadastro quando usuário não for encontrado.
            if (user is null)
            {
                return RedirectToPage("./Register");
            }

            // Disponibiliza e-mail para exibição na interface.
            Email = email;

            // Renderiza a página com instruções ao usuário.
            return Page();
        }
    }
}
