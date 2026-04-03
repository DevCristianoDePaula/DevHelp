using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DevHelp.Areas.Identity.Pages.Account
{
    // Permite acesso anônimo para exibir confirmação da redefinição de senha.
    [AllowAnonymous]
    public class ResetPasswordConfirmationModel : PageModel
    {
        // Renderiza confirmação da redefinição concluída.
        public void OnGet()
        {
        }
    }
}
