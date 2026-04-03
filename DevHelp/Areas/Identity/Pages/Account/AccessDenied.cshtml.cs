using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DevHelp.Areas.Identity.Pages.Account
{
    // Exibe página amigável quando usuário tenta acessar recurso sem permissão.
    [AllowAnonymous]
    public class AccessDeniedModel : PageModel
    {
        // Renderiza página de acesso negado com visual padrão do projeto.
        public void OnGet()
        {
        }
    }
}
