using System.ComponentModel.DataAnnotations;
using System.Text;
using DevHelp.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace DevHelp.Areas.Identity.Pages.Account
{
    // Permite acesso anônimo para solicitar novo envio de confirmação de e-mail.
    [AllowAnonymous]
    public class ResendEmailConfirmationModel(
        UserManager<ApplicationUser> userManager,
        IEmailSender<ApplicationUser> emailSender) : PageModel
    {
        // Gerencia operações relacionadas ao usuário no Identity.
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        // Serviço de envio de e-mails de confirmação.
        private readonly IEmailSender<ApplicationUser> _emailSender = emailSender;

        // Exibe mensagens de sucesso/erro na interface.
        [TempData]
        public string? StatusMessage { get; set; }

        // Dados recebidos do formulário de reenvio.
        [BindProperty]
        public InputModel Input { get; set; } = new();

        // Estrutura de dados do formulário.
        public class InputModel
        {
            // E-mail da conta que receberá novo link de confirmação.
            [Required(ErrorMessage = "O e-mail é obrigatório.")]
            [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
            [Display(Name = "E-mail")]
            public string Email { get; set; } = string.Empty;
        }

        // Carrega a página com e-mail opcional vindo da query string.
        public void OnGet(string? email = null)
        {
            // Pré-preenche o campo quando houver e-mail informado na URL.
            if (!string.IsNullOrWhiteSpace(email))
            {
                Input.Email = email;
            }
        }

        // Processa solicitação de novo envio de confirmação.
        public async Task<IActionResult> OnPostAsync()
        {
            // Mantém a página quando houver erros de validação.
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Tenta localizar usuário pelo e-mail informado.
            var user = await _userManager.FindByEmailAsync(Input.Email);

            // Retorna mensagem neutra quando conta não existir.
            if (user is null)
            {
                StatusMessage = "Se o e-mail estiver cadastrado, um novo link de confirmação será enviado.";
                return RedirectToPage();
            }

            // Se já confirmado, evita envio desnecessário e orienta login.
            if (user.EmailConfirmed)
            {
                StatusMessage = "Este e-mail já está confirmado. Você já pode entrar no sistema.";
                return RedirectToPage("./Login");
            }

            // Gera token de confirmação e converte para formato URL-safe.
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            // Monta a URL de confirmação para o usuário.
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, code },
                protocol: Request.Scheme);

            // Envia o novo link de confirmação sem dupla codificação.
            await _emailSender.SendConfirmationLinkAsync(user, Input.Email, callbackUrl!);

            // Confirma o envio e mantém o usuário na mesma tela.
            StatusMessage = "Novo link de confirmação enviado. Verifique sua caixa de entrada.";
            return RedirectToPage(new { email = Input.Email });
        }
    }
}
