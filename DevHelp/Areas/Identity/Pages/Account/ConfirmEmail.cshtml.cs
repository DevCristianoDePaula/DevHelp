using System.Text;
using DevHelp.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace DevHelp.Areas.Identity.Pages.Account
{
    // Permite acesso anônimo para conclusão do fluxo de confirmação de e-mail.
    [AllowAnonymous]
    public class ConfirmEmailModel(UserManager<ApplicationUser> userManager) : PageModel
    {
        // Gerencia operações de usuário para confirmar o e-mail da conta.
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        // Indica se a confirmação foi concluída com sucesso.
        public bool IsSuccess { get; private set; }

        // Mensagem exibida ao usuário com resultado da confirmação.
        public string StatusMessage { get; private set; } = string.Empty;

        // Processa o token recebido no link de confirmação.
        public async Task<IActionResult> OnGetAsync(string? userId, string? code)
        {
            // Interrompe quando parâmetros essenciais não forem informados.
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            {
                StatusMessage = "Link de confirmação inválido.";
                IsSuccess = false;
                return Page();
            }

            // Busca o usuário dono do link recebido por e-mail.
            var user = await _userManager.FindByIdAsync(userId);

            // Exibe erro quando o usuário não for localizado.
            if (user is null)
            {
                StatusMessage = "Não foi possível localizar a conta para confirmação.";
                IsSuccess = false;
                return Page();
            }

            // Se já confirmado, evita falha por reuso de token e mantém UX positiva.
            if (user.EmailConfirmed)
            {
                StatusMessage = "Seu e-mail já havia sido confirmado. Você já pode entrar no sistema.";
                IsSuccess = true;
                return Page();
            }

            // Decodifica o token URL-safe para o formato esperado pelo Identity.
            var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));

            // Executa confirmação de e-mail com o token decodificado.
            var result = await _userManager.ConfirmEmailAsync(user, decodedCode);

            // Define estado final para renderização da página.
            IsSuccess = result.Succeeded;
            StatusMessage = result.Succeeded
                ? "E-mail confirmado com sucesso. Você será redirecionado para o login em alguns segundos."
                : "Não foi possível confirmar o e-mail. Solicite um novo link de confirmação.";

            // Retorna a página com layout e ações de navegação.
            return Page();
        }
    }
}
