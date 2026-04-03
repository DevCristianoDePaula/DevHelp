using System.ComponentModel.DataAnnotations;
using DevHelp.Models.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DevHelp.Areas.Identity.Pages.Account
{
    // Permite acesso anônimo para autenticação de usuários.
    [AllowAnonymous]
    public class LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<LoginModel> logger) : PageModel
    {
        // Gerencia a operação de login/sessão do usuário.
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        // Permite recuperar informações completas do usuário autenticável.
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        // Registra informações de auditoria e diagnóstico.
        private readonly ILogger<LoginModel> _logger = logger;

        // Captura mensagens de erro temporárias entre requisições.
        [TempData]
        public string? ErrorMessage { get; set; }

        // Modelo de entrada do formulário de login.
        [BindProperty]
        public InputModel Input { get; set; } = new();

        // Retorno após login com sucesso.
        public string? ReturnUrl { get; set; }

        // Estrutura de dados do formulário.
        public class InputModel
        {
            // E-mail institucional utilizado para autenticação.
            [Required(ErrorMessage = "O e-mail é obrigatório.")]
            [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
            public string Email { get; set; } = string.Empty;

            // Senha cadastrada para autenticação.
            [Required(ErrorMessage = "A senha é obrigatória.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            // Opção para manter o usuário conectado no navegador.
            [Display(Name = "Manter conectado")]
            public bool RememberMe { get; set; }
        }

        // Inicializa a página de login.
        public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
        {
            // Evita exibir login quando o usuário já possui sessão autenticada.
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // Exibe erros pendentes em tela quando existirem.
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            // Define retorno padrão para a página inicial.
            returnUrl ??= Url.Content("~/");

            // Garante limpeza de cookies de autenticação externa.
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Armazena retorno para uso posterior no post.
            ReturnUrl = returnUrl;

            // Renderiza a página de login apenas para usuários não autenticados.
            return Page();
        }

        // Processa tentativa de login com e-mail e senha.
        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            // Define retorno padrão para a home.
            returnUrl ??= Url.Content("~/");

            // Continua apenas com dados válidos.
            if (ModelState.IsValid)
            {
                // Tenta autenticar usuário via e-mail/senha.
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

                // Segue fluxo de sucesso quando autenticação for concluída.
                if (result.Succeeded)
                {
                    // Obtém o usuário autenticado para validações adicionais.
                    var user = await _userManager.FindByEmailAsync(Input.Email);

                    // Se perfil ainda não foi concluído, direciona para a página de completar perfil.
                    if (user is not null && user.EmailConfirmed && !user.HasCompletedProfile())
                    {
                        return RedirectToPage("/Profile/Complete", new { area = "" });
                    }

                    // Registra sucesso da autenticação em log.
                    _logger.LogInformation("Usuário autenticado com sucesso.");
                    // Retorna para a URL original solicitada.
                    return LocalRedirect(returnUrl);
                }

                // Trata cenário de conta com 2FA habilitado.
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }

                // Trata cenário de conta bloqueada.
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Conta bloqueada durante tentativa de login.");
                    return RedirectToPage("./Lockout");
                }

                // Exibe mensagem genérica para credenciais inválidas.
                ModelState.AddModelError(string.Empty, "Tentativa de login inválida.");
                return Page();
            }

            // Retorna página quando houver erro de validação.
            return Page();
        }
    }
}
